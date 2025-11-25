using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using iText.Forms.Form.Element;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Org.BouncyCastle.Asn1.X509;
using Rectangle = iText.Kernel.Geom.Rectangle;


namespace PDF;
public class PdfTableExtractorxxx {
	public static void AddMultipleRedBorders(string inputPath, string outputPath, List<(Rectangle rect, string name)> rectangles) {
		using var reader = new PdfReader(inputPath);
		using var writer = new PdfWriter(outputPath);
		using var pdfDoc = new PdfDocument(reader, writer);
		using var document = new Document(pdfDoc);
		var page = pdfDoc.GetPage(1); // assuming page 1
		var canvas = new PdfCanvas(page);
		int pageNum = pdfDoc.GetPageNumber(page);
		foreach (var (rect, name) in rectangles) {
			AddBorder(document, canvas, pageNum, rect,name);
		}

	}

	private static void AddBorder(Document document, PdfCanvas canvas, int pageNum, Rectangle rect,string name) {
		// 1. Draw RED BORDER
		canvas
			.SetStrokeColor(ColorConstants.RED)
			.SetLineWidth(1f)
			.Rectangle(rect.GetX(), rect.GetY(), rect.GetWidth(), rect.GetHeight())
			.Stroke();

		float x1 = rect.GetX();
		float y1 = rect.GetY();
		float x2 = rect.GetRight();
		float y2 = rect.GetTop();

		PdfFont labelFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
		float labelFontSize = 7f;
		float padding = 0.5f; // 0.5f padding on each side




		// 2. BOTTOM-LEFT LABEL (outside)
		string text1 = $"({x1:F0}, {y1:F0})";
		float lblWidth = labelWidth(labelFont, labelFontSize, padding, text1);
		var label1 = new Paragraph(text1)
			.SetFontColor(ColorConstants.BLACK)
			.SetFontSize(7)
			.SetBackgroundColor(System.Drawing.Color.LightGray.ToITextColor())
			.SetPadding(1.5f)
			.SetMargin(0)
			.SetWidth(lblWidth)

			.SetFixedPosition(pageNum, x1 - 1, y1 - 12, lblWidth);// Move label slightly below and to the left of rect
		document.Add(label1);


		string text2 = $"({x2:F0}, {y2:F0})";// 3. TOP-RIGHT LABEL (above outside)
		lblWidth = labelWidth(labelFont, labelFontSize, padding+2, text1);
		var label2 = new Paragraph(text2)
			.SetFontColor(ColorConstants.BLACK)
			.SetFontSize(7)
			.SetBackgroundColor(System.Drawing.Color.LightGray.ToITextColor())
			.SetPadding(1.5f)
			.SetMargin(0)
			.SetWidth(lblWidth)
			.SetFixedPosition(pageNum, x2 - (lblWidth + 3), y2 + 2, lblWidth);
		document.Add(label2);
		string text3 = $"{name}";
		lblWidth = labelWidth(labelFont, labelFontSize, padding, text3);
		var label3 = new Paragraph(text3)
			.SetFontColor(ColorConstants.RED)
			.SetFontSize(7)
			.SetBackgroundColor(System.Drawing.Color.LightGray.ToITextColor())
			.SetPadding(0.5f)
			.SetMargin(0)
			.SetWidth(lblWidth)
			.SetFixedPosition(pageNum, rect.GetX() + (rect.GetWidth() / 2) - lblWidth / 2, y2 + 2, lblWidth);
		document.Add(label3);

		static float labelWidth(PdfFont labelFont, float labelFontSize, float padding, string text) {
			float rawFontUnits = labelFont.GetWidth(text);
			float textWidthPoints = (rawFontUnits / 1000.0f) * labelFontSize;
			float exactWidth = textWidthPoints + (padding * 2);// 3. Add padding to the final width
			return exactWidth;
		}
	}

	public static DataTable ExtractSingleTable(string pdfPath, int pageNumber, iText.Kernel.Geom.Rectangle tableArea, string tableName) {
		List<List<string>> tableRows = new();
		using (PdfDocument pdfDocument = new(new PdfReader(pdfPath))) {
			if (pageNumber > pdfDocument.GetNumberOfPages() || pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number is out of range.");
			PdfPage page = pdfDocument.GetPage(pageNumber);
			CustomLocationTextExtractionStrategy strategy = new(); // 1. Extract all text chunks on the page
			PdfTextExtractor.GetTextFromPage(page, strategy);
			Console.WriteLine($"Getting TextArea bounded by:X1={tableArea.GetX()}\tY1={tableArea.GetY()}\tX2={tableArea.GetRight()}\tY2={tableArea.GetTop()}");
			List<TextChunkInfo> filteredChunks = strategy.GetTextContent()// 2. Filter chunks to only those within the specified bounding box
				.Where(chunk =>
					chunk.Location!.GetLeft() >= tableArea.GetLeft() &&// X-AXIS (Strict Containment)
					chunk.Location.GetRight() <= tableArea.GetRight() &&// Chunk's X1 must be >= Table's X1 AND Chunk's X2 must be <= Table's X2
					chunk.Location.GetBottom() >= tableArea.GetBottom() &&// Y-AXIS (Strict Containment)
					chunk.Location.GetTop() <= tableArea.GetTop()// Chunk's Y1 must be >= Table's Y1 AND Chunk's Y2 must be <= Table's Y2
				)
						.OrderByDescending(c => c.BaseLineY) // Sort by Y (rows)
				.ThenBy(c => c.Location!.GetLeft())    // Then by X (columns)
				.ToList();
			if (filteredChunks.Any()) {// 3. Simple grouping by Y-coordinate to form rows
				float currentY = filteredChunks[0].BaseLineY;
				var currentRow = new List<TextChunkInfo>();
				const float Y_TOLERANCE = 5.0f; // Tolerance for grouping text on the same row
				foreach (var chunk in filteredChunks) {
					if (Math.Abs(chunk.BaseLineY - currentY) < Y_TOLERANCE) {
						if (currentRow.Any() && Math.Abs(chunk.Location!.GetLeft() - currentRow.Last().Location!.GetRight()) < 2) {   // Merge text chunks if they are very close horizontally (often single words)
							currentRow.Last().Text += " " + chunk.Text;
						} else {
							currentRow.Add(chunk);
						}
					} else {
						tableRows.Add(currentRow.OrderBy(c => c.Location!.GetLeft()).Select(c => c.Text).ToList());// New row detected: convert row chunks to simple strings
						currentRow = new List<TextChunkInfo> { chunk }; // Start new row
						currentY = chunk.BaseLineY;
					}
				}
				tableRows.Add(currentRow.OrderBy(c => c.Location!.GetLeft()).Select(c => c.Text).ToList());// Add the final row
			}
		}
		DataTable dt = ConvertRowsToDataTable(tableRows, tableName);// 4. Convert the list of string lists into a DataTable
		return dt;
	}
	private static DataTable ConvertRowsToDataTable(List<List<string>> tableRows, string tableName) {
		DataTable table = new(tableName);
		if (!tableRows.Any()) return table;
		List<string> header = tableRows.First();    // 1. Define Columns from the first row (Header)
		for (int i = 0; i < header.Count; i++) {
			string columnName = header[i]
				.Replace('\r', ' ').Replace('\n', ' ').Trim();
			if (string.IsNullOrEmpty(columnName)) columnName = $"Column_{i + 1}";
			table.Columns.Add(columnName, typeof(string));
		}
		foreach (var rowCells in tableRows.Skip(1)) {// 2. Add Data Rows (skipping the first row/header)
			DataRow newRow = table.NewRow();
			int columnCount = table.Columns.Count;
			for (int i = 0; i < columnCount; i++) {
				if (i < rowCells.Count) {// If rowCells has fewer items than columns, remaining cells will be default (DBNull)
					string cellValue = rowCells[i]
						.Replace('\r', ' ').Replace('\n', ' ').Trim();
					newRow[i] = cellValue;
				}
			}
			table.Rows.Add(newRow);
		}
		return table;
	}
}
public static class ColorExtensions {
	public static Color ToITextColor(this System.Drawing.Color c) {
		return new DeviceRgb(c.R, c.G, c.B);
	}
}

