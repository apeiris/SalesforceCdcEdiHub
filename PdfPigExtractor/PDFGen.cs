using System.Globalization; // For parsing currency/decimals
using System.IO;
using System.Linq;
using System.Xml.Linq; // Key namespace for XDocument
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.Xml.Serialization;
using System.Drawing;
using Color=iText.Kernel.Colors.Color;
namespace PDF;

#region data classes
[XmlRoot("PurchaseOrder")]
public class PurchaseOrder {
	public Header Header { get; set; }
	public Parties Parties { get; set; }

	[XmlArray("Items")]
	[XmlArrayItem("LineItem")]
	public List<LineItem> Items { get; set; }

	public Summary Summary { get; set; }
	public Notes Notes { get; set; }
}

public class Header {
	public string Type { get; set; }
	public string PONumber { get; set; }
	public string PODate { get; set; }
	public string Status { get; set; }
	public string DeliveryDate { get; set; }
}

public class Parties {
	public Party Buyer { get; set; }
	public Party Supplier { get; set; }
}

public class Party {
	public string Name { get; set; }
	public string Address { get; set; }
	public string Contact { get; set; }
	public string Email { get; set; }
}

public class LineItem {
	public string Item { get; set; }
	public string Code { get; set; }
	public int Quantity { get; set; }

	[XmlElement("UnitPrice")]
	public decimal UnitPrice { get; set; }
	// Add 'currency' attribute to LineItem class if you want to capture it from XML
	// For simplicity, we'll assume USD as per the PDF.

	[XmlElement("LineTotal")]
	public decimal LineTotal { get; set; }
}

public class Summary {
	public decimal Subtotal { get; set; }
	public decimal Discount { get; set; } // Note: This should be the actual amount, not the rate
	public decimal Tax { get; set; }
	public decimal Total { get; set; }
	// You may add properties for the attributes like 'rate' and 'currency' if needed
}

public class Notes {
	[XmlElement("Note")]
	public List<string> Note { get; set; }
}
#endregion
public class PDFGen {
 
	private PdfFont regularFont = PdfFontFactory.CreateFont(StandardFontFamilies.HELVETICA);
	private PdfFont boldFont =  PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
	private Color headerBgColor = new DeviceRgb(220, 220, 220); // Light Gray
	private readonly CultureInfo invariantCulture = CultureInfo.InvariantCulture;

	public void CreatePdf(string xmlFilePath, string pdfOutputPath) {
		// 1. Parse XML using XDocument and LINQ
		PurchaseOrder poData = ParseXmlWithXDocument(xmlFilePath);

		// 2. Initialize PDF Document
		using (var writer = new PdfWriter(pdfOutputPath))
		using (var pdf = new PdfDocument(writer)) {
			var document = new Document(pdf);
			document.SetMargins(30, 30, 30, 30);

			// 3. Add Content
			AddHeaderSection(document, poData);
			AddPartiesTable(document, poData.Parties);
			AddLineItemsTable(document, poData.Items);
			AddSummaryAndNotes(document, poData);

			document.Close();
		}
	}

	private PurchaseOrder ParseXmlWithXDocument(string xmlFilePath) {
		XDocument doc = XDocument.Load(xmlFilePath);
		XElement root = doc.Root;

		// Helper to safely get element value
		string GetValue(XElement parent, string elementName) => parent.Element(elementName)?.Value ?? string.Empty;

		// --- Parsing Header ---
		XElement headerXml = root.Element("Header");
		Header header = new Header {
			Type = GetValue(headerXml, "Type"),
			PONumber = GetValue(headerXml, "PONumber"),
			PODate = GetValue(headerXml, "PODate"),
			Status = GetValue(headerXml, "Status"),
			DeliveryDate = GetValue(headerXml, "DeliveryDate")
		};

		// --- Parsing Parties ---
		XElement partiesXml = root.Element("Parties");
		Parties parties = new Parties {
			Buyer = ParseParty(partiesXml.Element("Buyer")),
			Supplier = ParseParty(partiesXml.Element("Supplier"))
		};

		// --- Parsing Line Items ---
		IEnumerable<LineItem> items = root.Element("Items")
			.Elements("LineItem")
			.Select(itemXml => new LineItem {
				Item = GetValue(itemXml, "Item"),
				Code = GetValue(itemXml, "Code"),
				Quantity = int.Parse(GetValue(itemXml, "Quantity")),
				// Use decimal.Parse with InvariantCulture to handle standard numeric formats
				UnitPrice = decimal.Parse(GetValue(itemXml, "UnitPrice"), NumberStyles.Currency | NumberStyles.AllowDecimalPoint, invariantCulture),
				LineTotal = decimal.Parse(GetValue(itemXml, "LineTotal"), NumberStyles.Currency | NumberStyles.AllowDecimalPoint, invariantCulture)
			}).ToList();

		// --- Parsing Summary ---
		XElement summaryXml = root.Element("Summary");
		Summary summary = new Summary {
			Subtotal = decimal.Parse(GetValue(summaryXml, "Subtotal"), NumberStyles.Currency | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, invariantCulture),
			Discount = decimal.Parse(GetValue(summaryXml, "Discount"), NumberStyles.Currency | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, invariantCulture),
			Tax = decimal.Parse(GetValue(summaryXml, "Tax"), NumberStyles.Currency | NumberStyles.AllowDecimalPoint, invariantCulture),
			Total = decimal.Parse(GetValue(summaryXml, "Total"), NumberStyles.Currency | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, invariantCulture)
		};

		// --- Parsing Notes ---
		Notes notes = new Notes {
			Note = root.Element("Notes").Elements("Note").Select(n => n.Value).ToList()
		};


		return new PurchaseOrder {
			Header = header,
			Parties = parties,
			Items = items.ToList(),
			Summary = summary,
			Notes = notes
		};
	}

	private Party ParseParty(XElement partyXml) {
		if (partyXml == null) return new Party();

		// Helper to safely get element value
		string GetValue(XElement parent, string elementName) => parent.Element(elementName)?.Value ?? string.Empty;

		return new Party {
			Name = GetValue(partyXml, "Name"),
			Address = GetValue(partyXml, "Address"),
			Contact = GetValue(partyXml, "Contact"),
			Email = GetValue(partyXml, "Email")
		};
	}

	// --- PDF Content Building Methods (Unchanged from previous version) ---
	// (AddHeaderSection, AddHeaderCell, AddPartiesTable, AddPartyCell, 
	// AddLineItemsTable, AddSummaryAndNotes, AddSummaryRow remain the same)

	private void AddHeaderSection(Document document, PurchaseOrder poData) {
		// Title
		document.Add(new Paragraph(poData.Header.Type)
			.SetFont(boldFont).SetFontSize(24)
			.SetTextAlignment(TextAlignment.CENTER)
			.SetUnderline());

		// PO Details Table (2 columns: Label | Value)
		var poDetails = new Table(UnitValue.CreatePercentArray(new float[] { 1, 3 }))
			.SetWidth(UnitValue.CreatePercentValue(50))
			.SetMarginTop(10)
			.SetMarginBottom(10);

		AddHeaderCell(poDetails, "PO Number:", poData.Header.PONumber);
		AddHeaderCell(poDetails, "PO Date:", poData.Header.PODate);
		AddHeaderCell(poDetails, "Delivery Date:", poData.Header.DeliveryDate);
		AddHeaderCell(poDetails, "Status:", poData.Header.Status);

		document.Add(poDetails);
		document.Add(new Paragraph("\n"));
	}

	private void AddHeaderCell(Table table, string label, string value) {
		table.AddCell(new Cell().Add(new Paragraph(label).SetFont(boldFont).SetFontSize(10))
			.SetPadding(2).SetBorder(Border.NO_BORDER));
		table.AddCell(new Cell().Add(new Paragraph(value).SetFont(regularFont).SetFontSize(10))
			.SetPadding(2).SetBorder(Border.NO_BORDER));
	}

	private void AddPartiesTable(Document document, Parties parties) {
		// Buyer/Supplier Table (2 columns: Buyer | Supplier)
		var partiesTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
			.UseAllAvailableWidth();

		AddPartyCell(partiesTable, "Buyer:", parties.Buyer);
		AddPartyCell(partiesTable, "Supplier:", parties.Supplier);

		document.Add(partiesTable);
		document.Add(new Paragraph("\n"));
	}

	private void AddPartyCell(Table table, string title, Party party) {
		var cell = new Cell().SetBackgroundColor(headerBgColor).SetBorder(Border.NO_BORDER).SetPadding(5);

		cell.Add(new Paragraph(title).SetFont(boldFont).SetFontSize(12));
		cell.Add(new Paragraph(party.Name).SetFont(boldFont).SetFontSize(11));
		cell.Add(new Paragraph(party.Address).SetFont(regularFont).SetFontSize(10));
		cell.Add(new Paragraph($"Contact: {party.Contact}").SetFont(regularFont).SetFontSize(10));
		cell.Add(new Paragraph($"Email: {party.Email}").SetFont(regularFont).SetFontSize(10));

		table.AddCell(cell);
	}

	private void AddLineItemsTable(Document document, List<LineItem> items) {
		// Line Items Table
		var lineItemsTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1, 1, 1, 1 }))
			.UseAllAvailableWidth()
			.SetMarginBottom(20);

		// Header Row
		string[] headers = { "Item", "Code", "Qty", "Unit Price (USD)", "Line Total (USD)" };
		foreach (var header in headers) {
			lineItemsTable.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(boldFont).SetFontSize(10))
				.SetBackgroundColor(headerBgColor).SetTextAlignment(TextAlignment.CENTER).SetPadding(5));
		}

		// Data Rows
		foreach (var item in items) {
			lineItemsTable.AddCell(new Cell().Add(new Paragraph(item.Item).SetFont(regularFont).SetFontSize(10)).SetPadding(5));
			lineItemsTable.AddCell(new Cell().Add(new Paragraph(item.Code).SetFont(regularFont).SetFontSize(10)).SetPadding(5));
			lineItemsTable.AddCell(new Cell().Add(new Paragraph(item.Quantity.ToString()).SetFont(regularFont).SetFontSize(10)).SetTextAlignment(TextAlignment.RIGHT).SetPadding(5));
			// Format UnitPrice as currency
			lineItemsTable.AddCell(new Cell().Add(new Paragraph(item.UnitPrice.ToString("N2")).SetFont(regularFont).SetFontSize(10)).SetTextAlignment(TextAlignment.RIGHT).SetPadding(5));
			// Format LineTotal as currency
			lineItemsTable.AddCell(new Cell().Add(new Paragraph(item.LineTotal.ToString("N2")).SetFont(regularFont).SetFontSize(10)).SetTextAlignment(TextAlignment.RIGHT).SetPadding(5));
		}

		document.Add(lineItemsTable);
	}

	private void AddSummaryAndNotes(Document document, PurchaseOrder poData) {
		// Create a reliable wrapper table to align Notes (left) and Summary (right)
		// Column 1 is for Notes (wider), Column 2 is for Summary (narrower)
		var wrapperTable = new Table(UnitValue.CreatePercentArray(new float[] { 2, 1 }))
			.UseAllAvailableWidth()
			.SetMarginTop(15); // Add a small margin above the section


		// 1. Notes Cell (Left)
		var notesCell = new Cell().SetBorder(Border.NO_BORDER).SetPadding(0);
		notesCell.Add(new Paragraph("Notes:").SetFont(boldFont).SetFontSize(12).SetMarginBottom(5));

		if (poData.Notes != null && poData.Notes.Note != null) {
			foreach (var note in poData.Notes.Note) {
				notesCell.Add(new Paragraph($"- {note}").SetFont(regularFont).SetFontSize(10));
			}
		}
		// Set vertical alignment to top in case the cells have different heights
		notesCell.SetVerticalAlignment(VerticalAlignment.TOP);
		wrapperTable.AddCell(notesCell);


		// 2. Summary Table (Right)
		var summaryTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
			.SetWidth(UnitValue.CreatePercentValue(100)); // Use 100% of the containing cell width

		AddSummaryRow(summaryTable, "Subtotal:", poData.Summary.Subtotal.ToString("C2"), regularFont);
		AddSummaryRow(summaryTable, "Discount (3%):", $"-{poData.Summary.Discount.ToString("N2")}", regularFont);
		AddSummaryRow(summaryTable, "Tax (7%):", poData.Summary.Tax.ToString("N2"), regularFont);

		// Total Row - bold and highlighted
		AddSummaryRow(summaryTable, "Total:", poData.Summary.Total.ToString("C2") + " USD", boldFont, headerBgColor);

		// The Summary table is wrapped in a cell that is aligned to the right.
		var summaryCell = new Cell().SetBorder(Border.NO_BORDER).SetPadding(0)
			// This ensures the summary table aligns to the right edge of its container cell
			.SetTextAlignment(TextAlignment.RIGHT)
			.Add(summaryTable);

		wrapperTable.AddCell(summaryCell);

		document.Add(wrapperTable);
	}

	// Note: The helper method AddSummaryRow remains unchanged:
	private void AddSummaryRow(Table table, string label, string value, PdfFont font, Color bgColor = null) {
		var labelCell = new Cell().Add(new Paragraph(label).SetFont(font).SetFontSize(10))
			.SetBorder(Border.NO_BORDER).SetPadding(2).SetBackgroundColor(bgColor);
		var valueCell = new Cell().Add(new Paragraph(value).SetFont(font).SetFontSize(10))
			.SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPadding(2).SetBackgroundColor(bgColor);

		table.AddCell(labelCell);
		table.AddCell(valueCell);
	}



	//private void AddSummaryRow(Table table, string label, string value, PdfFont font, Color bgColor = null) {
	//	var labelCell = new Cell().Add(new Paragraph(label).SetFont(font).SetFontSize(10))
	//		.SetBorder(Border.NO_BORDER).SetPadding(2).SetBackgroundColor(bgColor);
	//	var valueCell = new Cell().Add(new Paragraph(value).SetFont(font).SetFontSize(10))
	//		.SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPadding(2).SetBackgroundColor(bgColor);

	//	table.AddCell(labelCell);
	//	table.AddCell(valueCell);
	//}
}

// Data classes (PurchaseOrder, Header, Parties, etc.) remain the same for structure
// but no longer need XmlSerialization attributes.
// You can remove the [XmlRoot] and other [Xml...] attributes from the original data classes
// if they are *only* being used with this XDocument approach.