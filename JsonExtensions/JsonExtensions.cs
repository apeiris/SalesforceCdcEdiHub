using System;
using System.Data;
using System.Linq;
using System.Text.Json;
namespace JsonExtensions;
public static class JsonExtensions {
	/// <summary>
	/// Converts a JSON element (object or array of objects) into a DataTable using LINQ projection.
	/// </summary>
	public static DataTable ToDataTable(this JsonElement jsonElement, string tableName = "data") {
		var table = new DataTable(tableName);

		if (jsonElement.ValueKind == JsonValueKind.Object) {
			// Single object → one row
			var props = jsonElement.EnumerateObject().ToList();
			props.Select(p => table.Columns.Add(p.Name, typeof(string))).ToList();
			table.Rows.Add(props.Select(p => p.Value.ToString()).ToArray());
		} else if (jsonElement.ValueKind == JsonValueKind.Array) {
			// Array → multiple rows
			var allProps = jsonElement
				.EnumerateArray()
				.SelectMany(o => o.EnumerateObject().Select(p => p.Name))
				.Distinct()
				.ToList();

			allProps.Select(p => table.Columns.Add(p, typeof(string))).ToList();

			foreach (var obj in jsonElement.EnumerateArray()) {
				var values = allProps.Select(col =>
					obj.TryGetProperty(col, out var val) ? val.ToString() : null
				).ToArray();
				table.Rows.Add(values);
			}
		} else {
			throw new InvalidOperationException("JsonElement must be an object or an array of objects.");
		}

		return table;
	}
}
