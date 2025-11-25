using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.Xml.Linq;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Rectangle=iText.Kernel.Geom.Rectangle;

namespace PDF;
public class ExtractPdfTableBelowY : LocationTextExtractionStrategy {
	public enum extractionConstraints {
		BelowY,
		WithinRectangle
	}
	private readonly float _heightThreshold;
	private readonly float _scanBelowY;
	private float? _lastBaselineY = null;
	private bool _collecting = false;
	private bool _finished = false;

	private readonly List<List<string>> _tableRows = new();
	private readonly List<string> _currentRow = new ();
	private readonly List<Rectangle> _bbox = new();
	public ExtractPdfTableBelowY(float heightThreshold, float scanBelowY, Rectangle? Bounds=null) {
		_heightThreshold = heightThreshold;
		_scanBelowY = scanBelowY;
	}
	public override void EventOccurred(IEventData data, EventType type) {
		if (!type.Equals(EventType.RENDER_TEXT) || _finished) return;
		TextRenderInfo renderInfo = (TextRenderInfo)data;
		float currentBaselineY = renderInfo.GetBaseline().GetStartPoint().Get(1);
		if (!_collecting) {
			if (currentBaselineY <= _scanBelowY) {
				_collecting = true;
				_lastBaselineY = currentBaselineY;
			} else {
				return;
			}
		} else {
			if (_lastBaselineY != null && (_lastBaselineY.Value - currentBaselineY) > _heightThreshold) {// If row gap exceeds threshold, stop collecting
				if (_currentRow.Count > 0) {
					_tableRows.Add(new(_currentRow));
					_currentRow.Clear();
				}
				_finished = true;
				return;
			}
			if (_lastBaselineY != null && Math.Abs(_lastBaselineY.Value - currentBaselineY) > 0.1f) {// If Y changes (within row group, tolerating small float differences)
				if (_currentRow.Count > 0) {
					_tableRows.Add(new(_currentRow));
					_currentRow.Clear();
				}
			}
		}
		_bbox.Add(new Rectangle(//	renderInfo.GetDescentLine().GetBoundingRectangle();
			(int)renderInfo.GetDescentLine().GetBoundingRectangle().GetX(),
			(int)renderInfo.GetDescentLine().GetBoundingRectangle().GetY(),
			(int)renderInfo.GetDescentLine().GetBoundingRectangle().GetWidth(),
			(int)renderInfo.GetDescentLine().GetBoundingRectangle().GetHeight()
		));
		_currentRow.Add(renderInfo.GetText());
		_lastBaselineY = currentBaselineY;
	}
	public  List<List<string>> GetTableRows() {
		if (_currentRow.Count > 0 && !_finished) {
			_tableRows.Add(new(_currentRow));
			_currentRow.Clear();
		}
		return _tableRows;
	}
	public List<Rectangle> GetBBoxes() {
		return _bbox;
	}
	public Rectangle GetTableBoundingBox() {
		if (_bbox.Count == 0) return null;
		Rectangle boundingRect = new(_bbox.Min(r => r.GetLeft()), _bbox.Min(r => r.GetBottom()), _bbox.Max(r => r.GetRight()) - _bbox.Min(r => r.GetLeft()), _bbox.Max(r => r.GetTop()) - _bbox.Min(r => r.GetBottom()));
		return boundingRect;
	}
	private static string ToXmlSafeName(string s) {
		if (string.IsNullOrWhiteSpace(s))
			return "Unknown";
		var cleaned = new string(s.Select(ch =>
			char.IsLetterOrDigit(ch) ? ch : '_'
		).ToArray());
		if (char.IsDigit(cleaned[0]))
			cleaned = "_" + cleaned;
		return cleaned;
	}
	public static XDocument ConvertToXDocument(List<List<string>> tableRows, string tableName = "rows") {
		if (tableRows == null || tableRows.Count == 0)
			throw new ArgumentException("Input table must not be empty");
		var headers = tableRows[0];
		var rowsElement = new XElement(tableName,
			tableRows.Skip(1).Select((row, rowIndex) => {
				var rowElement = new XElement("row", new XAttribute("index", rowIndex));
				for (int i = 0; i < headers.Count; i++) {
					var header = ToXmlSafeName(headers[i]);
					var value = i < row.Count ? row[i] : string.Empty;
					rowElement.SetAttributeValue(header, value);
				}
				return rowElement;
			})
		);
		return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), rowsElement);
	}
}