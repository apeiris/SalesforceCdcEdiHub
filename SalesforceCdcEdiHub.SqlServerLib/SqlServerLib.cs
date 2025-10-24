using System.Data;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesforceCdcEdiHub.Common;
using LogLevel = NLog.LogLevel;
namespace SalesforceCdcEdiHub;
public class SqlServerConfig { public string ConnectionString { get; set; } }
public delegate Task SqlEventHandler(SqlEventArgs args);
#region event args
public class SqlEventArgs : EventArgs {
	public string EntityName { get; set; }
	public string RecordId { get; set; }
	public string Operation { get; set; } // "INSERT", "UPDATE", "DELETE"
	public bool Success { get; set; }
	public DateTime ProcessedAt { get; set; }
	public string ErrorMessage { get; set; }
}

public class SqlEventArg : EventArgs {
	public LogLevel LogLevel { get; }
	public string Message { get; }
	public SqlEvents SqlEvent { get; }
	public string ReturningFrom { get; }
	public bool HasErrors { get; } = false;
	public SqlEventArg(string message, SqlEvents evt, LogLevel ll, string returningFrom, bool hasErrors) {
		LogLevel = ll;
		Message = message;
		SqlEvent = evt;
		ReturningFrom = returningFrom;
		hasErrors = hasErrors;
		}
	}
public class SqlObjectQuery : EventArgs {
	public LogLevel Loglevel { get; }
	public string ObjectName { get; }
	public string ObjectType { get; }
	public bool Exist { get; }
	public string Query { get; }
	public int Id { get; }
	public string message { get; }
	public SqlObjectQuery(string objectName, string objectType, int id, bool exist, string query, string msg) {
		ObjectName = objectName;
		ObjectType = objectType;
		Exist = exist;
		Query = query;
		message = msg;
		Loglevel = LogLevel.Off;// this event is not for logging
		Id = id;// row id when exist -1 otherwise
		}
	}
public class SqlTableEvent : EventArgs {
	public DataTable table { get; }
	public SqlEvents _event { get; }
	public SqlTableEvent(DataTable dt, SqlEvents evt) {
		table = dt;
		_event = evt;
		}
	}
#endregion event args
#region enums
public enum SqlEvents {
	None,
	Create,
	Inserted,
	Deleted,
	Updated,
	ReSeeded,
	SqlException,
	Exception,
	}
#endregion enums
public class ColumnMetadata {
	public string ColumnName { get; set; }
	public string DataType { get; set; }
	public bool IsNullable { get; set; }
	public int MaxLength { get; set; }
	}
public class SqlServerLib {
	private readonly PubSubService _pubSubService;
	private readonly string? _connectionString;
	private readonly SqlServerConfig _config;
	private readonly Common.ISalesforceService _salesforceService;
	private readonly ILogger<SqlServerLib> _logger;
	private Dictionary<string, string> _queryCache = new Dictionary<string, string>();
	private readonly string _sqlSchemaName = "sfo";
	public event EventHandler<SqlEventArg> SqlEvent;
	public event EventHandler<SqlObjectQuery> SqlObjectExist;
	public event EventHandler<SqlTableEvent> SqlTableEvent;
	public event SqlEventHandler OnSqlEvent;
	private void RaisSqlEvent(string message, SqlEvents enmSqlEvent, LogLevel ll, bool hasErrors, [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) {
		message = $"{message}:{callerMemberName}:{callerLineNumber}";
		SqlEvent?.Invoke(this, new SqlEventArg(message, enmSqlEvent, ll, callerMemberName, hasErrors));
		}
	private void RaisSqlObjectExist(int objectId, string objectName, string objectType, bool exists, string query, [CallerMemberName] string mn = "", [CallerLineNumber] int ln = 0) {
		string msg = $"{objectName}:{objectType}:{objectId}:{exists}:{mn}:{ln}";
		SqlObjectExist?.Invoke(this, new SqlObjectQuery(objectName, objectType, objectId, exists, query, msg));
		}
	private void RaiseSqlTableEvent(DataTable dt, SqlEvents e) {
		SqlTableEvent?.Invoke(this, new SqlTableEvent(dt, e));
		}
	#region  SqlServerLib.ctor
	public SqlServerLib(PubSubService pubSubService, ISalesforceService salesforceService, IConfiguration configuration, ILogger<SqlServerLib> logger) {
		_pubSubService = pubSubService ?? throw new ArgumentNullException(nameof(pubSubService));
		_connectionString = configuration.GetConnectionString("mssql") ?? throw new ArgumentNullException(nameof(configuration));
		_sqlSchemaName = configuration.GetSection("Salesforce")["SqlSchemaName"]!;
		_salesforceService = salesforceService ?? throw new ArgumentNullException(nameof(salesforceService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		if (string.IsNullOrWhiteSpace(_connectionString)) {
			_logger.LogError("Connection string 'mssql' is missing or empty in the configuration.!");
			throw new InvalidOperationException("Connection string 'mssql' is missing or empty in configuration.");
			}
	_pubSubService.CDCEvent += async (sender, e) => await _pubSubService_CDCEvent(sender, e);
	}
	private async Task RaiseSqlEventAsync(string entityName, string recordId,
	string operation, bool success, string errorMessage = null) {
		if (OnSqlEvent != null) {
			var args = new SqlEventArgs {
				EntityName = entityName,
				RecordId = recordId,
				Operation = operation,
				Success = success,
				ProcessedAt = DateTime.UtcNow,
				ErrorMessage = errorMessage
			};
			_ = Task.Run(async () =>// Fire and forget for completion events (non-blocking)
			{
				try {
					await OnSqlEvent.Invoke(args);
					_logger.LogDebug($"args:{args}");
					_logger.LogDebug("here...");
				} catch (Exception ex) {
					_logger.LogError($"SqlEvent handler failed: {ex.Message}");
				}
			});
		}
	}
	private async Task _pubSubService_CDCEvent(object? sender, CDCEventArgs e) {
		DataTable dtTransposed = e.DeltaFields.Transpose(primaryKey: "Id");//Transpose to row to columnset, and  defaults  FieldName, and Value as columns
		string sql = $"SELECT  {columnList(dtTransposed)} FROM sfo.[{dtTransposed.TableName}] where {dtTransposed.PrimaryKey.FirstOrDefault()?.ColumnName}='{e.RecordIds[0]}';";
		enmIsTo isto = (enmIsTo)Enum.Parse(typeof(enmIsTo), e.ChangeType, ignoreCase: true);
		switch (isto) {
			case enmIsTo.Insert:
			case enmIsTo.Create:
			case enmIsTo.Update:
				UpdateOrInsertRecordAsync(dtTransposed, e.RecordIds[0], isto);
				_logger.LogDebug($"prep:RaiseSqlEvent : table={dtTransposed.TableName}, recid : isto={isto.ToString()} ");
				await RaiseSqlEventAsync(e.DeltaFields.TableName, e.RecordIds[0],isto.ToString(), true, "done");
				break;
			case enmIsTo.Delete:
				int rowsAffected = DeleteRecord(e.DeltaFields.TableName, e.RecordIds[0]);
				break;
			}
		}
	#endregion SqlServerLib.ctor
	#region Public Methods
	public DataTable GetAll_sfoTables() /* gets all tables in sfo schema*/ {
		DataTable dataTable = new DataTable();
		try {
			using (SqlConnection connection = new SqlConnection(_connectionString)) {
				string query = @"
                        SELECT name = o.name  
						FROM sys.objects o
                        JOIN sys.schemas s ON o.schema_id = s.schema_id
                        WHERE type = 'U' and s.Name= 'sfo'";
				using (SqlCommand command = new SqlCommand(query, connection)) {
					connection.Open();
					using (SqlDataAdapter adapter = new SqlDataAdapter(command)) {
						adapter.Fill(dataTable);
						}
					}
				}
			} catch (SqlException ex) {
			_logger.LogError($"SQL Error:{ex.Message}");
			throw;
			} catch (Exception ex) {
			_logger.LogError(ex.Message);
			throw;
			}
		RaisSqlEvent($"{dataTable.Rows.Count} rows", SqlEvents.None, LogLevel.Debug, true);
		return dataTable;
		}
	public DataTable Select(string sql, string primaryKey = "Id") {
		DataTable dataTable = new DataTable();
		try {
			using (SqlConnection connection = new SqlConnection(_connectionString)) {
				connection.Open();
				using (SqlCommand command = new SqlCommand(sql, connection)) {
					using (SqlDataAdapter adapter = new SqlDataAdapter(command)) {
						adapter.Fill(dataTable);
						}
					}
				}
			} catch (SqlException ex) {
			_logger.LogError($"{ex.Message} stmt: {sql}");
			throw;
			} catch (Exception ex) {
			_logger.LogError($"{ex.Message}, stmt: {sql}");
			throw;
			}
		return dataTable;
		}
	public T ExecuteScalar<T>(string sql) {
		try {
			using (SqlConnection connection = new SqlConnection(_connectionString)) {
				connection.Open();
				using (SqlCommand command = new SqlCommand(sql, connection)) {
					object? result = command.ExecuteScalar();
					if (result == null || result == DBNull.Value) {
						return default!;
						}
					return (T)Convert.ChangeType(result, typeof(T));
					}
				}
			} catch (SqlException ex) {
			_logger.LogError($"{ex.Message} exception{ex.Message}");
			throw;
			} catch (Exception ex) {
			_logger.LogError(ex.Message);
			throw;
			}
		}
	public int ExecuteNoneQuery(string script) {
		int result = 0;
		try {
			using (SqlConnection connection = new SqlConnection(_connectionString)) {
				connection.Open();// Open the connection
				using (SqlCommand command = new SqlCommand(script, connection))
					result = command.ExecuteNonQuery();
				}
			} catch (SqlException ex) {
			result = -1;
			_logger.LogError($"SQL Error:{ex.Message}");
			} catch (Exception ex) {
			result = -1;
			_logger.LogError($"Error:{ex.Message}");
			}
		return result;
		}
	public void DeleteCDCObject(string objectName) {
		try {
			ExecuteNoneQuery($"DELETE FROM CDCObjects WHERE objectName ='{objectName}'");

			RaisSqlEvent($"Deleted {objectName} from CDC", SqlEvents.Deleted, LogLevel.Info, hasErrors: false);
			} catch (SqlException ex) {
			_logger.LogError($"SQL Error:{ex.Message}");
			} catch (Exception ex) {
			_logger.LogError($"Error:{ex.Message}");
			}
		}
	public string GenerateCreateTableScript(DataTable schema, string schemaName, string tableName) {
		StringBuilder sql = new StringBuilder();
		sql.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = '{tableName}' AND s.name = '{schemaName}')");
		sql.AppendLine("BEGIN");
		sql.AppendLine($"    CREATE TABLE [{schemaName}].[{tableName}] (");
		for (int i = 0; i < schema.Rows.Count; i++) {   // Build column definitions from DataRows
			DataRow row = schema.Rows[i];
			string name = $"[{row["Name"].ToString()}]";
			string salesforceType = row["Type"].ToString();
			int length = Convert.ToInt32(row["Length"]);
			string sqlType = mapToSqlType(salesforceType, length, name);
			string nullability = row["Nullable"].ToString() == "true" ? "NULL" : "NOT NULL";
			string dflt = row["Default"] != "" ? $"DEFAULT {row["Default"]}" : "";
			string columnDefinition = $"{name} {sqlType} {nullability} {dflt}";
			sql.Append($"        {columnDefinition}");
			if (i < schema.Rows.Count - 1 || schema.Columns.Contains("Id"))
				sql.Append(",");
			sql.AppendLine();
			}
		bool hasIdColumn = false;// Add primary key constraint for Id if present
		foreach (DataRow row in schema.Rows) {
			if (row["Name"].ToString() == "Id") {
				hasIdColumn = true;
				break;
				}
			}
		if (hasIdColumn) {
			sql.AppendLine($"        CONSTRAINT PK_{tableName} PRIMARY KEY (Id)");
			}
		sql.AppendLine("    );");
		sql.AppendLine("END");
		return sql.ToString();
		}
	public List<string> GetChangeEventUrls(DataTable sfoTables) {
		return sfoTables.AsEnumerable()
		.Select(row => {
			string name = row.Table.Columns.Contains("name") && !string.IsNullOrEmpty(row["name"]?.ToString())
				? row["name"].ToString() : row["ObjectName"].ToString();
			return $"/data/{SalesforceService.ObjectNameToChangeEvent(name!)}";
		})
		.OrderBy(name => name)
		.ToList();
		}
	public (int RowsInserted, string TableName) RegisterExludedCDCFields(string xml) {
		if (string.IsNullOrWhiteSpace(xml))
			throw new ArgumentException("XML input cannot be empty.", nameof(xml));
		try {
			using (SqlConnection conn = new SqlConnection(_connectionString)) {
				conn.Open();
				using (SqlCommand cmd = new SqlCommand("xprRegisterCDCobject", conn)) {
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.AddWithValue("@XmlInput", xml);// Add XML input parameter
					using (SqlDataReader reader = cmd.ExecuteReader()) {    // Execute and read output
						if (reader.Read()) {
							int rowsInserted = reader.GetInt32(0); // RowsInserted
							string tableName = reader.GetString(1); // TableName
							RaisSqlEvent($"{rowsInserted} rows insert to {tableName}", SqlEvents.Inserted, LogLevel.Info, false);
							return (rowsInserted, tableName);
							}
						}
					}
				}
			} catch (SqlException ex) {
			_logger.LogError($"SQL Error executing xprRegisterCDCobject: {ex.Message}");
			throw new Exception($"SQL error executing xprRegisterCDCobject: {ex.Message}", ex);
			} catch (Exception ex) {
			_logger.LogError($"Error processing XML: {ex.Message}");
			throw new Exception($"Error processing XML: {ex.Message}", ex);
			}

		throw new Exception("No results returned from stored procedure.");
		}
	public void AssertCDCObjectExist(string objectName, string schemaName = "sfo") {
		using (SqlConnection conn = new SqlConnection(_connectionString)) {
			conn.Open();
			try {
				using (SqlCommand cmd = new SqlCommand("SELECT dbo.fnObjectid(@context,@ObjectName)", conn)) {
					cmd.Parameters.AddWithValue("@context", schemaName);
					cmd.Parameters.AddWithValue("@ObjectName", objectName);
					object result = cmd.ExecuteScalar();
					int rowNum = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : -1;
					RaisSqlObjectExist(int.Parse(rowNum.ToString()!), objectName, "Table", (rowNum > 0), cmd.CommandText);
					}
				} catch (Exception ex) {
				_logger.LogError($"Error executing SQL: {ex.Message}");
				throw;
				}
			}
		}
	public bool AssertRecord(string ObjectName, string recordId, string schemaName = "sfo") {
		using (SqlConnection conn = new SqlConnection(_connectionString)) {
			conn.Open();
			try {
				using (SqlCommand cmd = new SqlCommand($"SELECT COUNT(*) FROM {schemaName}.[{ObjectName}] WHERE Id = @Id", conn)) {
					cmd.Parameters.AddWithValue("@Id", recordId);
					int count = (int)cmd.ExecuteScalar();
					return count > 0;
					}
				} catch (Exception ex) {
				_logger.LogError($"Error executing SQL: {ex.Message}");
				throw;
				}
			}
		}
	public int ExececuteScalar(string query) {
		using (SqlConnection conn = new SqlConnection(_connectionString)) {
			conn.Open();
			try {
				using (SqlCommand cmd = new SqlCommand(query)) {
					int count = (int)cmd.ExecuteScalar();
					return count;
					}
				} catch (Exception ex) {
				_logger.LogError($"Error executing SQL: {ex.Message}");
				throw;
				}
			}
		}
	public string ExecuteXmlQuery(string sql) {
		_logger.LogDebug("Executing XML query: {sql}", sql);
		try {
			using (var connection = new SqlConnection(_connectionString)) {
				connection.Open();
				using (var command = new SqlCommand(sql, connection)) {
					command.CommandType = CommandType.Text;
					command.CommandTimeout = 60;
					using (var reader = command.ExecuteReader()) {
						if (reader.Read()) {
							using var tr = reader.GetTextReader(0);
							string xml = tr.ReadToEnd();
							_logger.LogInformation("Retrieved XML length: {length} characters", xml.Length);
							return xml;
							}
						_logger.LogWarning("No XML data returned from query.");
						return string.Empty;
						}
					}
				}
			} catch (SqlException ex) {
			_logger.LogError(ex, "SQL Exception in ExecuteXmlQuery: {message}", ex.Message);
			throw;
			} catch (Exception ex) {
			_logger.LogError(ex, "Error executing XML query: {message}", ex.Message);
			throw;
			}
		}
	public int DeleteRecord(string tableName, string recordId) {
		string stmt = $"DELETE FROM sfo.[{tableName}] WHERE Id = '{recordId}'";
		return ExecuteNoneQuery(stmt);
		}
	private string columnList(DataTable dt) {
		string cList = string.Join(",", dt.Columns.Cast<DataColumn>()
		.Select(col => $"[{col.ColumnName}]"));
		return cList;
		}
	public void UpdateServerTable(DataTable modifiedTable, string selectStatment, string schema = "sfo") {
		try {
			string fIndex = selectStatment.IndexOf(" FROM ", StringComparison.OrdinalIgnoreCase) > 0 ? "FROM " : "from ";
			string bf = selectStatment.Substring(0, selectStatment.IndexOf(fIndex, StringComparison.OrdinalIgnoreCase) + fIndex.Length);
			string stmt = $"{bf} {schema}." + selectStatment.Substring(selectStatment.IndexOf(fIndex, StringComparison.OrdinalIgnoreCase) + fIndex.Length).TrimStart();
			using (SqlConnection conn = new SqlConnection(_connectionString)) {
				conn.Open();
				SqlDataAdapter da = new SqlDataAdapter(stmt, conn);
				SqlCommandBuilder cb = new SqlCommandBuilder(da);
				da.UpdateCommand = cb.GetUpdateCommand();
				da.Update(modifiedTable);
				}
			} catch (SqlException ex) {
			_logger.LogError($"SQL Error: {ex.Message}");
			} catch (Exception ex) {
			_logger.LogError($"Error: {ex.Message}");
			}
		}
	public enum enmIsTo {
		Insert,
		Update,
		Delete,
		Create
		}
	public async Task UpdateOrInsertRecordAsync(DataTable dataTable, string recordId, enmIsTo isTo, string dBschemaName = "sfo") {// update the sql server if exist, insert otherwise
		switch (isTo) {
			case enmIsTo.Update:
				_logger.LogInformation($"Update event received On {dataTable.TableName},Forcing Insert on RecordId={recordId} by deleting it..");
				DeleteRecord(dataTable.TableName, recordId);
				isTo = enmIsTo.Create;
				goto case enmIsTo.Create;

			case enmIsTo.Create:
				_logger.LogDebug($"{isTo.ToString()}, {dataTable}->{recordId}");
				if (!AssertRecord(dataTable.TableName, dataTable.Rows[0]["Id"]?.ToString() ?? throw new InvalidOperationException("Record ID is null"), dBschemaName)) {
					DataTable dt = await _salesforceService.GetSalesforceRecord(dataTable.TableName, recordId);
					await InsertRecordAsync(dt, dBschemaName);
					RaiseSqlTableEvent(dt, SqlEvents.Inserted);
					return;
					}
				break;
			default:
				throw new InvalidOperationException($"Record with Id {dataTable.Rows[0]["Id"]} does not exist in table {dataTable.TableName}.");
			}
		if (dataTable == null || dataTable.Rows.Count == 0) throw new ArgumentException("DataTable is empty or null.");
		string tableName = dataTable.TableName;
		try {
			using (var connection = new SqlConnection(_connectionString)) {
				await connection.OpenAsync();
				DataTable schemaTable = await getTableSchemaAsync(connection, tableName);
				DataRow row = dataTable.Rows[0];
				var dtColumns = dataTable.Columns.Cast<DataColumn>()
					.Select(col => col.ColumnName).ToList();
				var validColumns = schemaTable.AsEnumerable()
					.Where(s => dtColumns.Any(dtCol => dtCol.Equals(s.Field<string>("COLUMN_NAME"), StringComparison.OrdinalIgnoreCase)))
					.Select(s => new ColumnMetadata {
						ColumnName = s.Field<string>("COLUMN_NAME"),
						DataType = s.Field<string>("DATA_TYPE"),
						IsNullable = s.Field<string>("IS_NULLABLE") == "YES",
						MaxLength = s.IsNull("CHARACTER_MAXIMUM_LENGTH") ? -1 : s.Field<int>("CHARACTER_MAXIMUM_LENGTH")
						}).ToList();
				if (!validColumns.Any()) throw new Exception("No matching columns found between DataTable and SQL Server table schema.");
				var primaryKeyColumn = validColumns.First();
				var updateAssignments = string.Join(", ", validColumns
					.Skip(1) // Skip primary key column for updates
					.Select(c => $"{c.ColumnName} = @{c.ColumnName}"));
				string sql = $"UPDATE {dBschemaName}.[{tableName}] SET {updateAssignments} WHERE {primaryKeyColumn.ColumnName} = @{primaryKeyColumn.ColumnName}";
				using (var command = new SqlCommand(sql, connection)) {
					AddParametersToCommand(command, validColumns, row, dtColumns);
					int rowsAffected = await command.ExecuteNonQueryAsync();
					if (rowsAffected == 0)
						throw new Exception("No records were updated. Record not found or data unchanged.");
					}
				}
			} catch (SqlException ex) {
			throw new Exception($"SQL Server error during update: {ex.Message}", ex);
			} catch (Exception ex) {
			throw new Exception($"Error updating record in SQL Server: {ex.Message}", ex);
			}
		}
	public async Task InsertRecordAsync(DataTable dataTable, string schemaName = "sfo") {
		if (dataTable == null || dataTable.Rows.Count == 0) throw new ArgumentException("DataTable is empty or null.");
		string tableName = dataTable.TableName;
		try {
			using (var connection = new SqlConnection(_connectionString)) {
				await connection.OpenAsync();
				DataTable schemaTable = await getTableSchemaAsync(connection, tableName);// Retrieve schema using ftSfoSchema function
				DataRow row = dataTable.Rows[0];// Get the first row from DataTable
				var dtColumns = dataTable.Columns.Cast<DataColumn>()// Map DataTable columns to SQL Server schema (case-insensitive)
					.Select(col => col.ColumnName).ToList();
				var validColumns = schemaTable.AsEnumerable()
					.Where(s => dtColumns.Any(dtCol => dtCol.Equals(s.Field<string>("COLUMN_NAME"), StringComparison.OrdinalIgnoreCase)))
					.Select(s => new ColumnMetadata {
						ColumnName = s.Field<string>("COLUMN_NAME"),
						DataType = s.Field<string>("DATA_TYPE"),
						IsNullable = s.Field<string>("IS_NULLABLE") == "YES",
						MaxLength = s.IsNull("CHARACTER_MAXIMUM_LENGTH") ? -1 : s.Field<int>("CHARACTER_MAXIMUM_LENGTH")
						}).ToList();
				if (!validColumns.Any()) throw new Exception("No matching columns found between DataTable and SQL Server table schema.");
				var columnNames = string.Join(", ", validColumns.Select(c => c.ColumnName));// Build the SQL INSERT statement
				var parameterNames = string.Join(", ", validColumns.Select(c => $"@{c.ColumnName}"));
				string sql = $"INSERT INTO {schemaName}.[{tableName}] ({columnNames}) VALUES ({parameterNames})";
				using (var command = new SqlCommand(sql, connection)) {
					AddParametersToCommand(command, validColumns, row, dtColumns);// Add parameters using the reusable method
					await command.ExecuteNonQueryAsync();
					}
				}
			} catch (SqlException ex) {
			throw new Exception($"SQL Server error during insert: {ex.Message}\r\n{ex.StackTrace}", ex);
			} catch (Exception ex) {
			throw new Exception($"Error inserting record into SQL Server: {ex.Message}", ex);
			}
		}
	#region helpers (private)
	private void AddParametersToCommand(SqlCommand command, List<ColumnMetadata> validColumns, DataRow row, List<string> dtColumns) {
		foreach (var col in validColumns) {
			// Find matching DataTable column (case-insensitive)
			var dtColName = dtColumns.FirstOrDefault(c => c.Equals(col.ColumnName, StringComparison.OrdinalIgnoreCase))
				?? throw new Exception($"Column {col.ColumnName} not found in DataTable.");

			object value = row[dtColName];

			// Handle null values

			if (value == null || value == DBNull.Value || value.Equals(string.Empty)) {
				if (!col.IsNullable)
					throw new Exception($"Column {col.ColumnName} is not nullable but received a null value.");
				command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.NVarChar) { Value = DBNull.Value });
				continue;
				}

			// Handle data type conversion and validation
			switch (col.DataType.ToLower()) {
				case "varchar":
				case "nvarchar":
					string stringValue = value.ToString();
					if (col.MaxLength > 0 && stringValue.Length > col.MaxLength)
						throw new Exception($"Value for {col.ColumnName} exceeds maximum length of {col.MaxLength}.");
					command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.NVarChar, col.MaxLength) { Value = stringValue });
					break;

				case "int":
					if (!int.TryParse(value.ToString(), out int intValue))
						throw new Exception($"Cannot convert value for {col.ColumnName} to int.");
					command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.Int) { Value = intValue });
					break;

				case "bigint":
					if (!long.TryParse(value.ToString(), out long longValue))
						throw new Exception($"Cannot convert value for {col.ColumnName} to bigint.");
					if (longValue < 0)
						throw new Exception($"Unix epoch timestamp for {col.ColumnName} cannot be negative.");
					command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.BigInt) { Value = longValue });
					break;

				case "decimal":
				case "numeric":
					if (!decimal.TryParse(value.ToString(), out decimal decimalValue))
						throw new Exception($"Cannot convert value for {col.ColumnName} to decimal.");
					command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.Decimal) { Value = decimalValue });
					break;

				case "float":
					string floatString = value.ToString();
					if (string.IsNullOrEmpty(floatString)) {
						if (!col.IsNullable)
							throw new Exception($"Column {col.ColumnName} is not nullable but received an empty string.");
						command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.Float) { Value = DBNull.Value });
						} else {
						if (!double.TryParse(floatString, out double doubleValue))
							throw new Exception($"Cannot convert value for {col.ColumnName} to float.");
						command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.Float) { Value = doubleValue });
						}
					break;

				case "datetime":
				case "date":
					if (long.TryParse(value?.ToString(), out long unixMillis) && unixMillis >= 0) {
						// Convert Unix epoch milliseconds to DateTime (UTC)
						DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
						DateTime parsedDate = epochStart.AddMilliseconds(unixMillis);
						command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.DateTime) { Value = parsedDate });
						} else if (DateTime.TryParse(value?.ToString(), out DateTime parsedDateFromString)) {
						// Handle string-based DateTime parsing
						command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.DateTime) { Value = parsedDateFromString });
						} else {
						// Handle invalid or null values
						command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.DateTime) { Value = DBNull.Value });
						}
					break;
				case "bit":
					if (!bool.TryParse(value.ToString(), out bool boolValue))
						throw new Exception($"Cannot convert value for {col.ColumnName} to bit.");
					command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.Bit) { Value = boolValue });
					break;

				case "uniqueidentifier":
					if (!Guid.TryParse(value.ToString(), out Guid guidValue))
						throw new Exception($"Cannot convert value for {col.ColumnName} to uniqueidentifier.");
					command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.UniqueIdentifier) { Value = guidValue });
					break;

				default:
					command.Parameters.Add(new SqlParameter($"@{col.ColumnName}", SqlDbType.NVarChar) { Value = value.ToString() });
					break;
				}

			}
		}
	private async Task<DataTable> getTableSchemaAsync(SqlConnection connection, string tableName) {
		var schemaTable = new DataTable();
		string sql = "SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE FROM dbo.ftSfoSchema(@TableName)";
		using (var command = new SqlCommand(sql, connection)) {
			command.Parameters.AddWithValue("@TableName", tableName);
			using (var adapter = new SqlDataAdapter(command)) adapter.Fill(schemaTable);
			}
		if (schemaTable.Rows.Count == 0) throw new Exception($"No schema found for table {tableName} in schema sfo.");
		return schemaTable;
		}
	private static string mapToSqlType(string salesforceType, int length, string columnName) {
		return salesforceType.ToLower() switch {
			"string" => length > 0 && length <= 8000 ? $"VARCHAR({length})" : "NVARCHAR(MAX)",
			"reference" => length > 0 && length <= 8000 ? $"VARCHAR({length})" : "NVARCHAR(MAX)",
			"picklist" => length > 0 && length <= 8000 ? $"VARCHAR({length})" : "NVARCHAR(MAX)",
			"multipicklist" => length > 0 && length <= 8000 ? $"VARCHAR({length})" : "NVARCHAR(MAX)",
			"id" => length > 0 && length <= 8000 ? $"NVARCHAR({length})" : "NVARCHAR(MAX)",
			"boolean" => "BIT",
			"int" => "INT",
			"long" => "BIGINT",
			"double" => "FLOAT",
			"currency" => "MONEY",
			"date" => "DATE",
			"datetime" => "DATETIME",
			"textarea" => "TEXT",
			"url" => "VARCHAR(MAX)",
			"encryptedstring" => "VARBINARY(MAX)",
			"email" => "NVARCHAR(80)",
			"address" => "NVARCHAR(4000)",
			"phone" => "NVARCHAR(80)",
			"anytype" => "SQL_VARIANT",
			"complexvalue" => "NVARCHAR(MAX)",
			"combobox" => "NVARCHAR(255)",
			"json" => "NVARCHAR(MAX)",
			"percent" => "DECIMAL(5,2)",
			"time" => "TIME",
			"base64" => "VARCHAR(MAX)",
			"location" => "DECIMAL(9,6)",
			_ => throw new NotSupportedException($"Salesforce type {salesforceType} for column {columnName} is not supported.")
			};
		}
	#endregion helpers (private)
	}
#endregion	Public Methods


