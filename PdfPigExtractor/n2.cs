using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace n2;
public class TableRowByYStrategy : LocationTextExtractionStrategy {
	private float _heightThreshold;
	private float _startY;
	private float? _lastBaselineY = null;
	private bool _collecting = false;
	private bool _finished = false;

	private List<List<string>> _tableRows = new List<List<string>>();
	private List<string> _currentRow = new List<string>();

	public TableRowByYStrategy(float heightThreshold, float startY) {
		_heightThreshold = heightThreshold;
		_startY = startY;
	}

	public override void EventOccurred(IEventData data, EventType type) {
		if (!type.Equals(EventType.RENDER_TEXT) || _finished) return;

		TextRenderInfo renderInfo = (TextRenderInfo)data;
		float currentBaselineY = renderInfo.GetBaseline().GetStartPoint().Get(1);

		if (!_collecting) {
			if (currentBaselineY <= _startY) {
				_collecting = true;
				_lastBaselineY = currentBaselineY;
			} else {
				return;
			}
		} else {
			// If row gap exceeds threshold, stop collecting
			if (_lastBaselineY != null && (_lastBaselineY.Value - currentBaselineY) > _heightThreshold) {
				if (_currentRow.Count > 0) {
					_tableRows.Add(new List<string>(_currentRow));
					_currentRow.Clear();
				}
				_finished = true;
				return;
			}
			// If Y changes (within row group, tolerating small float differences)
			if (_lastBaselineY != null && Math.Abs(_lastBaselineY.Value - currentBaselineY) > 0.1f) {
				if (_currentRow.Count > 0) {
					_tableRows.Add(new List<string>(_currentRow));
					_currentRow.Clear();
				}
			}
		}

		_currentRow.Add(renderInfo.GetText());
		_lastBaselineY = currentBaselineY;
	}

	public List<List<string>> GetTableRows() {
		if (_currentRow.Count > 0 && !_finished) {
			_tableRows.Add(new List<string>(_currentRow));
			_currentRow.Clear();
		}
		return _tableRows;
	}

	public XDocument ConvertToXDocument(List<List<string>> tableRows, string tableName = "rows") {
		if (tableRows == null || tableRows.Count == 0)
			throw new ArgumentException("Input table must not be empty");

		var headers = tableRows[0];

		var rowsElement = new XElement(tableName,
			tableRows.Skip(1).Select((row, rowIndex) => {
				var rowElement = new XElement("row", new XAttribute("index", rowIndex));
				for (int i = 0; i < headers.Count; i++) {
					var header = headers[i];
					var value = i < row.Count ? row[i] : string.Empty;
					rowElement.SetAttributeValue(header, value);
				}
				return rowElement;
			})
		);

		return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), rowsElement);
	}

}




