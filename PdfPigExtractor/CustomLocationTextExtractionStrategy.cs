using System;
using System.Collections.Generic;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace PdfDataExtraction {
	/// <summary>
	/// A custom text extraction strategy that captures all text along with its location (bounding box).
	/// </summary>
	public class CustomLocationTextExtractionStrategy : ITextExtractionStrategy {
		private readonly List<TextChunkInfo> textContent = new List<TextChunkInfo>();

		public ICollection<EventType> GetSupportedEvents() {
			// We only care about text rendering events for extraction
			return new List<EventType>() { EventType.RENDER_TEXT };
		}

		public void EventOccurred(IEventData data, EventType type) {
			if (type.Equals(EventType.RENDER_TEXT)) {
				TextRenderInfo renderInfo = (TextRenderInfo)data;
				string text = renderInfo.GetText();

				// --- FIX 1: Use GetBoundingBox() instead of calculating from start/end points. ---
				// GetBoundingBox() is the simplest way to get the area occupied by the text.
				//Rectangle rect = renderInfo.GetBoundingBox();
				Rectangle rect = renderInfo.GetAscentLine().GetBoundingRectangle();

				// --- FIX 2: Rectangle objects are immutable, so we cannot use SetBBox(). ---
				// We use the coordinates to create a NEW Rectangle if we need to adjust the Y-position.

				// Get the baseline Y-coordinate for consistent row grouping.
				// We take the Y-coordinate of the start point of the text baseline.
				float baseLineY = renderInfo.GetBaseline().GetStartPoint().Get(1);

				// Create a new Rectangle using the baseline Y for consistent row detection.
				// We preserve the original width and height, but set the Y-coordinate to the baseline.
				Rectangle adjustedRect = new Rectangle(
					rect.GetLeft(),             // X-start
					baseLineY,                  // Y-start (adjusted to baseline)
					rect.GetWidth(),            // Width
					rect.GetHeight()            // Height
				);

				textContent.Add(new TextChunkInfo { Text = text, Location = adjustedRect });
			}
		}

		public string GetResultantText() {
			return null;
		}

		public ICollection<TextChunkInfo> GetTextContent() {
			return textContent;
		}
	}
}