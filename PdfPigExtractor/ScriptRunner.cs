using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CSharp.RuntimeBinder;
using NLog;
using static PDF.XmlMapProcessor;

namespace PDF;



public static class ScriptRunner {
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	private static readonly ScriptOptions options = ScriptOptions.Default
	.AddReferences(
		typeof(object).Assembly,                     // mscorlib/System.Runtime
		typeof(System.Linq.Enumerable).Assembly,     // System.Linq
		typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly, // Microsoft.CSharp runtime binder
		typeof(iText.Kernel.Pdf.PdfDocument).Assembly,
		typeof(StopOnLargeGapStrategy).Assembly,
		typeof(ExtractedArea).Assembly,
		AppDomain.CurrentDomain.GetAssemblies()
			.First(a => a.GetName().Name == "Microsoft.CSharp") // Explicitly add Microsoft.CSharp.dll
	)
	.AddImports(
		"System",
		"System.Linq",
		"System.Collections.Generic",
		"iText.Kernel.Pdf",
		"iText.Kernel.Geom",
		"PDF"
	);
	public static List<string> GetAvailableMethods() {
		return typeof(Globals)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.Where(m => !m.IsSpecialName) // Exclude property accessors like get_value
			.Select(m => {
				var parameters = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
				return $"{m.ReturnType.Name} {m.Name}({parameters})";
			}).ToList();
	}
	public static object Run(string code, Dictionary<string, object> globals, iText.Kernel.Pdf.PdfDocument pdfDoc) {
		Log.Debug("Running Roslyn Script with code:\n" + code);
		if (globals.TryGetValue("value", out var v)) globals["value"] = v?.ToString() ?? "";        // 🔥 normalize input to string so Split() always works
		var globalsInstance = new Globals(globals, pdfDoc);
		try {
			var result = CSharpScript.EvaluateAsync<object>(code, options, globalsInstance, typeof(Globals)).Result;
			return result;
		} catch (Exception ex) {
			Log.Error($"Script Execution Error: {ex.InnerException?.Message ?? ex.Message}");
			return null;
		}
	}
}
public class Globals {
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	private readonly Dictionary<string, object> _data;
	private readonly iText.Kernel.Pdf.PdfDocument _pdfDoc;
	public string[] Help => typeof(Globals)
		.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
		.Where(m => !m.IsSpecialName)
		.Select(m => m.Name)
		.ToArray();

	public Dictionary<string, object> data {
		get {
			Log.Debug("Roslyn Script: Accessing 'data' global object.");
			return _data;
		}
	}
	public string value => _data.TryGetValue("value", out var v) ? v?.ToString() ?? "" : "";    // 🔥 MUST BE string, not object
	public Globals(Dictionary<string, object> vars, iText.Kernel.Pdf.PdfDocument pdfDoc) {
		Log.Error("****Rolyn Globals*****[X]");
		_data = vars;
		_pdfDoc = pdfDoc;
		Log.Debug($"Roslyn Script: Environment initialized with keys: {string.Join(", ", vars.Keys)}");
		if (vars.ContainsKey("value")) {
			Log.Debug($"Roslyn Script: Input 'value' content: {vars["value"]}");
		}
	}
	public ExtractedArea ScrapePDF(float x, float scanBelowY, float width, float line2LineGap) {
		// 1. Initialize your custom strategy
		var strategy = new StopOnLargeGapStrategy(x, scanBelowY, width, line2LineGap);
		var parser = new iText.Kernel.Pdf.Canvas.Parser.PdfCanvasProcessor(strategy);
		try {
			// 2. Process the first page (as per your original logic)
			parser.ProcessPageContent(_pdfDoc.GetPage(1));
		} catch (Exception ex) {

			Log.Warn($"Font Error during PDF text extraction: {ex.Message}");
			// We don't return null here; we return whatever the strategy managed 
			// to collect BEFORE the crash happened.
		}
		// 3. Clean up text
		string resultText = strategy.GetResultantText();
		iText.Kernel.Geom.Rectangle bounds = strategy.GetCollectedTextBounds();

		// 4. Return the combined result
		return new ExtractedArea("ScriptResult", resultText, bounds);
	}

	
	public int test() { return 01; }

}
