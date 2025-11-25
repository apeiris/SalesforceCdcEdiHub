using System;
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
	//class ExtractBelowGlobals {
	//	public float lineHeight;
	//	public float belowY;
	//	public object? ExtractorInstance; // Will hold the instance created by the script
	//}

	public class ExtractBelowGlobals {
		public float lineHeight; 
		public float belowY ;
		public iText.Kernel.Pdf.PdfPage Page;
		// Outputs
		public List<List<string>> ResultRows ; 
		public iText.Kernel.Geom.Rectangle BoundingBox ;
		public XDocument XmlContent;
	}
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

	public async Task ProcessPdfAndMap(string pdfPath, List<string> tableHeader, string pdfMapFilePath) {
		//Rectangle boundingBox;
	//	List<List<string>> tableLines;
		
		using PdfReader pdfReader = new(pdfPath);
		using PdfDocument pdfDoc = new(pdfReader);
		XDocument mapDoc = XDocument.Load(pdfMapFilePath);
		XDocument resultDoc = new(new XElement(mapDoc.Root?.Attribute("document")?.Value ?? "Document"));
		var areas = mapDoc.Descendants("area");// Use switch on area name for maintainability and easy extension
		try {
			foreach (var area in areas) {
				string areaName = (string)area.Attribute("name")!;
				switch (areaName.ToLower()) {
					case "buyer":

						Log.Debug("Processing Buyer area");
						break;
					case "orderitems":
						var parameterList = area.Descendants("parameter")
							.Select(p => new Parameter { Name = (string)p.Attribute("name")!, Value = p.Value }).ToList();
						float scanBelowY = float.Parse(parameterList.First(p => p.Name == "scanBelowY").Value);
						float lineHeight = float.Parse(parameterList.First(p => p.Name == "lineThreshold").Value);

						//var(tLines,bbox) = await RunExtractorScriptAsync(pdfDoc.GetPage(1), lineHeight, scanBelowY);
						// ✅ CORRECT: Deconstructs the Tuple into two separate variables
						var (tableLines, boundingBox,xmlcontent) = await RunExtractorScriptAsync(pdfDoc.GetPage(1), lineHeight, scanBelowY);
						//var result = ExtractBelowY(pdfDoc, scanBelowY, lineHeight, tableHeader);

						//PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(1), extractor);
						//var tl = extractor.GetTableRows();

						//XDocument xmlResult = result.xmlContent;
						//resultDoc.Root!.Add(xmlResult.Elements());
						//XElement rowSetNode = area.Element("rowSet")!;
						//if (rowSetNode != null) {
						//	rowSetNode.RemoveNodes();
						//	foreach (XElement row in xmlResult.Root!.Elements("row")) {
						//		rowSetNode.Add(new XElement(row));
						//	}
						//} else {
						//	area.Add(
						//		new XElement("rowSet",
						//			new XAttribute("rowCount", "variable"),
						//			xmlResult.Root!.Elements("row").Select(r => new XElement(r))
						//		)
						//	);
						//}
						break;
					default:    // Do nothing or handle unknown areas as needed
						break;
				}
			}
		} catch (Exception ex) {
			Log.Error("Error processing PDF and map.");
			Log.Error(ex.Message);

			throw;
		}
	}


	// Your method (now properly named to avoid conflicts)
	private static (Rectangle BoundingBox, List<List<string>> TableLines, XDocument xmlContent)
	ExtractBelowY(PdfDocument pdfdoc, float belowY, float lineHeight, List<string> tableHeader) {
		var extractor = new PDF.ExtractPdfTableBelowY(lineHeight, belowY);
		PdfTextExtractor.GetTextFromPage(pdfdoc.GetPage(1), extractor);

		var tableLines = extractor.GetTableRows();
		if (tableLines.Count > 0)
			tableLines[0] = tableHeader;

		var boundingBox = extractor.GetTableBoundingBox();
		var xmlContent = PDF.ExtractPdfTableBelowY.ConvertToXDocument(tableLines, "OrderItems");

		return (boundingBox, tableLines, xmlContent);
	}

		

		public static async Task<(List<List<string>> Rows, Rectangle Box,XDocument xd)> RunExtractorScriptAsync(PdfPage page, float lh, float by) { 
		// 1. Get types for references
		Type extractorType = typeof(PDF.ExtractPdfTableBelowY);
		Type itextExtractorType = typeof(PdfTextExtractor); // Needed for GetTextFromPage
		Type xdocType = typeof(XDocument);
		// 2. Setup Globals with the Page
		var globals = new ExtractBelowGlobals {
			lineHeight = lh,
			belowY = by,
			Page = page,
			ResultRows = null,
			BoundingBox = null
		};

		// 3. The Script Code
		// Now the script does the work: Instantiates AND Extracts
		string scriptCode = @"
        // 1. Instantiate
        var extractor = new PDF.ExtractPdfTableBelowY(lineHeight, belowY);
        
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
				globals.XmlContent! );
	}
}

