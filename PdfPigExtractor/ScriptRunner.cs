using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder;

namespace PDF;

public static class ScriptRunner {
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
		"System.Linq"
	);
	public static object Run(string code, Dictionary<string, object> globals) {
		try {
			return CSharpScript.EvaluateAsync<object>(
				code,
				options,
				new Globals(globals),
				typeof(Globals)
			).GetAwaiter().GetResult();
		} catch (Exception ex) {
			Console.WriteLine(ex);
			return null;
		}
	}
}

public class Globals {
	private readonly Dictionary<string, object> Vars;
	public Globals(Dictionary<string, object> vars) => Vars = vars;

	public object this[string name] {
		get => Vars.ContainsKey(name) ? Vars[name] : null;
		set => Vars[name] = value;
	}

	// ⭐ magically expose dictionary values as dynamic fields
	public dynamic text => this["text"];
	public dynamic marker => this["marker"];
}