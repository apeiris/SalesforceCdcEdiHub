using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinForms {
	internal class NLogConsoleWriter : System.IO.TextWriter {
		private readonly NLog.Logger _logger;
		private readonly System.Text.StringBuilder _buffer = new();

		public NLogConsoleWriter(NLog.Logger logger) => _logger = logger;

		public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

		public override void Write(char value) {
			if (value == '\n' || value == '\r')
				FlushBuffer();
			else
				_buffer.Append(value);
		}

		public override void Write(string? value) {
			if (value == null) return;
			_buffer.Append(value);
			if (value.EndsWith("\n") || value.EndsWith("\r"))
				FlushBuffer();
		}

		public override void WriteLine(string? value) {
			if (value != null) _buffer.Append(value);
			FlushBuffer();
		}

		private void FlushBuffer() {
			if (_buffer.Length == 0) return;
			string line = _buffer.ToString().TrimEnd('\r', '\n');
			_buffer.Clear();
			_logger.Info(line);
		}
	}
}
