using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
namespace PDF;
public class XmlMapProcessor {
	public void ProcessPdfAndMap(string pdfPath, List<string> tableHeader, string pdfMapFilePath) {
		Rectangle boundingBox;
		List<List<string>> tableLines;
		XDocument xdContent;
		using PdfReader pdfReader = new(pdfPath);
		using PdfDocument pdfDoc = new(pdfReader);
	//	ExtractTableBelowY(pdfDoc, tableHeader, out boundingBox, out tableLines, out xdContent);
		XDocument pdfMapXml = XDocument.Load(pdfMapFilePath);
		var areas = pdfMapXml.Descendants("area");// Use switch on area name for maintainability and easy extension
		foreach (var area in areas) {
			string areaName = (string)area.Attribute("name")!;
			switch (areaName) {
				case "PurchaseOrderItems":
					float scanBelowY = float.Parse((string)area.Attribute("scanBelowY")!);
					float lineHeight = float.Parse((string)area.Attribute("lineHeight")!);

					ExtractTableBelowY(pdfDoc, scanBelowY, lineHeight, tableHeader, out boundingBox, out tableLines, out xdContent);

					XElement rowSetNode = area.Element("rowSet")!;
					if (rowSetNode != null) {
						rowSetNode.RemoveNodes();
						foreach (XElement row in xdContent.Root!.Elements("row")) {
							rowSetNode.Add(new XElement(row));
						}
					} else {
						area.Add(
							new XElement("rowSet",
								new XAttribute("rowCount", "variable"),
								xdContent.Root!.Elements("row").Select(r => new XElement(r))
							)
						);
					}
					break;
				// Add other cases here for different area names if needed
				default:
					// Do nothing or handle unknown areas as needed
					break;
			}
		}
		//pdfMapXml.Save("ProcessedPdfMap.xml");
		//Console.WriteLine($"Bounding box: Left={boundingBox.GetLeft()}, Bottom={boundingBox.GetBottom()}, Right={boundingBox.GetRight()}, Top={boundingBox.GetTop()}");
	}
	private static void ExtractTableBelowY(	PdfDocument pdfdoc,
											float belowY,
											float lineHeight,
											List<string> TableHeader,
											out Rectangle BoundingBox, 
											out List<List<string>> tableLines, 
											out XDocument xdContent) {
		// Your actual extraction implementation here
		PDF.ExtractPdfTableBelowY ContentBelowY = new(lineHeight, belowY);
		PdfTextExtractor.GetTextFromPage(pdfdoc.GetPage(1), ContentBelowY);
		tableLines = ContentBelowY.GetTableRows();
		tableLines[0] = TableHeader; // override header
		BoundingBox = ContentBelowY.GetTableBoundingBox();
		xdContent = PDF.ExtractPdfTableBelowY.ConvertToXDocument(tableLines, "PurchaseOrderItems");
	}
}
