using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using iText.IO.Util;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using NLog;
namespace PDF;
public class XmlMapProcessor {
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	public void ProcessPdfAndMap(string pdfPath, List<string> tableHeader, string pdfMapFilePath) {
		Rectangle boundingBox;
		List<List<string>> tableLines;
		XDocument xdContent;
		using PdfReader pdfReader = new(pdfPath);
		using PdfDocument pdfDoc = new(pdfReader);
		XDocument mapDoc = XDocument.Load(pdfMapFilePath);
		XDocument resultDoc = new(new XElement(mapDoc.Root?.Attribute("document")?.Value ?? "Document"));


		var areas = mapDoc.Descendants("area");// Use switch on area name for maintainability and easy extension
		foreach (var area in areas) {
			string areaName = (string)area.Attribute("name")!;
			switch (areaName) {
				case "Buyer":

						Log.Debug("Processing Buyer area");
					break;
				case "PurchaseOrderItems":
					float scanBelowY = float.Parse((string)area.Attribute("scanBelowY")!);
					float lineHeight = float.Parse((string)area.Attribute("lineHeight")!);
					//ExtractTableBelowY(pdfDoc, scanBelowY, lineHeight, tableHeader, out boundingBox, out tableLines, out xdContent);
					var result = ExtractTableBelowY(pdfDoc, scanBelowY, lineHeight, tableHeader);
					XDocument xmlResult = result.xmlContent;
					
				

					resultDoc.Root!.Add(xmlResult.Elements());


					XElement rowSetNode = area.Element("rowSet")!;
					if (rowSetNode != null) {
						rowSetNode.RemoveNodes();
						foreach (XElement row in xmlResult.Root!.Elements("row")) {
							rowSetNode.Add(new XElement(row));
						}
					} else {
						area.Add(
							new XElement("rowSet",
								new XAttribute("rowCount", "variable"),
								xmlResult.Root!.Elements("row").Select(r => new XElement(r))
							)
						);
					}
					break;
				default:    // Do nothing or handle unknown areas as needed
					break;
			}
		}
	}
	private static (Rectangle BoundingBox, List<List<string>> TableLines, XDocument xmlContent) ExtractTableBelowY(PdfDocument pdfdoc,
											float belowY,
											float lineHeight,
											List<string> TableHeader) {
		Rectangle BoundingBox;
		List<List<string>> tableLines;
		XDocument xmlContent;
		// Your actual extraction implementation here
		PDF.ExtractPdfTableBelowY ContentBelowY = new(lineHeight, belowY);
		PdfTextExtractor.GetTextFromPage(pdfdoc.GetPage(1), ContentBelowY);
		tableLines = ContentBelowY.GetTableRows();
		tableLines[0] = TableHeader; // override header
		BoundingBox = ContentBelowY.GetTableBoundingBox();
		xmlContent = PDF.ExtractPdfTableBelowY.ConvertToXDocument(tableLines, "OrderItems");
		return (BoundingBox, tableLines, xmlContent);
	}
}
