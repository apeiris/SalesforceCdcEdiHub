using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Layout.Borders;
using iText.Barcodes;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath; // Crucial for using dataField paths like "Header/Type"
using System.Globalization;
using System.Collections.Generic;
namespace PDF;
public class PDFGen {
	private Dictionary<string, PdfFont> fontCache = new Dictionary<string, PdfFont>();
	private Dictionary<string, Color> colorCache = new Dictionary<string, Color>();
	private readonly CultureInfo invariantCulture = CultureInfo.InvariantCulture;

	// --- Core PDF Generation Method ---

	/// <summary>
	/// Creates a PDF document by combining data from the PO XML file and layout rules from the Template XML file.
	/// </summary>
	/// <param name="dataFilePath">Path to the PO data XML (e.g., POData.xml).</param>
	/// <param name="templateFilePath">Path to the template configuration XML (e.g., PurchaceOrderTemplate.xml).</param>
	/// <param name="pdfOutputPath">Path where the final PDF will be saved.</param>
	public void CreatePdf(string dataFilePath, string templateFilePath, string pdfOutputPath) {
		// 1. Load Data and Template
		XDocument dataDoc = XDocument.Load(dataFilePath);
		XDocument templateDoc = XDocument.Load(templateFilePath);

		XElement layoutXml = templateDoc.Root.Element("Layout");
		XElement stylesXml = templateDoc.Root.Element("Styles");

		// 2. Initialize Styles and Fonts
		InitializeStyles(stylesXml);

		// 3. Initialize PDF Document
		using (var writer = new PdfWriter(pdfOutputPath))
		using (var pdf = new PdfDocument(writer)) {
			var document = new Document(pdf);

			// Set margins based on template
			SetPageMargins(document, layoutXml.Element("PageMargins"));

			// 4. Iterate through Sections and build content dynamically
			foreach (var sectionXml in layoutXml.Elements("Section")) {
				string sectionName = sectionXml.Attribute("name")?.Value;

				switch (sectionName) {
					case "HeaderDetails":
						BuildHeaderDetails(document, dataDoc.Root, sectionXml);
						break;
					case "Parties":
						BuildPartiesTable(document, dataDoc.Root, sectionXml);
						break;
					case "LineItems":
						BuildLineItemsTable(document, dataDoc.Root, sectionXml);
						break;
					case "SummaryAndNotes":
						BuildSummaryAndNotes(document, dataDoc.Root, sectionXml);
						break;
					default:
						// Log unknown section
						break;
				}
			}

			document.Close();
		}
	}


	// --- TEMPLATE & STYLE HELPER METHODS ---

	private void InitializeStyles(XElement stylesXml) {
		// Initialize Fonts
		foreach (var fontElement in stylesXml.Elements("Font")) {
			string family = fontElement.Attribute("family").Value;
			string style = fontElement.Attribute("style").Value;
			string key = $"{family}-{style}";

			if (style.Contains("Bold")) {
				fontCache[key] = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
			} else {
				fontCache[key] = PdfFontFactory.CreateFont(StandardFontFamilies.HELVETICA);
			}
		}

		// Initialize Colors
		foreach (var colorElement in stylesXml.Elements("Color")) {
			string name = colorElement.Attribute("name").Value;
			string hexValue = colorElement.Attribute("value").Value.TrimStart('#');
			if (hexValue.Length == 6) {
				int r = int.Parse(hexValue.Substring(0, 2), NumberStyles.HexNumber);
				int g = int.Parse(hexValue.Substring(2, 2), NumberStyles.HexNumber);
				int b = int.Parse(hexValue.Substring(4, 2), NumberStyles.HexNumber);
				colorCache[name] = new DeviceRgb(r, g, b);
			}
		}
	}

	private void SetPageMargins(Document document, XElement marginsXml) {
		if (marginsXml != null) {
			document.SetMargins(
				float.Parse(marginsXml.Attribute("top")?.Value ?? "30"),
				float.Parse(marginsXml.Attribute("right")?.Value ?? "30"),
				float.Parse(marginsXml.Attribute("bottom")?.Value ?? "30"),
				float.Parse(marginsXml.Attribute("left")?.Value ?? "30")
			);
		}
	}

	// Helper to get font from cache based on style name
	private PdfFont GetFont(string styleName) {
		return fontCache.TryGetValue(styleName, out var font) ? font : fontCache.First().Value;
	}

	// Helper to get color from cache based on name
	private Color GetColor(string colorName) {
		if (string.IsNullOrEmpty(colorName)) return null;
		return colorCache.TryGetValue(colorName, out var color) ? color : null;
	}

	// --- PDF BUILDER METHODS (Dynamic Layout) ---

	private void BuildHeaderDetails(Document document, XElement dataRoot, XElement sectionXml) {
		var poNumber = dataRoot.XPathSelectElement("Header/PONumber")?.Value;

		// 1. Title
		var titleConfig = sectionXml.Elements("Element").FirstOrDefault(e => e.Attribute("type")?.Value == "Paragraph");
		if (titleConfig != null) {
			string titleText = dataRoot.XPathSelectElement(titleConfig.Attribute("dataField").Value)?.Value ?? string.Empty;
			string styleName = titleConfig.Attribute("style").Value;

			//document.Add(new Paragraph(titleText)
			//	.SetFont(GetFont(styleName))
			//	.SetFontSize(24)
			//	.SetTextAlignment(TextAlignment.CENTER)
			//	.SetUnderline(titleConfig.Attribute("underline")?.Value == "true"));
		
			document.Add(new Paragraph(titleText)
				.SetFont(GetFont(styleName))
				.SetFontSize(24)
				.SetTextAlignment(TextAlignment.CENTER)
				.SetUnderline());
		}

		// 2. Wrapper Table for Details and Barcode (2 columns, 2:1 width)
		var headerWrapper = new Table(UnitValue.CreatePercentArray(new float[] { 2, 1 }))
			.UseAllAvailableWidth()
			.SetMarginTop(10)
			.SetMarginBottom(20);

		// --- A. PO Details Table (Left Cell) ---
		var tableConfig = sectionXml.Elements("Element").FirstOrDefault(e => e.Attribute("type")?.Value == "Table");
		var poDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 3 }))
			.SetWidth(UnitValue.CreatePercentValue(100))
			.SetBorder(Border.NO_BORDER);

		if (tableConfig != null) {
			foreach (var rowConfig in tableConfig.Elements("Row")) {
				var cellConfig = rowConfig.Element("Cell");
				string label = cellConfig.Attribute("label").Value;
				string dataField = cellConfig.Attribute("dataField").Value;
				string styleName = cellConfig.Attribute("style").Value;
				string value = dataRoot.XPathSelectElement(dataField)?.Value ?? string.Empty;

				poDetailsTable.AddCell(new Cell().Add(new Paragraph(label).SetFont(GetFont(styleName)).SetFontSize(10))
					.SetPadding(2).SetBorder(Border.NO_BORDER));
				poDetailsTable.AddCell(new Cell().Add(new Paragraph(value).SetFont(GetFont("Helvetica-Regular")).SetFontSize(10))
					.SetPadding(2).SetBorder(Border.NO_BORDER));
			}
		}
		headerWrapper.AddCell(new Cell().SetBorder(Border.NO_BORDER).Add(poDetailsTable));

		// --- B. Barcode Cell (Right Cell) ---
		var barcodeConfig = sectionXml.Elements("Element").FirstOrDefault(e => e.Attribute("type")?.Value == "Barcode");
		if (barcodeConfig != null && !string.IsNullOrEmpty(poNumber)) {
			Image barcodeImage = GenerateBarcodeImage(document.GetPdfDocument(), poNumber, barcodeConfig);

			headerWrapper.AddCell(new Cell()
				.SetBorder(Border.NO_BORDER)
				.SetVerticalAlignment(VerticalAlignment.MIDDLE)
				.SetTextAlignment(TextAlignment.RIGHT)
				.Add(barcodeImage));
		}

		document.Add(headerWrapper);
	}

	private void BuildPartiesTable(Document document, XElement dataRoot, XElement sectionXml) {
		var tableConfig = sectionXml.Elements("Element").FirstOrDefault(e => e.Attribute("type")?.Value == "Table");
		if (tableConfig == null) return;

		// Create the Parties Table
		var partiesTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
			.UseAllAvailableWidth();

		// Iterate over columns (Buyer and Supplier)
		foreach (var columnConfig in tableConfig.Elements("Column")) {
			string headerText = columnConfig.Attribute("headerText").Value;
			string dataPath = columnConfig.Attribute("dataPath").Value;
			Color bgColor = GetColor(columnConfig.Attribute("backgroundColor")?.Value);

			XElement partyData = dataRoot.XPathSelectElement(dataPath);

			var cell = new Cell().SetBackgroundColor(bgColor).SetBorder(Border.NO_BORDER).SetPadding(5);

			// Add Title (Buyer/Supplier)
			cell.Add(new Paragraph(headerText).SetFont(GetFont("Helvetica-Bold")).SetFontSize(12));

			// Add Fields (Name, Address, Contact, Email)
			foreach (var fieldConfig in columnConfig.Elements("Field")) {
				string dataField = fieldConfig.Attribute("dataField").Value;
				string styleName = fieldConfig.Attribute("style").Value;
				string prefix = fieldConfig.Attribute("prefix")?.Value ?? string.Empty;
				string value = partyData.Element(dataField)?.Value ?? string.Empty;

				PdfFont font = GetFont(styleName);
				if (dataField == "Name") font = GetFont("Header"); // Use header style for Name

				cell.Add(new Paragraph(prefix + value).SetFont(font).SetFontSize(10));
			}

			partiesTable.AddCell(cell);
		}

		document.Add(partiesTable);
		document.Add(new Paragraph("\n"));
	}

	private void BuildLineItemsTable(Document document, XElement dataRoot, XElement sectionXml) {
		var tableConfig = sectionXml.Elements("Element").FirstOrDefault(e => e.Attribute("type")?.Value == "Table");
		if (tableConfig == null) return;

		string dataPath = tableConfig.Attribute("dataPath").Value; // Items/LineItem

		// Get column definitions for width calculation
		var columnConfigs = tableConfig.Elements("Column").ToList();
		float[] columnWidths = columnConfigs.Select(c => float.Parse(c.Attribute("widthWeight").Value)).ToArray();

		var lineItemsTable = new Table(UnitValue.CreatePercentArray(columnWidths))
			.UseAllAvailableWidth()
			.SetMarginBottom(20);

		Color headerBgColor = GetColor(tableConfig.Attribute("headerColor")?.Value);

		// Header Row
		foreach (var columnConfig in columnConfigs) {
			lineItemsTable.AddHeaderCell(new Cell().Add(new Paragraph(columnConfig.Attribute("label").Value).SetFont(GetFont("Label")).SetFontSize(10))
				.SetBackgroundColor(headerBgColor).SetTextAlignment(TextAlignment.CENTER).SetPadding(5));
		}

		// Data Rows
		var itemsData = dataRoot.XPathSelectElements(dataPath);
		foreach (var itemXml in itemsData) {
			foreach (var columnConfig in columnConfigs) {
				string dataField = columnConfig.Attribute("dataField").Value;
				string alignment = columnConfig.Attribute("alignment").Value;
				string format = columnConfig.Attribute("format")?.Value;

				string rawValue = itemXml.Element(dataField)?.Value ?? string.Empty;
				string displayValue = rawValue;
				TextAlignment textAlignment = alignment == "Right" ? TextAlignment.RIGHT : TextAlignment.LEFT;

				if (format != null && decimal.TryParse(rawValue.Replace(",", ""), NumberStyles.Currency | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, invariantCulture, out decimal dValue)) {
					// Format currency/number if specified
					string formatString = format.Split(':')[1];
					displayValue = dValue.ToString(formatString, invariantCulture);
				}

				lineItemsTable.AddCell(new Cell().Add(new Paragraph(displayValue).SetFont(GetFont("Body")).SetFontSize(10))
					.SetTextAlignment(textAlignment).SetPadding(5));
			}
		}

		document.Add(lineItemsTable);
	}

	private void BuildSummaryAndNotes(Document document, XElement dataRoot, XElement sectionXml) {
		var wrapperConfig = sectionXml.Elements("Element").FirstOrDefault(e => e.Attribute("type")?.Value == "Table");
		if (wrapperConfig == null) return;

		// Initialize wrapper table for Notes (left) and Summary (right)
		var wrapperTable = new Table(UnitValue.CreatePercentArray(new float[] { 2, 1 }))
			.UseAllAvailableWidth()
			.SetMarginTop(15);

		// 1. Notes Cell (Left Column)
		var notesColumnConfig = wrapperConfig.Elements("Column").ElementAtOrDefault(0);
		var notesCell = new Cell().SetBorder(Border.NO_BORDER).SetPadding(0).SetVerticalAlignment(VerticalAlignment.TOP);

		// Add Notes Label
		notesCell.Add(new Paragraph(notesColumnConfig.Element("Label").Attribute("text").Value)
			.SetFont(GetFont("Header")).SetFontSize(12).SetMarginBottom(5));

		// Add Notes List
		var listConfig = notesColumnConfig.Element("List");
		if (listConfig != null) {
			string dataPath = listConfig.Attribute("dataPath").Value; // Notes/Note
			string prefix = listConfig.Attribute("prefix")?.Value ?? string.Empty;

			var notes = dataRoot.XPathSelectElements(dataPath).Select(n => n.Value);

			foreach (var note in notes) {
				notesCell.Add(new Paragraph($"{prefix}{note}").SetFont(GetFont("Body")).SetFontSize(10));
			}
		}
		wrapperTable.AddCell(notesCell);

		// 2. Summary Table (Right Column)
		var summaryCell = new Cell().SetBorder(Border.NO_BORDER).SetPadding(0).SetTextAlignment(TextAlignment.RIGHT);

		var summaryTableConfig = wrapperConfig.Elements("Column").ElementAtOrDefault(1).Element("Element");
		var summaryTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
			.SetWidth(UnitValue.CreatePercentValue(100));

		// Iterate over Summary Rows
		foreach (var rowConfig in summaryTableConfig.Elements("SummaryRow")) {
			string label = rowConfig.Attribute("label").Value;
			string dataField = rowConfig.Attribute("dataField").Value;
			string format = rowConfig.Attribute("format").Value;
			string prefix = rowConfig.Attribute("prefix")?.Value ?? string.Empty;
			string suffix = rowConfig.Attribute("suffix")?.Value ?? string.Empty;
			string style = rowConfig.Attribute("style").Value;
			string bgColorName = rowConfig.Attribute("backgroundColor")?.Value;

			string rawValue = dataRoot.XPathSelectElement(dataField)?.Value.Replace(",", "") ?? "0";

			if (decimal.TryParse(rawValue, NumberStyles.Currency | NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, invariantCulture, out decimal dValue)) {
				string formatString = format.Split(':')[1];
				string formattedValue = prefix + dValue.ToString(formatString, invariantCulture) + suffix;

				Color bgColor = GetColor(bgColorName);
				PdfFont font = GetFont(style); // Note: Assuming "Header" style maps to Bold font

				AddSummaryRow(summaryTable, label, formattedValue, font, bgColor);
			}
		}

		summaryCell.Add(summaryTable);
		wrapperTable.AddCell(summaryCell);

		document.Add(wrapperTable);
	}

	private void AddSummaryRow(Table table, string label, string value, PdfFont font, Color bgColor) {
		var labelCell = new Cell().Add(new Paragraph(label).SetFont(font).SetFontSize(10))
			.SetBorder(Border.NO_BORDER).SetPadding(2).SetBackgroundColor(bgColor);
		var valueCell = new Cell().Add(new Paragraph(value).SetFont(font).SetFontSize(10))
			.SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPadding(2).SetBackgroundColor(bgColor);

		table.AddCell(labelCell);
		table.AddCell(valueCell);
	}

	private Image GenerateBarcodeImage(PdfDocument pdf, string code, XElement config) {
		Barcode128 barcode = new Barcode128(pdf);

		barcode.SetCode(code);
		barcode.SetBarHeight(float.Parse(config.Attribute("barHeight")?.Value ?? "40"));
		barcode.SetX(1.0f); // Module width

		Image barcodeImage = new Image(barcode.CreateFormXObject(ColorConstants.BLACK, ColorConstants.BLACK, pdf))
			.ScaleToFit(float.Parse(config.Attribute("scaleX")?.Value ?? "150"),
						float.Parse(config.Attribute("scaleY")?.Value ?? "60"));

		return barcodeImage;
	}
}