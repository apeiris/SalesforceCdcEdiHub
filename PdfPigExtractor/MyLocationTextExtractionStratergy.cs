using System;
using System.Collections.Generic;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
namespace MyNamespace;

public class MyLocationTextExtractionStrategy : ITextExtractionStrategy {
	public List<TextBlock> TextBlocks { get; } = new();
	public int StrokedPathsCount { get; private set; } = 0;

	public void EventOccurred(IEventData data, EventType type) {
		switch (type) {
			case EventType.RENDER_TEXT:
				var renderInfo = (TextRenderInfo)data;

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
				break;

			case EventType.RENDER_PATH:
				var pathInfo = (PathRenderInfo)data;

				// Only consider stroked paths (lines)
				if (pathInfo.GetOperation() == PathRenderInfo.STROKE) {

					//	var lineSegments = pathInfo.GetLineSegments(); // Returns List<LineSegment>
					var lineSegments = pathInfo.GetPath(); // Returns List<LineSegment>
					var shape = pathInfo.GetPath();

					// Check if it's a rectangle shape
					StrokedPathsCount++;
				}
				
				break;

			// Optionally handle images or other events
			case EventType.RENDER_IMAGE:
				// Handle images if needed
				break;

			default:
				// Ignore other event types
				break;
		}
	}

	public ICollection<EventType> GetSupportedEvents() {
		//	return new HashSet<EventType> { EventType.RENDER_TEXT };
		return null; //apparently cause listening to all events;
	}

	public string GetResultantText() {
		return string.Join(" ", TextBlocks.ConvertAll(b => b.Text));
	}
}

public class TextBlock {
	public string Text { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
	public float Width { get; set; }
	public float Height { get; set; }
}
