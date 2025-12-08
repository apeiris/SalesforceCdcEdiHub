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
		var options = ScriptOptions.Default
			.AddReferences(
				typeof(System.Linq.Enumerable).Assembly, // For .Select()
				typeof(System.Collections.Generic.Dictionary<string, string>).Assembly, // For Dictionary
				typeof(System.Runtime.CompilerServices.DynamicAttribute).Assembly // For dynamic handling
			)
			.AddImports("System", "System.Linq", "System.Collections.Generic");
		try {
			var globalsInstance = new Globals(globals);
			var task = CSharpScript.EvaluateAsync<object>(
				code,
				options,
				globalsInstance,
				typeof(Globals)
			);

			return task.Result; // Simplified wait
		} catch (Exception ex) {
			Log.Error($"Script Execution Error: {ex.InnerException?.Message ?? ex.Message}");// Log the actual script exception details
			return null;
		}
	}
}

public class Globals {
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	private Dictionary<string, object> _data;
	public Dictionary<string, object> data {
		get {
			Log.Debug("Roslyn Script: Accessing 'data' global object.");	// This logs every time the script accesses the 'data' global
			return _data;
		}
	}

	public Globals(Dictionary<string, object> vars) {
		this._data = vars;
		Log.Debug("Roslyn Script: Environment initialized with keys: " + string.Join(", ", vars.Keys));
		if (vars.ContainsKey("value")) {
			Log.Debug($"Roslyn Script: Input 'value' content: {vars["value"]}");
		}
	}
}