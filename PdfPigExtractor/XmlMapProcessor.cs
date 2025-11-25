using System;
using System.ComponentModel;
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
		public float belowY;
		public iText.Kernel.Pdf.PdfPage Page;
		// Outputs
		public List<List<string>> ResultRows;
		public iText.Kernel.Geom.Rectangle BoundingBox;
		public XDocument XmlContent;
	}
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	static XElement AddAndReturn(XElement root, string name) {
		var el = new XElement(name);
		root.Add(el);
		return el;
	}
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
				string parentName = (string)area.Attribute("parent")!;
				switch (areaName.ToLower()) {
					case "buyer":
						XElement parties = resultDoc.Root!.Element(parentName) != null	? resultDoc.Root.Element(parentName)!: AddAndReturn(resultDoc.Root, parentName);
						
						Log.Debug("Processing Buyer area");
						break;
					case "orderitems":
						var parameterList = area.Descendants("parameter")
							.Select(p => new Parameter { Name = (string)p.Attribute("name")!, Value = p.Value }).ToList();
						float scanBelowY = float.Parse(parameterList.First(p => p.Name == "scanBelowY").Value);
						float lineHeight = float.Parse(parameterList.First(p => p.Name == "lineThreshold").Value);

						var (tableLines, boundingBox, xmlcontent) = await RunExtractorScriptAsync(pdfDoc.GetPage(1), lineHeight, scanBelowY);
						resultDoc.Root!.Add(xmlcontent.Root!.Elements());
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
				globals.XmlContent!);
	}
}

