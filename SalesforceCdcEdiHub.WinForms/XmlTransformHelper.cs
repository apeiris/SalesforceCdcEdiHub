using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Rectangle= iText.Kernel.Geom.Rectangle;

// NOTE: CustomTransformers, TextChunkInfo, and CustomLocationTextExtractionStrategy must be defined in your environment.

public class ScriptGlobals {
	public string text { get; set; }
	public string marker { get; set; }
}

public static class XmlTransformHelper {

	public static async Task<IDictionary<string, object>> ExecuteScriptAsync(
	   string scriptCode,
	   string inputValue,
	   string marker) {
		var globals = new ScriptGlobals {
			text = inputValue,
			marker = marker
		};

		// Run script
		var result = await CSharpScript.EvaluateAsync<object>(
			scriptCode,
			globals: globals
		);

		// The script must return an anonymous object like:
		// return new { street = "A", city = "B", postalCode = "C" };

		// Convert anonymous object to dictionary
		return result.GetType()
					 .GetProperties()
					 .ToDictionary(
						p => p.Name,
						p => p.GetValue(result)
					 );
	}

	public static XDocument ExecuteXmlTransformation(string xmlFilePath, string pdfPath, int pageNumber = 1) {
		// 1. Load the configuration XML
		XDocument configDoc = XDocument.Load(xmlFilePath);

		// 2. Initialize the result XDocument with a root element
		XElement resultRoot = new XElement("PurchaseOrderData");

		// 3. Identify all areas that require processing (those with a <rowSet>)
		var processingAreas = configDoc.Descendants("earMarked")
									   .Descendants("area")
									   .Where(a => a.Element("rowSet") != null)
									   .ToList();

		foreach (var area in processingAreas) {
			string areaName = (string)area.Attribute("name")!;
			string rectangleStr = (string)area.Attribute("rectangle")!;

			// Parse rectangle string into iText Rectangle
			string[] coords = rectangleStr.Split(',');
			if (coords.Length != 4 || !coords.All(c => float.TryParse(c.Trim(), out _))) continue;

			float[] rectCoords = coords.Select(c => float.Parse(c.Trim())).ToArray();
			Rectangle areaRectangle = new Rectangle(rectCoords[0], rectCoords[1], rectCoords[2], rectCoords[3]);

			// --- 4. DATA EXTRACTION: Extract raw text chunks from the PDF area ---
			List<string> rawChunks = ExtractList(pdfPath, pageNumber, areaRectangle);

			// --- 5. Derive Raw Data for Each Row (SIMPLIFIED HEURISTIC) ---
			// In a production system, this mapping must be precise.
			string rawName = rawChunks.FirstOrDefault() ?? string.Empty;
			string rawAddress = string.Join(" ", rawChunks);

			var areaRawData = new Dictionary<string, string>
			{
				{ "name", rawName },
				{ "address", rawAddress },
			};

			// --- 6. DATA TRANSFORMATION & XML POPULATION ---
			XElement areaDataElement = new XElement(areaName); // e.g., <Buyer> or <Supplier>
			var rowDefinitions = area.Element("rowSet")?.Elements("row") ?? Enumerable.Empty<XElement>();

			foreach (var row in rowDefinitions) {
				string rowName = (string)row.Attribute("name");
				string rawData = areaRawData.GetValueOrDefault(rowName, string.Empty);
				string operation = (string)row.Attribute("operation") ?? "none";

				if (operation == "none") {
					// Direct mapping: create child element with the row name
					areaDataElement.Add(new XElement(rowName, rawData));
				} else if (operation == "transform") {
					// Execute C# method using Reflection
					string methodName = (string)row.Attribute("executor");
					string marker = row.Element("marker")?.Attribute("value")?.Value ?? ",";

					MethodInfo? method = typeof(XmlTransformHelper).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

					if (method != null && method.Invoke(null, new object[] { rawData, marker }) is List<string> structuredData) {
						// Create nested elements based on <columns> structure
						var columnNames = row.Element("columns")?.Elements().Select(c => c.Name.LocalName).ToList();

						for (int i = 0; i < structuredData.Count && i < columnNames?.Count; i++) {
							// e.g., <street>77 Bay Street</street>
							areaDataElement.Add(new XElement(columnNames[i], structuredData[i]));
						}
					}
				}
			}

			// 7. Add the completed area element to the result root
			resultRoot.Add(areaDataElement);
		}

		// Return the new XDocument containing the structured data
		return new XDocument(resultRoot);
	}

	// The provided ExtractList method (unchanged)
	public static List<string> ExtractList(string pdfPath, int pageNumber, Rectangle rectangle) {
		var allTextChunks = new List<string>();

		if (!File.Exists(pdfPath))
			throw new FileNotFoundException("PDF file not found", pdfPath);

		using (PdfDocument pdfDocument = new(new PdfReader(pdfPath))) {
			if (pageNumber > pdfDocument.GetNumberOfPages() || pageNumber < 1)
				throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number is out of range.");

			PdfPage page = pdfDocument.GetPage(pageNumber);
			PDF.CustomLocationTextExtractionStrategy strategy = new();
			PdfTextExtractor.GetTextFromPage(page, strategy);

			// 2. Filter chunks to only those within the specified bounding box
			List<PDF.TextChunkInfo> filteredChunks = strategy.GetTextContent()
				.Where(chunk =>
					chunk.Location!.GetLeft() >= rectangle.GetLeft() &&
					chunk.Location.GetRight() <= rectangle.GetRight() &&
					chunk.Location.GetBottom() >= rectangle.GetBottom() &&
					chunk.Location.GetTop() <= rectangle.GetTop()
				)
				.OrderByDescending(c => c.BaseLineY)
				.ThenBy(c => c.Location!.GetLeft())
				.ToList();

			// 3. Flatten the list by extracting the text from each chunk
			if (filteredChunks.Any()) {
				allTextChunks = filteredChunks.Select(c => c.Text).ToList();
			}
		}
		return allTextChunks;
	}

}