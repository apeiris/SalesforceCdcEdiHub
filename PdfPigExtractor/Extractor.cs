using System.Text;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using NLog;
namespace PDF;

// ──────────────────────────────────────────────────────────────
// UPDATED STRATEGY – safe for later use (no graphics state crash)
// ──────────────────────────────────────────────────────────────
public class StopOnLargeGapStrategy : ITextExtractionStrategy {
	private readonly float _startX;
	private readonly float _startY;
	private readonly float _width;
	private readonly float _gapThreshold;

	private bool started = false;
	private bool stopped = false;
	private float lastBaselineY = 0f;

	private readonly List<TextChunk> chunks = new List<TextChunk>();
	private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
	public StopOnLargeGapStrategy(float startX, float startY, float width, float gapThreshold) {
		_startX = startX;
		_startY = startY;
		_width = width;
		_gapThreshold = gapThreshold;
	}

	public void EventOccurred(IEventData data, EventType type) {
		if (stopped) return;
		if (type == EventType.RENDER_TEXT) {
			TextRenderInfo tri = (TextRenderInfo)data;
			var ascent = tri.GetAscentLine();
			var descent = tri.GetDescentLine();
			LineSegment baseline = tri.GetBaseline();   // Capture everything WHILE graphics state is still alive
			Vector baselineStart = baseline.GetStartPoint();
			float curX = baselineStart.Get(Vector.I1);
			float curY = baselineStart.Get(Vector.I2);

			if (!started) {// 1. Wait until we hit the starting region
				if (curX >= _startX && curY <= _startY)
					started = true;
				else
					return;
			}
			if (lastBaselineY > 0) {// 2. Detect large vertical gap → stop forever
				float gap = lastBaselineY - curY;
				if (gap > _gapThreshold) {
					stopped = true;
					return;
				}
			}

			bool inXlimits = curX >= _startX && curX <= _startX + _width;// 3. Keep only text inside the horizontal scan band
		
			if (inXlimits) {// PRE-CAPTURE bounds and baseline Y here (safe zone)
				float left = descent.GetStartPoint().Get(0);
				float right = ascent.GetEndPoint().Get(0);
				float top = ascent.GetStartPoint().Get(1);
				float bottom = descent.GetEndPoint().Get(1);
				Rectangle bounds = new Rectangle(
					left,
					bottom,
					right - left,
					top - bottom
				);
			
				float baselineY = curY;      // already calculated
			    string lineEnd = lastBaselineY != baselineY ? "\r\n" : "";
				
				TextChunk tc =	new TextChunk(
												text: lineEnd+tri.GetText() ,
												startPoint: baselineStart,
												baselineY: baselineY,
												bounds: bounds,
												textRenderMode: tri.GetTextRenderMode()
											 );
				Logger.Debug($"lastbline={lastBaselineY}: baselineY={baselineY}:{tc.Text} :lineEnd={lineEnd}");
				lastBaselineY = curY;
				chunks.Add(tc);
			}
		}
	}

	public string GetResultantText() {
		if (chunks.Count == 0) return "";

		// Sort top→bottom, then left→right
		chunks.Sort((a, b) => {
			int yCmp = b.BaselineY.CompareTo(a.BaselineY);
			if (yCmp != 0) return yCmp;
			return a.StartPoint.Get(Vector.I1).CompareTo(b.StartPoint.Get(Vector.I1));
		});

		StringBuilder sb = new StringBuilder();
		const float tolerance = 18.0f;
		float lastY = -9999f;

		foreach (var chunk in chunks) {
			float curY = chunk.BaselineY;

			if (Math.Abs(curY - lastY) > tolerance && lastY > -9990)
				sb.AppendLine();

			sb.AppendLine(chunk.Text);
			if (lastY != curY) {
				sb.AppendLine();
			
				sb.AppendLine();
			}

			lastY = curY;
		}
		return sb.ToString();
	}
	// NEW METHOD – 100% safe, no graphics state needed
	public Rectangle GetCollectedTextBounds() {
		if (chunks.Count == 0) return null;

		float left = float.MaxValue;
		float bottom = float.MaxValue;
		float right = float.MinValue;
		float top = float.MinValue;
		float maxPad = 0f;
		foreach (var c in chunks) {
			Rectangle r = c.Bounds;
			left = Math.Min(left, r.GetLeft());
			bottom = Math.Min(bottom, r.GetBottom());
			right = Math.Max(right, r.GetRight());
			top = Math.Max(top, r.GetTop());
			float pad = r.GetHeight() * 0.10f;
			maxPad = pad > maxPad ? pad : maxPad;
		}

		return new Rectangle(left - maxPad, bottom - maxPad, (right - left) + maxPad * 2, (top - bottom) + maxPad * 2);
	}

	// Required interface members
	public ICollection<EventType> GetSupportedEvents() => null;
	public void BeginTextBlock() { }
	public void EndTextBlock() { }
	public void RenderImage(ImageRenderInfo data) { }
	public void RenderText(TextRenderInfo renderInfo) { }
}

// ──────────────────────────────────────────────────────────────
// UPDATED TextChunk – stores everything we need later
// ──────────────────────────────────────────────────────────────
class TextChunk {
	public string Text { get; }
	public Vector StartPoint { get; }
	public float BaselineY { get; }      // pre-captured Y


	public Rectangle Bounds { get; }     // pre-captured bounding box
	public int TextRenderMode { get; }

	public TextChunk(string text, Vector startPoint, float baselineY, Rectangle bounds, int textRenderMode) {
		Text = text;
		StartPoint = startPoint;
		BaselineY = baselineY;
		Bounds = bounds;
		TextRenderMode = textRenderMode;
	}
}