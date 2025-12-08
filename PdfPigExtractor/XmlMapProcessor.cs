#define DEBUG_BREAK
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.XMP.Impl;
using iText.Layout;
using iText.Layout.Splitting;
using iText.StyledXmlParser.Jsoup.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
	string Text,
	Rectangle Bounds
);


	private XElement doRowScript(XElement row, Dictionary<string, object> globals) {
		var markerNode = row.Element("marker");
		if (markerNode != null)
			globals["marker"] = markerNode.Attribute("value")?.Value!;
		var inputNode = row.Element("input");
		if (inputNode != null) {
			string attr = (string)inputNode.Attribute("dataAttribute")!;
			globals["input"] = attr;
		}
		var script = row.Element("script")!.Value.Replace("\t", "").Replace("\n", "").Replace("\r", "");
		var result = ScriptRunner.Run(script, globals);


		foreach (var col in row.Element("columns")!.Elements()) {// 🔹 Map returned anonymous object to XML <columns>
			string colName = col.Name.LocalName;
			var prop = result?.GetType().GetProperty(colName);
			row.Add(new XElement(colName, prop?.GetValue(result)?.ToString() ?? ""));
		}
		return row;
	}
	private bool isTrue(XElement element, string attributeName) {
		return element.Attribute(attributeName) != null ? bool.Parse(element.Attribute(attributeName)!.Value) : false;
	}
	private bool hasElement(XElement e, string name) {
		return e.Element(name) != null;
	}
	private static string getAttributesAsDelimited(XElement el, string delimiter = ",") {
		var attributeStrings = el.Attributes()
		.Select(attr => $"{attr.Name.LocalName}={attr.Value}");

		// 4. Combine all the formatted strings using the specified delimiter.
		return string.Join(delimiter, attributeStrings);
	}

	private async Task<XElement> ProcessRowSet(XElement areaElement, string text) {
		XElement rowSet = areaElement.Element("rowSet")!;
		XElement output = new XElement(areaElement.Attribute("name")?.Value!);
		if (rowSet != null)
			foreach (var row in rowSet.Elements("row")) {
				string op = (string)row.Attribute("operation")!;
				string rowName = (string)row.Attribute("name")!;
				XElement xmlRow = new XElement(rowName);
				switch (op.ToLower()) {
					case "copy":
						int idx = int.Parse(row.Attribute("index")!.Value);
						var lines = text.Split('\n');
						xmlRow.Value = lines.Length > idx ? lines[idx].Trim() : "";
						break;

					case "script":// 🔹 Build globals (text, marker, input, etc.)
						Dictionary<string, object> globals = new() { ["text"] = text };
						if (isTrue(row, "repeat")) {
							string[] inDataRows = text.Split("\r\n\r\n");
							foreach (string x in inDataRows) {

								xmlRow = doRowScript(row, new Dictionary<string, object> { ["text"] = x });
							}
							Log.Debug($"Repeat row in ={xmlRow.Name}");
							//Debugger.Break();
						} else
							xmlRow = doRowScript(row, new Dictionary<string, object> { ["text"] = text });
						break;

					default:
						throw new InvalidOperationException($"Unknown row operation: '{op}'");
				}
				output.Add(xmlRow);
			}
		return output;
	}
	static XElement ReplicateXElementWithPdf(XElement src, PdfDocument pdfDoc) {
		var newElement = new XElement(src.Name);
		string value = "";
		List<ExtractedArea> collectedAreas = new(); // Store results before drawing
		foreach (var attr in src.Attributes()) {
			Dictionary<string, string> prms = attr.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(pair => pair.Split('=', 2)) // Split into exactly 2 parts
			.Where(parts => parts.Length == 2)          // Ensure we have a key and a value
			.ToDictionary(
				parts => parts[0].Trim(),
				parts => parts[1].Trim()
			);
			string valueFrom = prms["src"] ?? throw new Exception($"Missing mandatory parameter 'src' in attribute={attr.Name} ");
			switch (valueFrom.ToLower()) {
				case "pdf":
					var strategy = new StopOnLargeGapStrategy(float.Parse(prms["x"]), float.Parse(prms["scanBelowY"]), float.Parse(prms["width"]), float.Parse(prms["line2LineGap"]));
					var parser = new PdfCanvasProcessor(strategy);
					parser.ProcessPageContent(pdfDoc.GetPage(1)); // Safe because no drawing yet
					value = strategy.GetResultantText();
					Rectangle bounds = strategy.GetCollectedTextBounds();
					collectedAreas.Add(new ExtractedArea(attr.Name.ToString(), value, bounds));
					Log.Debug($"{attr.Name}={value}");
					break;
				case "constant":
					 value = attr.Value.Split(',', 2)[0];
					attr.SetValue(value);

					Log.Debug($"{attr.Name}={value}");
					break;
			}
			newElement.SetAttributeValue(attr.Name, value);
		}
		foreach (var node in src.Nodes()) {

			switch (node) {
				case XElement childElement:
					newElement.Add(ReplicateXElementWithPdf(childElement, pdfDoc));
					break;
				case XText text:
					newElement.Add(new XText(text.Value));
					break;

				case XComment comment:
					newElement.Add(new XComment(comment.Value));
					break;

				//case XCData cdata:
				//	newElement.Add(new XCData(cdata.Value));
				//	break;

				case XProcessingInstruction pi:
					newElement.Add(new XProcessingInstruction(pi.Target, pi.Data));
					break;

				default:
					// Ignore others if needed
					break;
			}
		}
		return newElement;
	}

	static XElement ReplicateXElement(XElement source) {
		// Create new element with same name
		var newElement = new XElement(source.Name);

		// Copy all attributes exactly as they are
		foreach (var attr in source.Attributes()) {
			newElement.SetAttributeValue(attr.Name, attr.Value);
		}

		// Recursively replicate child nodes (elements, text, comments, etc.)
		foreach (var node in source.Nodes()) {
			switch (node) {
				case XElement childElement:
					newElement.Add(ReplicateXElement(childElement));
					break;

				case XText text:
					newElement.Add(new XText(text.Value));
					break;

				case XComment comment:
					newElement.Add(new XComment(comment.Value));
					break;

				//case XCData cdata:
				//	newElement.Add(new XCData(cdata.Value));
				//	break;

				case XProcessingInstruction pi:
					newElement.Add(new XProcessingInstruction(pi.Target, pi.Data));
					break;

				default:
					// Ignore others if needed
					break;
			}
		}

		return newElement;
	}

	static XDocument ReplicateXDocument(XDocument source) {
		if (source.Root is null) throw new InvalidOperationException("Source document has no root element.");
		XElement replicatedRoot = ReplicateXElement(source.Root);
		return new XDocument(replicatedRoot);
	}
	static XDocument GetChildren(XDocument doc) {
		var children = doc.Root?.Elements().Select(e => new XElement(e)) ?? Enumerable.Empty<XElement>();
		return new XDocument(children);
	}
	public async Task<XElement> ProcessPdfAndMap(string pdfPath, string xmlMapPath) {

		using var reader = new PdfReader(pdfPath);
		using var writer = new PdfWriter("C:\\temp\\pdfOut.pdf");
		using var pdfDoc = new PdfDocument(reader, writer);
		List<ExtractedArea> collectedAreas = new(); // Store results before drawing
		XDocument originalDoc = XDocument.Load(xmlMapPath);
		XDocument replicaDoc =GetChildren( ReplicateXDocument(originalDoc));// ignore the root <pdfMap>, it is not relavent  
		string value = ""; // work vars
		
		foreach (XElement xe in replicaDoc.Descendants()) {
			foreach (var attr in xe.Attributes()) {
				Dictionary<string, string> prms = attr.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(pair => pair.Split('=', 2)) // Split into exactly 2 parts
				.Where(parts => parts.Length == 2)          // Ensure we have a key and a value
				.ToDictionary(
					parts => parts[0].Trim(),
					parts => parts[1].Trim()
				);
				string valueFrom = prms["src"] ?? throw new Exception($"Missing mandatory parameter 'src' in attribute={attr.Name} ");
				switch (valueFrom.ToLower()) {
					case "pdf":
						var strategy = new StopOnLargeGapStrategy(float.Parse(prms["x"]), float.Parse(prms["scanBelowY"]), float.Parse(prms["width"]), float.Parse(prms["line2LineGap"]));
						var parser = new PdfCanvasProcessor(strategy);
						parser.ProcessPageContent(pdfDoc.GetPage(1)); // Safe because no drawing yet
						value = strategy.GetResultantText().Replace("\r","").Replace("\n","");
						Rectangle bounds = strategy.GetCollectedTextBounds();
						collectedAreas.Add(new ExtractedArea(attr.Name.ToString(), value, bounds));
						Log.Debug($"{attr.Name}={value}");
						break;
					case "constant":
						value = attr.Value.Split(',', 2)[0];
						attr.SetValue(value);

						Log.Debug($"{attr.Name}={value}");
						break;
				}
				xe.SetAttributeValue(attr.Name, value);
			}
		}




		return null;
		await Task.CompletedTask;
	}



}
