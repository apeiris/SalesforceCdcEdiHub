#define DEBUG_BREAK
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
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

	
	public static XDocument GetElementAsDocument(XDocument doc, string elementName) {
		if (doc == null) throw new ArgumentNullException(nameof(doc));
		if (string.IsNullOrWhiteSpace(elementName)) throw new ArgumentException("Element name cannot be null or empty.", nameof(elementName));
		var element = doc.Root?.Element(elementName);   // Find the first element with the given name
		if (element == null) return null;
		return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement(element));// Create a new XDocument containing a deep copy of the element
	}
	private (bool success,Dictionary<string, string> matches) GetRegex(string input) {
		string[] matchKeys = { "cast", "method", "params" };

		const string pattern = "(?<cast>\\(\\w+\\))?(?<method>\\w+)\\((?<params>[^()]*)\\)";
		Match match = Regex.Match(input, pattern);
		Dictionary<string, string> matches = matchKeys.ToDictionary(k => k, k => match.Groups[k].Value);
	if(match.Success) matches.Add("capture", match.Value);
		return (match.Success,matches);
	}
	public async Task<XElement> ProcessPdfAndMap(string pdfPath, string xmlMapPath) {
		using var reader = new PdfReader(pdfPath);
		using var writer = new PdfWriter("C:\\temp\\pdfOut.pdf");
		using var pdfDoc = new PdfDocument(reader, writer);
		List<ExtractedArea> collectedAreas = new(); // Store results before drawing
		XDocument originalDoc = XDocument.Load(xmlMapPath);
		XDocument replicaDoc = GetElementAsDocument(originalDoc, "po");// ignore the root <pdfMap>, it is not relavent  
		string value = ""; // work vars
		Dictionary<string, string> regExDict = new();
		foreach (XElement xe in replicaDoc.Descendants()) {
			foreach (var attr in xe.Attributes()) {
				var (hasMatch, dict) = GetRegex(attr.Value);
				if (!hasMatch) continue;
				switch (dict["method"]) {
					case "ScrapePDF":
					Dictionary<string,string>	prms = dict["params"].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
										   .Select(pair => pair.Split(':', 2)) // Split into exactly 2 parts
										   .Where(parts => parts.Length == 2)          // Ensure we have a key and a value
										   .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());
						var strategy = new StopOnLargeGapStrategy(float.Parse(prms["x"]), float.Parse(prms["scanBelowY"]), float.Parse(prms["width"]), float.Parse(prms["line2LineGap"]));
						var parser = new PdfCanvasProcessor(strategy);
						parser.ProcessPageContent(pdfDoc.GetPage(1)); // Safe because no drawing yet
						value = strategy.GetResultantText();
						Rectangle bounds = strategy.GetCollectedTextBounds();
						collectedAreas.Add(new ExtractedArea(attr.Name.ToString(), value, bounds));
						xe.SetAttributeValue(attr.Name, value);

						break;
				}
			}
		}


		/*
		foreach (XElement xe in replicaDoc.Descendants()) {
			Log.Info($"Element Name={xe.Name}  XPath={xe.GetXPath()}");
			foreach (var attr in xe.Attributes()) {
				Log.Info(attr.GetXPath());
				var aDict = attr.AttributesToFsmDictionary();
				if (aDict.Count ==0) continue;
				Dictionary<string,string>prms = new Dictionary<string,string>();
				foreach(KeyValuePair<string,string> kv in aDict) {
					if (kv.Key.Contains("<%")|kv.Key.Contains("&lt;%")) {
						regExDict = GetRegex(attr.Value);
					} else regExDict.Clear();
						Log.Debug($"kv k={kv.Key} v={kv.Value}");
					switch (kv.Key.ToLower()) {
						case "scrape":
							 Log.Debug($"v={kv.Value}");
							prms = kv.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
										   .Select(pair => pair.Split('=', 2)) // Split into exactly 2 parts
										   .Where(parts => parts.Length == 2)          // Ensure we have a key and a value
										   .ToDictionary(parts => parts[0].Trim(),parts => parts[1].Trim());
							var strategy = new StopOnLargeGapStrategy(float.Parse(prms["x"]), float.Parse(prms["scanBelowY"]), float.Parse(prms["width"]), float.Parse(prms["line2LineGap"]));
							var parser = new PdfCanvasProcessor(strategy);
							parser.ProcessPageContent(pdfDoc.GetPage(1)); // Safe because no drawing yet
							value = strategy.GetResultantText().Replace("\r", "").Replace("\n", "");
							Rectangle bounds = strategy.GetCollectedTextBounds();
							collectedAreas.Add(new ExtractedArea(attr.Name.ToString(), value, bounds));
							Log.Debug($"{attr.Name}={value}");
							xe.SetAttributeValue(attr.Name, value);
							break;
						case "script":
							// the first op scrape is already done and value has the desired input
							//  revise this  to use Regex
							Log.Debug($"{attr.Name}=value={value} ");
							string method = aDict["script"];
							string script = originalDoc.XPathSelectElement($"/pdfMap/scriptlib/script[@name='{aDict["script"]}']")!.Value;
							var data = new Dictionary<string, object> { ["value"] = value };
							var results =(List<string>)ScriptRunner.Run(script, data);
							if(aDict.Keys.Contains("map")) {
								string xpath = aDict["xpath"];
								string[] atts = aDict["map"].Split(',', StringSplitOptions.RemoveEmptyEntries);
								int i = 0;
								xe.Attribute("map")?.Remove();// remove the map attribute after use
								foreach (string att in atts) {
									if (results.Count-1 < i) break;
									var xx = results[i];
									var attValue = results;
									xe.SetAttributeValue(att, results[i]);
									Log.Debug($"Mapped {att}={attValue.First()}");
									i++;
								}
							}
							break;
					}
				}
			}
		}*/
		foreach (var area in collectedAreas) {
			Render.DrawBorder(pdfDoc, area.Bounds);
			Render.DrawCornerLabel(pdfDoc, area.Bounds, LabelLocation.BOTTOM_LEFT_and_TOP_RIGHT_NODECIMAL);
		}
		Log.Debug(replicaDoc.ToString());
		return (XElement)replicaDoc.Root!;
		await Task.CompletedTask;
	}
}
public static class XElementExtensions {
	///
	///Extension method Bulk updates or creates attributes on an XElement from a Dictionary.	
	///
	public static void SetAttributes(this XElement e, Dictionary<string, string> dict) {
		if (e == null || dict == null) return;

		foreach (KeyValuePair<string, string> kv in dict) {
			e.SetAttributeValue(kv.Key, kv.Value);
		}
	}
	public static Dictionary<string, string> AttributesToFsmDictionary(this XAttribute attr) {
		if (attr == null || string.IsNullOrWhiteSpace(attr.Value)) return new Dictionary<string, string>();// null dictionary
		return attr.Value
			.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
			.Select(segment => {
				char sep = segment.Contains(':') ? ':' : segment.Contains('=') ? '=' : '\0';
				if (sep == '\0') return null;
				var parts = segment.Split(sep, 2);
				if (parts.Length < 2)
					return null;
				return new {
					Key = parts[0].Trim(),
					Value = parts[1].Trim()
				};
			})
			.Where(x => x != null) // remove nulls
			.ToDictionary(x => x.Key, x => x.Value);
	}
	public static string GetXPath(this XNode node) {
		if (node == null) return string.Empty;
		// XNodes include elements, text, and comments. 
		// We handle XElement specifically to get tag names.
		if (node is XElement element) {
			// Build the path by checking indices among siblings
			string path = element.AncestorsAndSelf().Reverse().Select(e => {
				// Count siblings with the same name that appear before this node
				var siblings = e.ElementsBeforeSelf(e.Name).Count() + 1;
				return $"{e.Name.LocalName}[{siblings}]";
			}).Aggregate((current, next) => $"{current}/{next}");

			return $"/{path}";
		}

		// If it's a Text node or Comment, return parent path with specific identifier
		return node.Parent != null ? $"{node.Parent.GetXPath()}/{node.NodeType.ToString().ToLower()}()" : "/";
	}
	public static string GetXPath(this XAttribute attribute) {
		if (attribute == null) return string.Empty;

		// Get the parent element's path and append the attribute identifier (@)
		return $"{attribute.Parent.GetXPath()}/@{attribute.Name.LocalName}";
	}
}