#define DEBUG_BREAK
using System.Xml;
using System.Xml.Linq;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.CodeAnalysis;
namespace PDF;

public class XmlMapProcessor {
	public string AssemblyName => this.GetType().Assembly.GetName().Name!;
	public string Namespace => this.GetType().Namespace!;
	class Parameter {
		public string Name { get; set; }
		public string Value { get; set; }
	}
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	private static bool IsScriptExpression(string value) => value.Contains("<%");
	private static string ExtractScript(string value)	=> value.Replace("<%", "").Replace("%>", "")	.Trim();
	public async Task<XElement> ProcessPdfAndMapAsync(string pdfPath, string xmlMapPath) {
		using var reader = new PdfReader(pdfPath);
		using var writer = new PdfWriter("C:\\temp\\pdfOut.pdf");
		using var pdfDoc = new PdfDocument(reader, writer);
		var originalDoc = XDocument.Load(xmlMapPath, LoadOptions.SetLineInfo);
		var replicaDoc = GetElementAsDocument(originalDoc, "po");
		var scriptGlobals = new Dictionary<string, object>();
		foreach (var element in replicaDoc.Descendants()) {
			foreach (var attr in element.Attributes().ToList()) {
				if (!IsScriptExpression(attr.Value))
					continue;
				string script = ExtractScript(attr.Value);
				Log.Debug($"Executing script at {attr.GetXPath()} => {script}");
				object? result = await ScriptRunner.RunAsync(
					script,
					scriptGlobals,
					pdfDoc
				);

				switch (result) {
					case XElement xe:
						element.Add(xe);
						attr.Remove();
						break;

					case ExtractedArea area:
						attr.Value = area.Value ?? string.Empty;
						Render.DrawBorder(pdfDoc, area.Bounds);
						break;

					case string s:
						attr.Value = s;
						break;

					case null:
						Log.Warn($"Script returned null: {script}");
						attr.Value = string.Empty;
						break;

					default:
						attr.Value = result.ToString() ?? string.Empty;
						break;
				}
			}
		}
		return replicaDoc.Root!;
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
				default:// Ignore others if needed
					break;
			}
		}
		return newElement;
	}
	static XElement ReplicateXElement(XElement source) {
		var newElement = new XElement(source.Name); // Create new element with same name
		foreach (var attr in source.Attributes()) {// Copy all attributes exactly as they are
			newElement.SetAttributeValue(attr.Name, attr.Value);
		}
		foreach (var node in source.Nodes()) {  // Recursively replicate child nodes (elements, text, comments, etc.)
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
	public static XDocument GetElementAsDocument(XDocument doc, string elementName) {
		if (doc == null) throw new ArgumentNullException(nameof(doc));
		if (string.IsNullOrWhiteSpace(elementName)) throw new ArgumentException("Element name cannot be null or empty.", nameof(elementName));
		var element = doc.Root?.Element(elementName);   // Find the first element with the given name
		if (element == null) return null;
		return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement(element));// Create a new XDocument containing a deep copy of the element
	}
}
public static class XElementExtensions {
	public static void SetAttributes(this XElement e, Dictionary<string, string> dict) {
		if (e == null || dict == null) return;

		foreach (KeyValuePair<string, string> kv in dict) {
			e.SetAttributeValue(kv.Key, kv.Value);
		}
	}
	public static int LineNumber(this XObject obj) {
		var lineInfo = (IXmlLineInfo)obj;
		return lineInfo.HasLineInfo() ? int.Parse(lineInfo.LineNumber.ToString()) : 0;
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