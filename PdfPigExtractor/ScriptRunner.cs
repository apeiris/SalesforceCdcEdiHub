using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using NLog;
using NLog.Layouts;

namespace PDF;

public static class ScriptRunner {
	private static readonly Logger Log = LogManager.GetCurrentClassLogger();

	private static readonly ScriptOptions Options =
		ScriptOptions.Default
			.AddReferences(
				typeof(object).Assembly,
				typeof(Enumerable).Assembly,
				typeof(iText.Kernel.Pdf.PdfDocument).Assembly,
				typeof(ExtractedArea).Assembly,
				typeof(StopOnLargeGapStrategy).Assembly,
				typeof(XElement).Assembly
			)
			.AddImports(
				"System",
				"System.Linq",
				"System.Collections.Generic",
				"System.Xml.Linq",
				"iText.Kernel.Pdf",
				"iText.Kernel.Geom",
				"PDF"
			);

	/// <summary>
	/// Execute a Roslyn C# script with access to Globals and PDF document.
	/// </summary>
	/// <param name="code">C# script code</param>
	/// <param name="globalsDict">Dictionary of global variables</param>
	/// <param name="pdfDoc">PDF document context</param>
	/// <returns>Result of script execution (XElement, ExtractedArea, string, etc.)</returns>
	public static async Task<object?> RunAsync(
		string code,
		Dictionary<string, object> globalsDict,
		iText.Kernel.Pdf.PdfDocument pdfDoc) {
		Log.Debug("Running Roslyn Script:\n" + code);

		var globalsInstance = new Globals(globalsDict, pdfDoc);

		try {
			return await CSharpScript.EvaluateAsync<object>(
				code,
				Options,
				globalsInstance,
				typeof(Globals)
			);
		} catch (CompilationErrorException ex) {
			var diagnostics = string.Join(Environment.NewLine, ex.Diagnostics);
			Log.Error($"Script compilation failed:\n{diagnostics}");
			throw;
		} catch (Exception ex) {
			Log.Error(ex, "Script execution failed");
			throw;
		}
	}
}

public sealed class Globals {
	private static readonly Logger Log = LogManager.GetCurrentClassLogger();
	private readonly iText.Kernel.Pdf.PdfDocument _pdfDoc;
	private readonly Dictionary<string, object> _globals;

	public Globals(Dictionary<string, object> globals, iText.Kernel.Pdf.PdfDocument pdfDoc) {
		_globals = globals ?? new Dictionary<string, object>();
		_pdfDoc = pdfDoc;
		Log.Debug($"Roslyn Globals initialized with keys: {string.Join(", ", _globals.Keys)}");
	}

	// ---------- HELP / DISCOVERY ----------
	public string[] Help =>
		GetType()
			.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.Where(m => !m.IsSpecialName)
			.Select(m => {
				var args = string.Join(", ",
					m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
				return $"{m.ReturnType.Name} {m.Name}({args})";
			})
			.ToArray();

	// ---------- PDF SCRAPING ----------
	public ExtractedArea ScrapePDF(float x, float scanBelowY, float width, float line2LineGap) {
		var strategy = new StopOnLargeGapStrategy(x, scanBelowY, width, line2LineGap);
		var parser = new iText.Kernel.Pdf.Canvas.Parser.PdfCanvasProcessor(strategy);

		try {
			parser.ProcessPageContent(_pdfDoc.GetPage(1));
		} catch (Exception ex) {
			Log.Warn($"PDF extraction warning: {ex.Message}");
		}

		return new ExtractedArea(
			"ScrapePDF",
			strategy.GetResultantText(),
			strategy.GetCollectedTextBounds()
		);
	}

	// ---------- ADDRESS SPLITTING ----------
	//public XElement Split(ExtractedArea area, IEnumerable<string> columns,char delimiter=',') {
	//	if (area == null) throw new ArgumentNullException(nameof(area));
	//	var parts = area.Value
	//		.Split(delimiter, StringSplitOptions.RemoveEmptyEntries)
	//		.Select(p => p.Trim());
	//	XElement x = (XElement)_globals["__currentElement"];
	//	return new XElement(x.Name.LocalName,
	//				parts.Zip(columns, (value, column) =>
	//					new XAttribute(column, value))
	//			);
	//}
	public XElement Split(
	ExtractedArea area,
	IEnumerable<string> columns,
	char delimiter = ',') {
		if (area == null)
			throw new ArgumentNullException(nameof(area));

		if (!_globals.TryGetValue("__currentElement", out var elObj) ||
			elObj is not XElement element) {
			throw new InvalidOperationException(
				"Split requires __currentElement to be set.");
		}

		var parts = area.Value
			.Split(delimiter, StringSplitOptions.RemoveEmptyEntries)
			.Select(p => p.Trim())
			.ToList();

		// Remove any existing attributes that match target columns
		foreach (var col in columns)
			element.Attribute(col)?.Remove();

		// Apply attributes directly to the current element
		foreach (var (value, column) in parts.Zip(columns)) {
			element.SetAttributeValue(column, value);
		}

		return element;
	}

	public XElement SplitLinesToColumns(
	ExtractedArea area,
	IEnumerable<string> columns,
	char columnDelimiter = '\t') {
		if (area == null)
			throw new ArgumentNullException(nameof(area));

		if (!_globals.TryGetValue("__currentElement", out var elObj) ||
			elObj is not XElement element)
			throw new InvalidOperationException(
				"SplitLinesToColumns requires __currentElement to be set.");

		var lines = area.Value?
			.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
			.Select(l => l.Trim())
			.Where(l => l.Length > 0)
			.ToList() ?? new List<string>();

		element.RemoveNodes(); // clear existing children

		foreach (var line in lines) {
			var parts = line
				.Split(columnDelimiter, StringSplitOptions.None)
				.Select(p => p.Trim())
				.ToList();

			var loop = new XElement("line");

			foreach (var (column, index) in columns.Select((c, i) => (c, i))) {
				string value = index < parts.Count ? parts[index] : string.Empty;
				loop.SetAttributeValue(column, value);
			}

			element.Add(loop);
		}

		return element;
	}


	public object? Get(string key) => _globals.TryGetValue(key, out var val) ? val : null;
	public void Set(string key, object value) => _globals[key] = value;

	// ---------- TEST ----------
	public int Test() => 1;
}