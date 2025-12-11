using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder;

namespace PDF;

public static class ScriptRunner {
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	private static readonly ScriptOptions options = ScriptOptions.Default
	.AddReferences(
		typeof(object).Assembly,                     // mscorlib/System.Runtime
		typeof(System.Linq.Enumerable).Assembly,     // System.Linq
		typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly, // Microsoft.CSharp runtime binder
		AppDomain.CurrentDomain.GetAssemblies()
			.First(a => a.GetName().Name == "Microsoft.CSharp") // Explicitly add Microsoft.CSharp.dll
	)
	.AddImports(
		"System",
		"System.Linq",
		"System.Collections.Generic"
	);
	public static object Run(string code, Dictionary<string, object> globals) {

		// 🔥 normalize input to string so Split() always works
		if (globals.TryGetValue("value", out var v))
			globals["value"] = v?.ToString() ?? "";

		var globalsInstance = new Globals(globals);

		try {
			var result = CSharpScript.EvaluateAsync<object>(
				code,
				options,
				globalsInstance,
				typeof(Globals)
			).Result;

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

	public Dictionary<string, object> data {
		get {
			Log.Debug("Roslyn Script: Accessing 'data' global object.");
			return _data;
		}
	}

	// 🔥 MUST BE string, not object
	public string value =>
		_data.TryGetValue("value", out var v) ? v?.ToString() ?? "" : "";

	public Globals(Dictionary<string, object> vars) {
		_data = vars;

		Log.Debug("Roslyn Script: Environment initialized with keys: " +
				  string.Join(", ", vars.Keys));

		if (vars.ContainsKey("value")) {
			Log.Debug($"Roslyn Script: Input 'value' content: {vars["value"]}");
		}
	}
}
