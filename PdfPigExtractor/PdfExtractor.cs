using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Filter;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

public class ScriptGlobals {
	public string text { get; set; }
	public string marker { get; set; }
}

public static class PdfExtractor {
	public static XDocument ExtractPdfContentAsXml(string pdfPath, string xmlMap, int pageNumber = 1) {
		XDocument mapDoc = XDocument.Load(xmlMap);
		string documentType = mapDoc.Root?.Attribute("document")?.Value ?? "Document";
		XDocument extractedDoc = new XDocument(new XElement(documentType));
		XElement root = extractedDoc.Root!;
		using (PdfReader reader = new PdfReader(pdfPath))
		using (PdfDocument pdf = new PdfDocument(reader)) {
			PdfPage page = pdf.GetPage(pageNumber);
			float pageHeight = page.GetPageSizeWithRotation().GetHeight();
			float pageWidth = page.GetPageSizeWithRotation().GetWidth();
			string fullText = PdfTextExtractor.GetTextFromPage(page);
			Console.WriteLine($"Full page text length: {fullText.Length}");
			Console.WriteLine($"Full page text snippet: {fullText.Substring(0, Math.Min(200, fullText.Length))}...");
			var earMarked = mapDoc.Root?.Element("earMarked");
			if (earMarked == null) return extractedDoc;
			foreach (var area in earMarked.Elements("area")) {
				string? areaName = area.Attribute("name")?.Value;
				string? rectStr = area.Attribute("rectangle")?.Value;
				string? parentName = area.Attribute("parent")?.Value;
				if (string.IsNullOrEmpty(areaName) || string.IsNullOrEmpty(rectStr) || string.IsNullOrEmpty(parentName))
					continue;
				XElement? parentElem;
				if (parentName == root.Name.LocalName) {
					parentElem = root;
				} else {
					parentElem = root.Element(parentName);
					if (parentElem == null) {
						parentElem = new XElement(parentName);
						root.Add(parentElem);
					}
				}
				XElement areaElem = new XElement(areaName);
				parentElem.Add(areaElem);
				string[] parts = rectStr.Split(',');
				if (parts.Length != 4) continue;
				float x = float.Parse(parts[0]);
				float y = float.Parse(parts[1]);
				float w = float.Parse(parts[2]);
				float h = float.Parse(parts[3]);
				Rectangle rect = new Rectangle(x, y, w, h);
				Console.WriteLine($"Rectangle for {areaName}: x={x}, y={y}, width={w}, height={h} (page height={pageHeight}, width={pageWidth})");
				TextRegionEventFilter regionFilter = new TextRegionEventFilter(rect);
				FilteredTextEventListener strategy = new FilteredTextEventListener(new LocationTextExtractionStrategy(), regionFilter);
				string extractedText = PdfTextExtractor.GetTextFromPage(page, strategy).Trim();
				Console.WriteLine($"Extracted text for area {areaName}: '{extractedText}'");
				var rowSet = area.Element("rowSet");
				if (rowSet == null) {
					string value = extractedText;
					if (value.Contains(':')) {
						value = value.Split(':').Last().Trim();
					}
					areaElem.Value = value;
				} else {
					string[] lines = extractedText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
						.Select(l => l.Trim())
						.Where(l => !string.IsNullOrEmpty(l))
						.ToArray();
					if (lines.Length > 0 && lines[0].EndsWith(":")) {
						lines = lines.Skip(1).ToArray();
					}
					Console.WriteLine($"Lines for area {areaName}: {lines.Length}");
					for (int i = 0; i < lines.Length; i++) {
						Console.WriteLine($"Line {i}: '{lines[i]}'");
					}
					foreach (var row in rowSet.Elements("row")) {
						string? indexStr = row.Attribute("index")?.Value;
						if (!int.TryParse(indexStr, out int index) || index >= lines.Length || index < 0) continue;
						string text = lines[index];
						string? operation = row.Attribute("operation")?.Value;
						string? rowName = row.Attribute("name")?.Value;
						string? parentAttr = row.Attribute("parent")?.Value;
						if (string.IsNullOrEmpty(operation) || string.IsNullOrEmpty(rowName))
							continue;
						XElement targetElem = areaElem;
						if (!string.IsNullOrEmpty(parentAttr)) {
							XElement? existing = areaElem.Element(parentAttr);
							if (existing == null) {
								existing = new XElement(parentAttr);
								areaElem.Add(existing);
							}
							targetElem = existing;
						}
						if (operation == "copy") {
							targetElem.Add(new XElement(rowName, text));
						} else if (operation == "script") {
							string markerVal = row.Element("marker")?.Attribute("value")?.Value ?? string.Empty;
							XElement? scriptElem = row.Element("script");
							if (scriptElem == null) continue;
							string? lang = scriptElem.Attribute("language")?.Value;
							if (lang != "csharp") continue;
							string code = scriptElem.Value.Trim();
							code = code.Replace("StringSplitOptions", "System.StringSplitOptions");
							var scriptOptions = ScriptOptions.Default
								.WithImports("System")
								.WithImports("System.Linq")
								.WithReferences(
									typeof(string).Assembly,
									typeof(Enumerable).Assembly
								);
							var globals = new ScriptGlobals { text = text, marker = markerVal };
							object? result = null;
							try {
								result = CSharpScript.EvaluateAsync(code, scriptOptions, globals).Result;
							} catch (Exception ex) {
								Console.WriteLine($"Script execution error for row {rowName}: {ex.Message}");
							}
							if (result == null) continue;
							PropertyInfo[] props = result.GetType().GetProperties();
							XElement? columns = row.Element("columns");
							if (columns != null) {
								foreach (var col in columns.Elements()) {
									string colName = col.Name.LocalName;
									object? val = null;
									PropertyInfo? prop = props.FirstOrDefault(p => p.Name == colName);
									if (prop != null) {
										val = prop.GetValue(result);
									} else {
										prop = props.FirstOrDefault(p => string.Equals(p.Name, colName, StringComparison.OrdinalIgnoreCase));
										if (prop != null) {
											val = prop.GetValue(result);
										}
									}
									targetElem.Add(new XElement(colName, val?.ToString() ?? string.Empty));
								}
							}
						}
					}
				}
			}
		}

		return extractedDoc;
	}
}