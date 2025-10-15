#region Extensions
using System.Data;
using System.Text;
using System.Text.Json;
using SalesforceCdcEdiHub;

public static class SqlServerLibExtensions {

	public static string GetSqlDataType(DataColumn column) {
		string sqlType = column.DataType switch {
			Type t when t == typeof(string) => $"NVARCHAR({(column.MaxLength > 0 ? column.MaxLength.ToString() : "MAX")})",
			Type t when t == typeof(int) => "INT",
			Type t when t == typeof(long) => "BIGINT",
			Type t when t == typeof(short) => "SMALLINT",
			Type t when t == typeof(byte) => "TINYINT",
			Type t when t == typeof(bool) => "BIT",
			Type t when t == typeof(DateTime) => "DATETIME",
			Type t when t == typeof(decimal) => "DECIMAL(18,2)",
			Type t when t == typeof(double) => "FLOAT",
			Type t when t == typeof(float) => "REAL",
			Type t when t == typeof(Guid) => "UNIQUEIDENTIFIER",
			_ => "NVARCHAR(MAX)"
		};

		if (!column.AllowDBNull)
			sqlType += " NOT NULL";

		return sqlType;
	}

	public static string GenerateDDL(this DataSet dataSet) {
		StringBuilder ddl = new StringBuilder();
		for (int i = dataSet.Tables.Count - 1; i >= 0; i--) {// Drop tables if they exist (in reverse order to avoid FK conflicts)
			var table = dataSet.Tables[i];
			ddl.AppendLine($"IF OBJECT_ID('{table.TableName}', 'U') IS NOT NULL DROP TABLE {table.TableName};");
		}
		foreach (DataTable table in dataSet.Tables) {// Create tables
			ddl.AppendLine($"CREATE TABLE {table.TableName} (");
			for (int i = 0; i < table.Columns.Count; i++) { // Columns
				var column = table.Columns[i];
				string columnDef = $"    {column.ColumnName} {GetSqlDataType(column)}";

				if (column.DefaultValue != DBNull.Value && column.DefaultValue != null) {// Handle defaults
					string defaultValue = FormatDefaultValue(column);
					columnDef += $" DEFAULT {defaultValue}";
				}
				if (!column.AllowDBNull && !IsPrimaryKey(column, table)) {// Handle nullability
					columnDef += " NOT NULL";
				}
				if (i < table.Columns.Count - 1)
					columnDef += ",";
				ddl.AppendLine(columnDef);
			}
			if (table.PrimaryKey.Length > 0) {// Primary Key
				var pkColumns = string.Join(", ", Array.ConvertAll(table.PrimaryKey, c => c.ColumnName));
				ddl.AppendLine($"    CONSTRAINT PK_{table.TableName} PRIMARY KEY ({pkColumns})");
			}
			ddl.AppendLine(");");
		}
		foreach (DataRelation relation in dataSet.Relations) {// Foreign Keys
			var parentTable = relation.ParentTable.TableName;
			var childTable = relation.ChildTable.TableName;
			var parentColumn = relation.ParentColumns[0].ColumnName;
			var childColumn = relation.ChildColumns[0].ColumnName;
			ddl.AppendLine($"ALTER TABLE {childTable}");
			ddl.AppendLine($"ADD CONSTRAINT FK_{childTable}_{parentTable} FOREIGN KEY ({childColumn})");
			ddl.AppendLine($"REFERENCES {parentTable} ({parentColumn});");
		}
		return ddl.ToString();
	}
	static string FormatDefaultValue(DataColumn column) {
		if (column.DefaultValue == null)
			throw new InvalidOperationException("DefaultValue cannot be null.");
		if (column.DataType == typeof(string))
			return $"'{column.DefaultValue}'";
		if (column.DataType == typeof(DateTime))
			return "'2023-01-01'"; // Simplified for example  
		if (column.DataType == typeof(decimal) || column.DataType == typeof(int))
			return column.DefaultValue.ToString()!;
		if (column.DataType == typeof(bool))
			return (bool)column.DefaultValue ? "1" : "0";
		return "''";
	}
	static bool IsPrimaryKey(DataColumn column, DataTable table) {
		return Array.Exists(table.PrimaryKey, pk => pk.ColumnName == column.ColumnName);
	}
	public static string GetXml(this DataTable table, string ColumnsToSelect) {
		DataTable tblClipped = table.DefaultView.ToTable(true, ColumnsToSelect.Split(','));
		tblClipped.TableName = table.TableName;
		DataSet ds = new DataSet();
		ds.Tables.Add(tblClipped.Copy());
		ds.DataSetName = "X";
		return ds.GetXml();
	}
	public static DataTable Transpose(this DataTable inputTable, string rowLabel = "FieldName", string contentLabel = "Value", string primaryKey = null) {
		if (inputTable == null || inputTable.Rows.Count == 0)
			return new DataTable();
		DataTable transposedTable = new DataTable(inputTable.TableName);
		inputTable.AsEnumerable()// Project FieldName values to create columns
			.Select(row => row[rowLabel]?.ToString())
			.Where(fieldName => !string.IsNullOrEmpty(fieldName) && !transposedTable.Columns.Contains(fieldName))
			.ToList()
			.ForEach(fieldName => transposedTable.Columns.Add(fieldName));
		var rowData = inputTable.AsEnumerable()     // Project rows to key-value pairs with DateTime conversion
					.Select(row => new {
						FieldName = row[rowLabel]?.ToString(),
						FieldValue = inputTable.Columns.Contains("DataType") && row["DataType"]?.ToString() == "DateTime" && long.TryParse(row[contentLabel]?.ToString(), out long longValue) ? ConvertLongToDateTime(longValue) : row[contentLabel]?.ToString()
					})
							.Where(x => !string.IsNullOrEmpty(x.FieldName) && transposedTable.Columns.Contains(x.FieldName)).ToList();
		DataRow newRow = transposedTable.NewRow();// Create and populate a new row
		rowData.ForEach(x => newRow[x.FieldName] = x.FieldValue != null ? (object)x.FieldValue : DBNull.Value);
		transposedTable.Rows.Add(newRow);
		if (primaryKey != null && transposedTable.Columns.Contains(primaryKey))// Set primary key if specified
			transposedTable.PrimaryKey = new[] { transposedTable.Columns[primaryKey]! };
		return transposedTable;
	}
	public static DataTable Transpose(this DataTable dt) {
		if (dt == null) return null;
		// Create a new DataTable for the transposed result
		DataTable transposedTable = new DataTable();

		// Add two columns: one for the original column names and one for the values
		transposedTable.Columns.Add("ColumnName", typeof(string));
		transposedTable.Columns.Add("Value", typeof(object));


		if (dt.Rows.Count != 1) {// Ensure the input DataTable has exactly one row
			throw new ArgumentException("Input DataTable must contain exactly one row.");
		}

		// Get the single row
		DataRow row = dt.Rows[0];

		// Use LINQ to create DataRows and add them directly to the DataTable
		dt.Columns.Cast<DataColumn>()
			.Select(col => transposedTable.Rows.Add(col.ColumnName, row[col]))
			.ToList();

		return transposedTable;
	}
	private static string ConvertLongToDateTime(long longValue) {
		try {

			DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);// Assume longValue is a Unix timestamp in milliseconds
			return epoch.AddMilliseconds(longValue).ToString("o"); // ISO 8601 format
																   // Alternative: If longValue is ticks, use:
																   // return new DateTime(longValue).ToString("o");
		} catch (Exception) {
			return longValue.ToString();// Fallback to original value if conversion fails
		}
	}
	public static string ToJson(this DataTable table, bool indented = false, string excludedColumns = "", bool singleObject = true) {

		if (table == null || table.Rows.Count == 0) {// Handle empty DataTable
			return JsonSerializer.Serialize(new Dictionary<string, object>(), new JsonSerializerOptions {
				WriteIndented = indented,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			});
		}
		var excludeColumns = string.IsNullOrWhiteSpace(excludedColumns)
			? new HashSet<string>()
			: new HashSet<string>(excludedColumns.Split(',', StringSplitOptions.RemoveEmptyEntries)
												.Select(col => col.Trim()), StringComparer.OrdinalIgnoreCase);
		var validColumns = table.Columns.Cast<DataColumn>() // Identify columns with at least one non-null value (optional, based on your previous request)
			.Where(col => !excludeColumns.Contains(col.ColumnName) &&
						  table.AsEnumerable().Any(row => !row.IsNull(col)))
			.Select(col => col.ColumnName)
			.ToHashSet();
		if (singleObject) { // Serialize a single object if singleObject is true (for UpsertSobject)
			var row = table.Rows[0]; // Take first row for single-object JSON
			var dict = table.Columns.Cast<DataColumn>()
				.Where(col => validColumns.Contains(col.ColumnName))
				.ToDictionary(
					col => col.ColumnName,
					col => row.IsNull(col) ? null : row[col]
				);

			return JsonSerializer.Serialize(dict, new JsonSerializerOptions {
				WriteIndented = indented,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			});
		}
		var rows = table.Rows.Cast<DataRow>()   // Serialize all rows as an array (for other use cases)
					.Select(row => table.Columns.Cast<DataColumn>()
						.Where(col => validColumns.Contains(col.ColumnName))
						.ToDictionary(
							col => col.ColumnName,
							col => row.IsNull(col) ? null : row[col]
						));
		return JsonSerializer.Serialize(rows, new JsonSerializerOptions {
			WriteIndented = indented,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});
	}
	//==========================================================================================
	public static string ToJson(this DataRow row, bool indented = false, string excludedColumns = "") {
		var dict = new Dictionary<string, object>();
		var excluded = excludedColumns.Split(',', StringSplitOptions.RemoveEmptyEntries)
									 .Select(c => c.Trim())
									 .ToHashSet();
		foreach (DataColumn column in row.Table.Columns) {
			if (!excluded.Contains(column.ColumnName)) {
				dict[column.ColumnName] = row.IsNull(column) ? null : row[column];
			}
		}
		var options = new JsonSerializerOptions {
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = indented,
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
		};
		return JsonSerializer.Serialize(dict, options); // Serializes as object, not array
	}
	public static DataTable DeriveColumn(this DataTable inputTable, string deriveFromColumn, string newColumnName) {
		inputTable.Columns.Add(newColumnName, typeof(string));
		inputTable.AsEnumerable()
			.Select((row, index) => {
				string selectedEntity = row[deriveFromColumn]?.ToString();
				object derivedValue = DBNull.Value;
				row[newColumnName] = SalesforceService.PlatformEventChannelMemeberToObjectName(selectedEntity);
				return row; // Required by Select, though not used as rows are modified in-place
			})
			.ToList();
		if (inputTable.Columns.Contains(deriveFromColumn)) inputTable.Columns.Remove(deriveFromColumn);
		return inputTable;
	}
}
#endregion Extensions