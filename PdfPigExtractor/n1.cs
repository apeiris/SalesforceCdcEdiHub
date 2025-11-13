using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;



using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace PdfTableExtractor;

public class TextBlock {
	public string Text { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
	public float Width { get; set; }
	public float Height { get; set; }
}

public class TableRectangle {
	public int Index { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
	public float Width { get; set; }
	public float Height { get; set; }
}

/// <summary>
/// Custom text extraction strategy to get text blocks with positions
/// </summary>
public class MyLocationTextExtractionStrategy : ITextExtractionStrategy {
	public List<TextBlock> TextBlocks { get; } = new List<TextBlock>();

	public void EventOccurred(IEventData data, EventType type) {
		if (type != EventType.RENDER_TEXT) return;

		var renderInfo = (TextRenderInfo)data;

		// Get ascent and descent lines
		var ascent = renderInfo.GetAscentLine().GetBoundingRectangle();
		var descent = renderInfo.GetDescentLine().GetBoundingRectangle();

		float x = (float)descent.GetX();
		float y = (float)descent.GetY();
		float width = (float)ascent.GetX() + (float)ascent.GetWidth() - x;
		float height = (float)ascent.GetY() + (float)ascent.GetHeight() - y;

		TextBlocks.Add(new TextBlock {
			Text = renderInfo.GetText(),
			X = x,
			Y = y,
			Width = width,
			Height = height
		});

	}

	public ICollection<EventType> GetSupportedEvents() {
		return new HashSet<EventType> { EventType.RENDER_TEXT };
	}

	public string GetResultantText() {
		return string.Join(" ", TextBlocks.ConvertAll(b => b.Text));
	}
}

public class PdfTableParser {
	/// <summary>
	/// Extract text blocks and table rectangles from a PDF file
	/// </summary>
	/// <param name="pdfPath">Path to PDF</param>
	/// <param name="rowTolerance">Vertical distance tolerance to group rows</param>
	/// <returns>Tuple of text blocks and table rectangles</returns>
	public static (List<TextBlock> TextBlocks, List<TableRectangle> Tables) ExtractTables(string pdfPath, float rowTolerance = 5f) {
		if (!File.Exists(pdfPath))
			throw new FileNotFoundException("PDF file not found", pdfPath);

		var textBlocks = new List<TextBlock>();
		var tables = new List<TableRectangle>();

		using (var pdfDoc = new PdfDocument(new PdfReader(pdfPath))) {
			for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++) {
				var page = pdfDoc.GetPage(i);
				var strategy = new MyLocationTextExtractionStrategy();
				PdfTextExtractor.GetTextFromPage(page, strategy);

				textBlocks.AddRange(strategy.TextBlocks);

				// --- Simple Table Detection ---
				var sorted = strategy.TextBlocks.OrderByDescending(tb => tb.Y).ToList();
				var currentRow = new List<TextBlock>();

				foreach (var tb in sorted) {
					if (!currentRow.Any()) {
						currentRow.Add(tb);
						continue;
					}

					var prev = currentRow.Last();
					if (Math.Abs(prev.Y - tb.Y) <= rowTolerance) {
						currentRow.Add(tb);
					} else {
						if (currentRow.Any())
							tables.Add(GetBoundingRectangle(currentRow));
						currentRow = new List<TextBlock> { tb };
					}
				}
				if (currentRow.Any())
					tables.Add(GetBoundingRectangle(currentRow));
			}
		}

		return (textBlocks, tables);
	}

	private static TableRectangle GetBoundingRectangle(List<TextBlock> blocks) {
		float minX = blocks.Min(b => b.X);
		float maxX = blocks.Max(b => b.X + b.Width);
		float minY = blocks.Min(b => b.Y);
		float maxY = blocks.Max(b => b.Y + b.Height);

		return new TableRectangle {
			X = minX,
			Y = minY,
			Width = maxX - minX,
			Height = maxY - minY
		};
	}
	public static List<TableRectangle> MergeRowsIntoTables(List<TableRectangle> rows, int nextIndex, float rowTolerance = 5f) {
		// Sort rows from top to bottom (descending Y)
		var sorted = rows.OrderByDescending(r => r.Y).ToList();
		var mergedTables = new List<TableRectangle>();

		TableRectangle currentTable = null;
		int tblIndex = nextIndex;
		foreach (var row in sorted) {
			if (currentTable == null) {
				currentTable = new TableRectangle {
					X = row.X,
					Y = row.Y,
					Width = row.Width,
					Height = row.Height,
					Index = tblIndex
				};
			} else {// Check vertical adjacency (Y coordinate difference)
				float verticalGap = currentTable.Y - (row.Y + row.Height);
				if (Math.Abs(verticalGap) <= rowTolerance) {// Expand table rectangle to include this row
					float minX = Math.Min(currentTable.X, row.X);
					float maxX = Math.Max(currentTable.X + currentTable.Width, row.X + row.Width);
					float maxY = Math.Max(currentTable.Y + currentTable.Height, row.Y + row.Height);
					currentTable.X = minX;
					currentTable.Y = row.Y; // bottom-most row
					currentTable.Width = maxX - minX;
					currentTable.Height = maxY - currentTable.Y;
					currentTable.Index = tblIndex;
				} else {
					mergedTables.Add(currentTable);
					currentTable = new TableRectangle {
						X = row.X,
						Y = row.Y,
						Width = row.Width,
						Height = row.Height,
						Index = tblIndex
					};
					tblIndex++;
				}
			}
		}
		if (currentTable != null) {
			
			currentTable.Index = tblIndex;
			mergedTables.Add(currentTable);
		}
		return mergedTables;
	}
}
