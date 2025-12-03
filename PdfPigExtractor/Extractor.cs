using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Geom;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NLog;

namespace PDF;
// Custom strategy that stops on gap > 10pt
public class StopOnLargeGapStrategy : ITextExtractionStrategy {
	private readonly float _startX;
	private readonly float _startY;
	private readonly float _gapThreshold;
	private readonly float _scanWidth;

	private bool started = false;
	private bool stopped = false;
	private float lastBaselineY = 0f;

	// We collect chunks manually for full control
	private readonly List<TextChunk> chunks = new List<TextChunk>();
	public StopOnLargeGapStrategy(float startX, float startY, float scanWidth, float gapThreshold) {
		_startX = startX;
		_startY = startY;
		_scanWidth = scanWidth;
		_gapThreshold = gapThreshold;

	}


	public void EventOccurred(IEventData data, EventType type) {
		if (stopped) return;
		if (type == EventType.RENDER_TEXT) {
			TextRenderInfo tri = (TextRenderInfo)data;
			Vector baselineStart = tri.GetBaseline().GetStartPoint();// Baseline start point
			float curX = baselineStart.Get(Vector.I1);
			float curY = baselineStart.Get(Vector.I2);
			if (!started) {// 1. Wait until we are in the target region
				if (curX >= _startX && curY <= _startY) {
					started = true;
				} else {
					return; // ignore everything before the region
				}
			}
			if (lastBaselineY > 0) {// 2. If we have a previous line, check vertical gap
				float gap = lastBaselineY - curY; // PDF coordinates go bottom-up
				if (gap > _gapThreshold) {// stop forever
					stopped = true;
					return;
				}
			}
			lastBaselineY = curY;// Update last seen baseline (we process top-down, so higher Y = earlier)
			var inXlimits = curX >= _startX && curX <= _startX + _scanWidth;
			//if (curX <= _startX + _scanWidth) // include only if within X limits
			Console.WriteLine($"Text: '{tri.GetText()}' at ({curX},{curY}) Inxlimits={inXlimits}");
			if (inXlimits) {
				
				chunks.Add(new TextChunk(tri.GetText(), baselineStart, tri.GetTextRenderMode()));// Store the chunk
			}
		}
	}

	public string GetResultantText() {
		if (chunks.Count == 0) return "";

		// Sort: highest Y first (top of page), then left to right
		chunks.Sort((a, b) => {
			int yCompare = b.StartPoint.Get(Vector.I2).CompareTo(a.StartPoint.Get(Vector.I2));
			if (yCompare != 0) return yCompare; // descending Y
			return a.StartPoint.Get(Vector.I1).CompareTo(b.StartPoint.Get(Vector.I1));
		});

		StringBuilder sb = new StringBuilder();
		float tolerance = 18.0f;
		float lastY = -9999f;

		foreach (var chunk in chunks) {
			float curY = chunk.StartPoint.Get(Vector.I2);

			// Add line break if moved to a new line
			if (Math.Abs(curY - lastY) > tolerance && lastY > -9990) {
				sb.AppendLine();
			}
			sb.Append(chunk.Text + '|');
			lastY = curY;
		}

		return sb.ToString();
	}

	// Required by interface (we don't use these)
	public ICollection<EventType> GetSupportedEvents() => null;
	public void BeginTextBlock() { }
	public void EndTextBlock() { }
	public void RenderImage(ImageRenderInfo data) { }
	public void RenderText(TextRenderInfo renderInfo) { }
}

// Simple helper class to hold chunk data
class TextChunk {
	public string Text { get; }
	public Vector StartPoint { get; }
	public int TextRenderMode { get; }

	public TextChunk(string text, Vector startPoint, int textRenderMode) {
		Text = text;
		StartPoint = startPoint;
		TextRenderMode = textRenderMode;
	}
}

// ==========================
// USAGE
// ==========================
//class Program {
//	static void Main(string[] args) {
//		string pdfFile = @"C:\temp\template.pdf";
//		int pageNumber = 1;

//		using (var pdfReader = new PdfReader(pdfFile))
//		using (var pdfDoc = new PdfDocument(pdfReader)) {
//			var strategy = new StopOnLargeGapStrategy();

//			var parser = new PdfCanvasProcessor(strategy);
//			parser.ProcessPageContent(pdfDoc.GetPage(pageNumber));

//			string result = strategy.GetResultantText();

//			Console.WriteLine("=== EXTRACTED TEXT (stops on >10pt gap) ===");
//			Console.WriteLine(result);
//		}
//	}
//}
