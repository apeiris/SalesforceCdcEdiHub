//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace PdfDataExtraction; 
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;


public static class PdfDataExtractor {
	/// <summary>
	/// Extracts data from PDF using the <pdfMap> XML and returns structured XDocument.
	/// Respects parent="..." attribute for grouping.
	/// </summary>

	public static XDocument ExtractToXml(
string pdfPath,
int pageNumber,
string pdfMapXml) {
		var mapDoc = XDocument.Load(pdfMapXml);
		var earMarked = mapDoc.Root?.Element("earMarked")
		?? throw new InvalidOperationException("<earMarked> element not found.");
		var client = (string)mapDoc.Root?.Attribute("client") ?? "";
		var document = (string)mapDoc.Root?.Attribute("document") ?? "";
		var resultDoc = new XDocument(
		new XDeclaration("1.0", "utf-8", "yes"),
		new XElement("extractedData",
		new XAttribute("client", client),
		new XAttribute("document", document)
		)
		);
		var resultRoot = resultDoc.Root!;
		var parentNodes = new Dictionary<string, XElement>();
		foreach (var areaElem in earMarked.Elements("area")) {
			string areaName = (string)areaElem.Attribute("name") ?? "Unknown";
			string rectStr = (string)areaElem.Attribute("rectangle") ?? "";
			string parent = (string)areaElem.Attribute("parent") ?? "";
			if (string.IsNullOrWhiteSpace(rectStr)) continue;
			var rect = ParseRectangle(rectStr);
			var rawTable = PdfDataExtraction.PdfTableExtractor.ExtractSingleTable(pdfPath, pageNumber, rect, areaName);
			if (rawTable.Rows.Count == 0) continue;
			var srcRow = rawTable.Rows[0];
			var areaResult = new XElement(areaName);
			if (areaElem.Element("rowSet") == null) {// === SIMPLE AREA (no rowSet) ===
				areaResult.Value = srcRow[0]?.ToString() ?? "";
			} else {// === ROWSET AREA ===
				var rowSet = areaElem.Element("rowSet");
				foreach (var rowElem in rowSet.Elements("row")) {
					int index = (int?)rowElem.Attribute("index") ?? -1;
					string name = (string)rowElem.Attribute("name") ?? "";
					string operation = (string)rowElem.Attribute("operation") ?? "";
					string executor = (string)rowElem.Attribute("executor") ?? "";
					string inputAttr = (string)rowElem.Element("input")?.Attribute("dataAttribute") ?? "";
					//string rawText = "";
					string rawText = rawTable.Rows[index][0]?.ToString() ?? "";
					if (!string.IsNullOrEmpty(inputAttr) && rawTable.Columns.Contains(inputAttr))
						rawText = srcRow[inputAttr]?.ToString() ?? "";
					else if (index >= 0 && index < rawTable.Columns.Count)
						rawText = srcRow[index]?.ToString() ?? "";
					if (string.IsNullOrEmpty(operation) || operation == "copy") {
						if (!string.IsNullOrEmpty(name))
							areaResult.Add(new XElement(name, rawText));
						continue;
					}
					if (operation == "script") {
						string script = rowElem.Element("script")?.Value ?? "";
						string marker = (string)rowElem.Element("marker")?.Attribute("value") ?? "";
						var result = RunScript(script, rawText, marker);
						var columns = rowElem.Element("columns")?.Elements().Select(e => e.Name.LocalName)
						?? Enumerable.Empty<string>();
						foreach (var col in columns) {
							string val = GetProperty(result, col);
							areaResult.Add(new XElement(col, val));
						}
						continue;
					}
					if (operation == "transform" && !string.IsNullOrEmpty(executor)) {
						string marker = (string)rowElem.Element("marker")?.Attribute("value") ?? "";
						var result = CallExecutor(executor, rawText, marker);
						var columns = rowElem.Element("columns")?.Elements().Select(e => e.Name.LocalName)
						?? Enumerable.Empty<string>();
						foreach (var col in columns) {
							string val = GetProperty(result, col);
							areaResult.Add(new XElement(col, val));
						}
					}
				}
			}
			XElement container;// === ADD TO PARENT OR ROOT ===
			if (!string.IsNullOrEmpty(parent)) {
				if (!parentNodes.TryGetValue(parent, out container)) {
					container = new XElement(parent);
					parentNodes[parent] = container;
					resultRoot.Add(container);
				}
			} else {
				container = resultRoot;
			}
			container.Add(areaResult);
		}
		return resultDoc;
	}


	// -----------------------------------------------------------------
	// Helper: Parse rectangle "x,y,width,height"
	// -----------------------------------------------------------------
	private static iText.Kernel.Geom.Rectangle ParseRectangle(string rect) {
		var parts = rect.Split(',');
		if (parts.Length != 4) throw new FormatException($"Invalid rectangle format: {rect}");
		float x = float.Parse(parts[0].Trim());
		float y = float.Parse(parts[1].Trim());
		float w = float.Parse(parts[2].Trim());
		float h = float.Parse(parts[3].Trim());
		return new iText.Kernel.Geom.Rectangle(x, y, w, h);
	}

	// -----------------------------------------------------------------
	// Helper: Run embedded C# script
	// -----------------------------------------------------------------
	private static object RunScript(string body, string text, string marker) {
		// Escape quotes properly
		text = text?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
		marker = marker?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

		// Full script: text and marker are injected as local variables
		var code = $@"
        using System;
        using System.Linq;

        var text = ""{text}"";
        var marker = ""{marker}"";
        {body}
    ";

		var options = ScriptOptions.Default
			.AddReferences(typeof(object).Assembly)
			.AddReferences(typeof(Enumerable).Assembly);

		return CSharpScript.EvaluateAsync(code, options).Result;
	}

	// -----------------------------------------------------------------
	// Helper: Call static transform method
	// -----------------------------------------------------------------
	private static object CallExecutor(string methodName, string text, string delimiter) {
		var mi = typeof(PdfDataExtractor).GetMethod(methodName,
			BindingFlags.Static | BindingFlags.NonPublic);
		if (mi == null) throw new MissingMethodException($"Executor '{methodName}' not found.");
		return mi.Invoke(null, new object[] { text, delimiter });
	}

	private static object SplitAddressAndContact(string text, string delimiter) {
		var p = text.Split(new[] { delimiter }, StringSplitOptions.None);
		return new {
			street = p.Length > 0 ? p[0].Trim() : "",
			city = p.Length > 1 ? p[1].Trim() : "",
			postalCode = p.Length > 2 ? p[2].Trim() : "",
			country = p.Length > 3 ? p[3].Trim() : ""
		};
	}

	// -----------------------------------------------------------------
	// Helper: Get property from anonymous object
	// -----------------------------------------------------------------
	private static string GetProperty(object obj, string name) {
		if (obj == null) return "";
		var pi = obj.GetType().GetProperty(name,
			BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
		return pi?.GetValue(obj)?.ToString() ?? "";
	}
}
