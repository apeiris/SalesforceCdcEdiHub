using System.Collections.Generic;
using System.Data;
using System.Linq;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace PdfDataExtraction {
	public class PdfTableExtractor {
		/// <summary>
		/// Extracts data from all tables in a PDF document based on coordinate analysis.
		/// </summary>
		/// <param name="pdfPath">The full path to the PDF file.</param>
		/// <returns>A list of tables, where each table is a list of rows, and each row is a list of cell strings.</returns>
		public static List<List<List<string>>> ExtractAllTables(string pdfPath) {
			var allExtractedTables = new List<List<List<string>>>();

			using (PdfDocument pdfDocument = new(new PdfReader(pdfPath))) {
				for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++) {
					PdfPage page = pdfDocument.GetPage(pageNum);

					// 1. Extract all text chunks with their locations
					CustomLocationTextExtractionStrategy strategy = new();
					PdfTextExtractor.GetTextFromPage(page, strategy);

					List<TextChunkInfo> textChunks = strategy.GetTextContent()
															  .OrderByDescending(c => c.BaseLineY) // Sort by Y for rows
															  .ThenBy(c => c.Location.GetLeft())    // Then by X for columns
															  .ToList();

					// --- 2. Implement Table Inference Logic Here ---
					// This is the part that requires custom logic based on the PDF's layout.
					// The inference logic involves:
					// a. Grouping chunks that have a similar BaseLineY (e.g., within 5 units) to form a Row.
					// b. Analyzing the X-coordinates within each group (Row) to identify distinct Columns.
					// c. Assembling the structured data.

					// Example: Simple grouping by Y-coordinate

					var rows = new List<List<TextChunkInfo>>();
					if (textChunks.Any()) {
						float currentY = textChunks[0].BaseLineY;
						var currentRow = new List<TextChunkInfo>();

						foreach (var chunk in textChunks) {
							// If the chunk is close to the current baseline Y, add it to the row.
							// 5 units is an arbitrary tolerance that often works for standard fonts.
							if (System.Math.Abs(chunk.BaseLineY - currentY) < 5) {
								currentRow.Add(chunk);
							} else {
								// New row detected
								rows.Add(currentRow.OrderBy(c => c.Location.GetLeft()).ToList());
								currentRow = new List<TextChunkInfo> { chunk };
								currentY = chunk.BaseLineY;
							}
						}
						rows.Add(currentRow.OrderBy(c => c.Location.GetLeft()).ToList());
					}

					// --- 3. Convert Chunks to Table Format (Placeholder) ---
					var pageTables = new List<List<string>>();
					foreach (var rowChunks in rows) {
						// In a real scenario, you'd match the chunk X-coordinates against column boundaries
						// to ensure empty cells are preserved. For this simple example, we just join the text.
						pageTables.Add(rowChunks.Select(c => c.Text).ToList());
					}

					// This simple logic will likely combine non-table text (like 'PURCHASE ORDER') 
					// into single-cell "rows". You'll need area filtering to isolate true tables.
					allExtractedTables.Add(pageTables);
				}
			}

			return allExtractedTables;
		}
		public static List<string> FormatTablesAsCsv(List<List<List<string>>> allExtractedTables) {
			var csvRows = new List<string>();

			foreach (var pageTables in allExtractedTables) {
				// Assuming the simple inference logic returned a list of rows for the page
				foreach (var rowCells in pageTables) {
					// Escape and quote values, then join them by a comma
					var formattedCells = rowCells.Select(cell => {
						// Clean up common PDF extraction artifacts like extra spaces or newlines
						string cleanedCell = cell.Replace('\r', ' ').Replace('\n', ' ').Trim();

						// Basic CSV escaping: double quotes (") in the data must be escaped as ("")
						if (cleanedCell.Contains("\"") || cleanedCell.Contains(",")) {
							return $"\"{cleanedCell.Replace("\"", "\"\"")}\"";
						}
						return cleanedCell;
					});

					csvRows.Add(string.Join(",", formattedCells));
				}

				// Add a separator for tables/pages if needed
				// csvRows.Add("--- END OF PAGE/TABLE ---"); 
			}

			return csvRows;
		}

		public static void DrawRectangle(
			string sourcePdfPath,
			string destPdfPath,
			int pageNumber,
			iText.Kernel.Geom.Rectangle highlightArea) {
			// 1. Ensure the destination path's directory exists
			string directory = Path.GetDirectoryName(destPdfPath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
				Directory.CreateDirectory(directory);
			}

			// Attempt to delete the file one last time to break locks
			if (File.Exists(destPdfPath)) {
				try { File.Delete(destPdfPath); } catch (Exception) { /* Swallow delete exception if locked, hoping FileShare.None works */ }
			}

			// Use PdfReader for the source file
			using (PdfReader reader = new PdfReader(sourcePdfPath)) {
				// 👇 THE FIX: Create the FileStream explicitly with exclusive access (FileShare.None)
				using (FileStream fs = new FileStream(
					destPdfPath,
					FileMode.Create, // Create a new file or overwrite existing
					FileAccess.Write,
					FileShare.None)) // Crucial: Prevents any other process from opening the file
				{
					// Pass the stream to the PdfWriter
					using (PdfWriter writer = new PdfWriter(fs)) {
						using (PdfDocument pdfDocument = new PdfDocument(reader, writer)) {
							// ... (rest of the drawing logic) ...
							PdfPage page = pdfDocument.GetPage(pageNumber);
							PdfCanvas canvas = new PdfCanvas(page);

							canvas
								.SetStrokeColor(new DeviceRgb(255, 0, 0))
								.SetLineWidth(1.0f)
								.Rectangle(highlightArea.GetLeft(), highlightArea.GetBottom(),
										   highlightArea.GetWidth(), highlightArea.GetHeight())
								.Stroke();
						}
					}
				}
			}
		}


		public static void AddRedBorderToPdf(string inputPath, string outputPath, iText.Kernel.Geom.Rectangle rect) {
			using var reader = new PdfReader(inputPath);
			using var writer = new PdfWriter(outputPath);
			using var pdfDoc = new PdfDocument(reader, writer);
			using var document = new Document(pdfDoc);

			var page = pdfDoc.GetFirstPage();
			var canvas = new PdfCanvas(page);

			// Set border properties
			canvas
				.SetStrokeColor(ColorConstants.RED)
				.SetLineWidth(1f)           // thickness in points
				.SetLineDash(0)             // solid line (no dash)
				.Rectangle(rect.GetX(), rect.GetY(), rect.GetWidth(), rect.GetHeight())
				.Stroke();

			// Optional: Add a label inside the box
			string coordsText = $"({rect.GetX():F0}, {rect.GetY():F0}) {rect.GetWidth():F0}×{rect.GetHeight():F0}";
			var label = new Paragraph(coordsText)
				.SetFontColor(ColorConstants.WHITE)
				.SetFontSize(8)                    // Small text
				.SetBackgroundColor(ColorConstants.RED)
				.SetPadding(2)
				.SetMargin(0);

			// Position: 5pt from bottom-left corner of rectangle
			float textX = rect.GetX() + 5;
			float textY = rect.GetY() + 5;

			document.ShowTextAligned(
				label,
				textX,
				textY,
				pdfDoc.GetPageNumber(page),
				TextAlignment.LEFT,
				VerticalAlignment.BOTTOM,
				0
			);
		}



		public static DataTable ExtractSingleTable(string pdfPath, int pageNumber, iText.Kernel.Geom.Rectangle tableArea, string tableName) {
			List<List<string>> tableRows = new();
			using (PdfDocument pdfDocument = new(new PdfReader(pdfPath))) {
				if (pageNumber > pdfDocument.GetNumberOfPages() || pageNumber < 1) {
					throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number is out of range.");
				}
				PdfPage page = pdfDocument.GetPage(pageNumber);
				CustomLocationTextExtractionStrategy strategy = new(); // 1. Extract all text chunks on the page
				PdfTextExtractor.GetTextFromPage(page, strategy);
				//Console.WriteLine($"Getting TextArea bounded by:X1={tableArea.}}\\tY1={{tableArea.GetBottom()}}\\tX2={{tableArea.GetRight()}}\\tY2={{tableArea.GetTop()}}\");
				Console.WriteLine($"Getting TextArea bounded by:X1={tableArea.GetX()}\tY1={tableArea.GetY()}\tX2={tableArea.GetRight()}\tY2={tableArea.GetTop()}");



				List<TextChunkInfo> filteredChunks = strategy.GetTextContent()// 2. Filter chunks to only those within the specified bounding box
					.Where(chunk =>
						chunk.Location.GetLeft() >= tableArea.GetLeft() &&// X-AXIS (Strict Containment)
						chunk.Location.GetRight() <= tableArea.GetRight() &&// Chunk's X1 must be >= Table's X1 AND Chunk's X2 must be <= Table's X2
						chunk.Location.GetBottom() >= tableArea.GetBottom() &&// Y-AXIS (Strict Containment)
						chunk.Location.GetTop() <= tableArea.GetTop()// Chunk's Y1 must be >= Table's Y1 AND Chunk's Y2 must be <= Table's Y2
					)
							.OrderByDescending(c => c.BaseLineY) // Sort by Y (rows)
					.ThenBy(c => c.Location.GetLeft())    // Then by X (columns)
					.ToList();
				if (filteredChunks.Any()) {// 3. Simple grouping by Y-coordinate to form rows
					float currentY = filteredChunks[0].BaseLineY;
					var currentRow = new List<TextChunkInfo>();
					const float Y_TOLERANCE = 5.0f; // Tolerance for grouping text on the same row
					foreach (var chunk in filteredChunks) {
						if (Math.Abs(chunk.BaseLineY - currentY) < Y_TOLERANCE) {
							if (currentRow.Any() && Math.Abs(chunk.Location.GetLeft() - currentRow.Last().Location.GetRight()) < 2) {   // Merge text chunks if they are very close horizontally (often single words)
								currentRow.Last().Text += " " + chunk.Text;
							} else {
								currentRow.Add(chunk);
							}
						} else {

							tableRows.Add(currentRow.OrderBy(c => c.Location.GetLeft()).Select(c => c.Text).ToList());// New row detected: convert row chunks to simple strings
							currentRow = new List<TextChunkInfo> { chunk }; // Start new row
							currentY = chunk.BaseLineY;
						}
					}

					tableRows.Add(currentRow.OrderBy(c => c.Location.GetLeft()).Select(c => c.Text).ToList());// Add the final row
				}
			}


			DataTable dt = ConvertRowsToDataTable(tableRows, tableName);// 4. Convert the list of string lists into a DataTable


			return dt;
		}

		// --- Helper Method to Convert List<List<string>> to DataTable ---
		private static DataTable ConvertRowsToDataTable(List<List<string>> tableRows, string tableName) {
			DataTable table = new(tableName);

			if (!tableRows.Any()) return table;

			// 1. Define Columns from the first row (Header)
			List<string> header = tableRows.First();
			for (int i = 0; i < header.Count; i++) {
				string columnName = header[i]
					.Replace('\r', ' ').Replace('\n', ' ').Trim();

				if (string.IsNullOrEmpty(columnName)) columnName = $"Column_{i + 1}";

				table.Columns.Add(columnName, typeof(string));
			}

			// 2. Add Data Rows (skipping the first row/header)
			foreach (var rowCells in tableRows.Skip(1)) {
				DataRow newRow = table.NewRow();
				int columnCount = table.Columns.Count;

				for (int i = 0; i < columnCount; i++) {
					// If rowCells has fewer items than columns, remaining cells will be default (DBNull)
					if (i < rowCells.Count) {
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
}
