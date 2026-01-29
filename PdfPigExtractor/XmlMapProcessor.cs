#define DEBUG_BREAK
using System.Xml.Linq;
using System.Xml.XPath;
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
	private static string ExtractScript(string value) => value.Replace("<%", "").Replace("%>", "").Trim();
	public async Task<XElement> ProcessPdfAndMapAsync(string xmlMapPath) {
		string pdfPath = "";
		XDocument xd = XDocument.Load(xmlMapPath);
		 pdfPath = xd.XPathSelectElement("//pdfMap")?.Attribute("pdfSource")?.Value!;
		

		var scriptGlobals = new Dictionary<string, object>();
		var extractedAreas = new List<ExtractedArea>();
		scriptGlobals["__extractedAreas"] = extractedAreas;
		using var reader = new PdfReader(pdfPath);
		using var writer = new PdfWriter("C:\\temp\\pdfOut.pdf");
		using var pdfDoc = new PdfDocument(reader, writer);
		var originalDoc = XDocument.Load(xmlMapPath, LoadOptions.SetLineInfo);
		XDocument replicaDoc = new XDocument(originalDoc);

		foreach (var element in replicaDoc.Descendants()) {
			foreach (var attr in element.Attributes().ToList()) {
				if (!IsScriptExpression(attr.Value)) continue;
				scriptGlobals["__currentElement"] = element;
				string script = ExtractScript(attr.Value);
				Log.Debug($"Executing script at {attr.GetXPath()} => {script}");
				object? result = await ScriptRunner.RunAsync(
					script,
					scriptGlobals,
					pdfDoc
				);
				switch (result) {
					case XElement xe:
						if (ReferenceEquals(xe, element)) {
							attr.Remove(); // mapping consumed
						} else {
							attr.Remove();
							element.ReplaceWith(xe);
						}
						break;
					case ExtractedArea area:
						attr.Value = area.Value ?? string.Empty;
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
		foreach (ExtractedArea a in (List<ExtractedArea>)scriptGlobals["__extractedAreas"]) {
			Render.DrawBorder(pdfDoc, a.Bounds);
			Render.DrawCornerLabel(pdfDoc, a.Bounds, LabelLocation.BOTTOM_LEFT_and_TOP_RIGHT_NODECIMAL);
		}

		XmlNormalization.Normalize(replicaDoc);
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
				case XProcessingInstruction pi:
					newElement.Add(new XProcessingInstruction(pi.Target, pi.Data));
					break;
				default:// Ignore others if needed
					break;
			}
		}
		return newElement;
	}
}
internal static class XmlNormalization {
	public static void Normalize(XDocument doc) {
		foreach (var attr in doc.Descendants().Attributes())
			attr.Value = NormalizeValue(attr.Value);
		foreach (var el in doc.Descendants())
			if (!el.HasElements)
				el.Value = NormalizeValue(el.Value);
	}
	private static string NormalizeValue(string? value) {
		if (string.IsNullOrWhiteSpace(value))
			return value ?? string.Empty;
		return new string(
			value
				.Where(c => !char.IsControl(c) || c == '\n' || c == '\r')
				.ToArray()
		).Trim();
	}
}
public static class XElementExtensions {

	public static string GetXPath(this XNode node) {
		if (node == null) return string.Empty;
		if (node is XElement element) {
			string path = element.AncestorsAndSelf().Reverse().Select(e => {
				var siblings = e.ElementsBeforeSelf(e.Name).Count() + 1;
				return $"{e.Name.LocalName}[{siblings}]";
			}).Aggregate((current, next) => $"{current}/{next}");
			return $"/{path}";
		}
		return node.Parent != null ? $"{node.Parent.GetXPath()}/{node.NodeType.ToString().ToLower()}()" : "/";
	}
	public static string GetXPath(this XAttribute attribute) {
		if (attribute == null) return string.Empty;
		return $"{attribute.Parent.GetXPath()}/@{attribute.Name.LocalName}";
	}
}