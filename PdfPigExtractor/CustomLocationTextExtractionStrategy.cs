using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace PdfDataExtraction {
	/// <summary>
	/// A custom text extraction strategy that captures all text along with its location (bounding box).
	/// </summary>
	public class CustomLocationTextExtractionStrategy : ITextExtractionStrategy {
		private readonly List<TextChunkInfo> textContent = new();

		public ICollection<EventType> GetSupportedEvents() {
			// We only care about text rendering events for extraction
			return new List<EventType>() { EventType.RENDER_TEXT };
		}

		public void EventOccurred(IEventData data, EventType type) {
			if (type.Equals(EventType.RENDER_TEXT)) {
				TextRenderInfo renderInfo = (TextRenderInfo)data;
				string text = renderInfo.GetText();

				// 1. Define the Y-coordinates for the full height of the text block.
				// Y2 (Top): The highest point the text should reach (Ascent Line)
				float yTop = renderInfo.GetAscentLine().GetEndPoint().Get(1);

				// Y1 (Bottom): The lowest point the text can reach (Descent Line)
				float yBottom = renderInfo.GetDescentLine().GetStartPoint().Get(1);

				// Height: The difference between the highest and lowest points.
				float height = yTop - yBottom;

				// 2. Define the X-coordinates and width.
				// X1 (Left): The start point of the text baseline.
				float xLeft = renderInfo.GetBaseline().GetStartPoint().Get(0);

				// X2 (Right): The end point of the text baseline.
				float xRight = renderInfo.GetBaseline().GetEndPoint().Get(0);

				// Width: The total horizontal space occupied by the text.
				float width = xRight - xLeft;

				// 3. Get the baseline Y-coordinate for consistent row grouping (Y_BASE).
				// We use this in TextChunkInfo.BaseLineY for sorting, but YBottom for the Rectangle.
				float baseLineY = renderInfo.GetBaseline().GetStartPoint().Get(1);

				// 4. Create the final Bounding Rectangle.
				// Use the precise YBottom for the rectangle's bottom boundary.
				Rectangle adjRect = new(
					xLeft,      // X-start (Left)
					yBottom,    // Y-start (Bottom/Descent Line)
					width,      // Width
					height      // Height (YTop - YBottom)
				);
			//	Console.WriteLine($"Getting TextArea bounded by:X1={tableArea.GetX()}\tY1={tableArea.GetY()}\tX2={tableArea.GetRight()}\tY2={tableArea.GetTop()}");
				
			//	Console.WriteLine($"Adjusted bound             :X1={adjRect.GetX()}\tY1={adjRect.GetY()}\tX2={adjRect.GetRight()}\tY2={adjRect.GetTop()}");

				textContent.Add(new TextChunkInfo {
					Text = text,
					Location = adjRect,
					// Store the baseline Y separately for clean row sorting logic in the extractor.
					// This is why we created BaseLineY property in TextChunkInfo.
					BaseLineY = baseLineY
				});

			}
		}

		// ... [Rest of the class methods] ...
	


		public string GetResultantText() {
			return null!;
		}

		public ICollection<TextChunkInfo> GetTextContent() {
			return textContent;
		}
	}
}