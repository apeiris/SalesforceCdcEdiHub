#define DEBUG_BREAK
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Org.BouncyCastle.Crypto;
namespace PDF;

public class XmlMapProcessor {
	class Parameter {
		public string Name { get; set; }
		public string Value { get; set; }
	}
	public class ExtractBelowGlobals {
		public float lineHeight;
		public float belowY;
		public iText.Kernel.Pdf.PdfPage Page;
		// Outputs
		public List<List<string>> ResultRows;
		public iText.Kernel.Geom.Rectangle BoundingBox;
		public XDocument XmlContent;
	}
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	public record ExtractedArea(
	string Name,
	string Parent,
	string Text,
	Rectangle Bounds
);
	private async Task<XElement> ProcessRowSet(XElement areaElement, string text) {
		XElement rowSet = areaElement.Element("rowSet")!;
		XElement output = new XElement(areaElement.Attribute("name")?.Value);
		foreach (var row in rowSet.Elements("row")) {
			string op = (string)row.Attribute("operation")!;
			string rowName = (string)row.Attribute("name")!;
			XElement xmlRow = new XElement(rowName);
			switch (op.ToLower()) {
				case "copy": {
						int idx = int.Parse(row.Attribute("index")!.Value);
						var lines = text.Split('\n');
						xmlRow.Value = lines.Length > idx ? lines[idx].Trim() : "";
						break;
					}
				case "script": {
						Dictionary<string, object> globals = new() {// 🔹 Build globals (text, marker, input, etc.)
							["text"] = text.Trim(['\t', '\r', '\n'])
						};
						var markerNode = row.Element("marker");
						if (markerNode != null)
							globals["marker"] = markerNode.Attribute("value")?.Value!;
						var inputNode = row.Element("input");
						if (inputNode != null) {
							string attr = (string)inputNode.Attribute("dataAttribute")!;
							globals["input"] = attr;
						}
						var script = row.Element("script")!.Value;//.Replace("\t", "").Replace("\n", "").Replace("\r", "");
						var result = ScriptRunner.Run(script, globals);


						foreach (var col in row.Element("columns")!.Elements()) {// 🔹 Map returned anonymous object to XML <columns>
							string colName = col.Name.LocalName;
							var prop = result?.GetType().GetProperty(colName);
							xmlRow.Add(new XElement(colName, prop?.GetValue(result)?.ToString() ?? ""));
						}
						break;
					}
				default:
					throw new InvalidOperationException($"Unknown row operation: '{op}'");
			}
			output.Add(xmlRow);
		}
		return output;
	}
	public async Task<XDocument> ProcessPdfAndMap(string pdfPath, List<string> tableHeader, string pdfMapFilePath) {
		List<ExtractedArea> collectedAreas = new(); // Store results before drawing

		using var reader = new PdfReader(pdfPath);
		using var writer = new PdfWriter("C:\\temp\\pdfOut.pdf");
		using var pdfDoc = new PdfDocument(reader, writer);

		XDocument mapDoc = XDocument.Load(pdfMapFilePath);
		XDocument resDoc = new(new XElement(mapDoc.Root?.Attribute("document")?.Value ?? "Document"));
		var areas = mapDoc.Descendants("area");

		// Step 1: Extract text for each area
		foreach (var area in areas) {
			var name = (string)area.Attribute("name")!;
			var parent = (string)area.Attribute("parent")!;
			var parameters = area.Descendants("parameter")
								 .ToDictionary(x => (string)x.Attribute("name")!, x => x.Value);

			float startX = float.Parse(parameters["x"]);
			float scanBelowY = float.Parse(parameters["scanBelowY"]);
			float scanWidth = float.Parse(parameters["scanWidth"]);
			float verticalGap = float.Parse(parameters["verticalGapBelow"]);

			var strategy = new StopOnLargeGapStrategy(startX, scanBelowY, scanWidth, verticalGap);
			var parser = new PdfCanvasProcessor(strategy);
			parser.ProcessPageContent(pdfDoc.GetPage(1)); // Safe because no drawing yet

			string extractedText = strategy.GetResultantText();
			Rectangle bounds = strategy.GetCollectedTextBounds();

			collectedAreas.Add(new ExtractedArea(name, parent, extractedText, bounds));
		}

		// Step 2: Draw borders/labels and build XML
		foreach (var area in collectedAreas) {
			switch (area.Name.ToLower()) {
				case "buyer":
				case "seller":
				case "orderitems":
					Render.DrawBorder(pdfDoc, area.Bounds);
					Render.DrawCornerLabel(pdfDoc, area.Bounds, LabelLocation.BOTTOM_LEFT_and_TOP_RIGHT_NODECIMAL);
					break;
				default:
					break;
			}

			var areaNode = areas.First(x => (string)x.Attribute("name")! == area.Name);

			try {
				XElement mappedAreaXml = await ProcessRowSet(areaNode, area.Text);
				resDoc.Root!.Add(mappedAreaXml); // Add under root
			} catch (Exception ex) {
				Log.Error(ex.Message);
				throw;
			}
		}

		// Step 3: Return the final XML document
		return resDoc;
	}


	public static async Task<(List<List<string>> Rows, Rectangle Box, XDocument xd)> RunExtractorScriptAsync(PdfPage page, float lheight, float below) {
		// 1. Get types for references
		Type extractorType = typeof(PDF.ExtractPdfTableBelowY);
		Type itextExtractorType = typeof(PdfTextExtractor); // Needed for GetTextFromPage
		Type xdocType = typeof(XDocument);
		// 2. Setup Globals with the Page
		var globals = new ExtractBelowGlobals {
			lineHeight = lheight,
			belowY = below,
			Page = page,
			ResultRows = null,
			BoundingBox = null
		};

		// 3. The Script Code
		// Now the script does the work: Instantiates AND Extracts
		string scriptCode = @"
        // 1. Instantiate
        var extractor = new PDF.ExtractPdfTableBelowY(verticalGap, belowY);
        
        // 2. Run Extraction (using the passed 'Page' variable)
        iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(Page, extractor);
        
        // 3. Get Rows and assign to result
        ResultRows = extractor.GetTableRows();
		BoundingBox=extractor.GetTableBoundingBox();
		XmlContent=PDF.ExtractPdfTableBelowY.ConvertToXDocument(ResultRows,""OrderItems"");";

		var scriptOptions = ScriptOptions.Default
			.AddImports("PDF", "iText.Kernel.Pdf.Canvas.Parser") // Import namespace
			.AddReferences(
				extractorType.Assembly,      // Your assembly
				itextExtractorType.Assembly, // iText.kernel assembly
				typeof(PdfPage).Assembly,     // iText.io or kernel assembly
				xdocType.Assembly
			)
			.WithAllowUnsafe(false);

		var script = CSharpScript.Create(
			scriptCode,
			options: scriptOptions,
			globalsType: typeof(ExtractBelowGlobals)
		);

		await script.RunAsync(globals);

		return (globals.ResultRows ?? new List<List<string>>(),
				globals.BoundingBox!,
				globals.XmlContent!);
	}
}
