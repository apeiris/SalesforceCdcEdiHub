using System.Data;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avro;
using Avro.Generic;
using Avro.IO;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static NetUtils.PubSub;

using NetUtils;

//using static Salesforce.PubSub;
namespace Common;
public class ProgressUpdateEventArgs : EventArgs {
	public string Message { get; }
	public ProgressUpdateEventArgs(string message) {
		Message = message;
		}
	}
public class CDCEventArgs {
	public string EntityName { get; set; }
	public List<string> RecordIds { get; set; }
	public string ChangeType { get; set; }
	public List<string> ChangedFields { get; set; }
	public DataTable DeltaFields { get; set; }
	public string ReplayId { get; set; }
	public string Error { get; set; }
	public CDCEventArgs() {
		RecordIds = new List<string>();
		ChangedFields = new List<string>();
		DeltaFields = new DataTable();
		DeltaFields.Columns.Add("FieldName", typeof(string));
		DeltaFields.Columns.Add("Value", typeof(string));
		}
	}
public class PubSubService : IDisposable {
	private Dictionary<string, string> _replayIds = new Dictionary<string, string>();
	private bool _DebugBreak = false;
	private readonly ISalesforceService _oauthService;
	private readonly SalesforceConfig _config;
	private readonly GrpcChannel _channel;
	private readonly PubSubClient _client;
	private readonly ILogger<PubSubService> _logger;
	private readonly List<(AsyncDuplexStreamingCall<FetchRequest, FetchResponse> Call, CancellationTokenSource Cts)> _subscriptions;
	private readonly Dictionary<string, RecordSchema> _schemaCache;
	#region events
	public event EventHandler<ProgressUpdateEventArgs> ProgressUpdated;
	public event EventHandler<CDCEventArgs> CDCEvent;
	public static event EventHandler<string> LogEmit;
	#endregion events
	public bool DebugBreak { get { return _DebugBreak; } set { _DebugBreak = value; } }
	public PubSubService(ISalesforceService oauthService, IOptions<SalesforceConfig> configOptions, ILogger<PubSubService> logger) {
		_oauthService = oauthService ?? throw new ArgumentNullException(nameof(oauthService));
		_config = configOptions?.Value ?? throw new ArgumentNullException(nameof(configOptions));
		_channel = GrpcChannel.ForAddress(_config.GrpcUrl, new GrpcChannelOptions {
			Credentials = ChannelCredentials.SecureSsl
			});
		_client = new PubSubClient(_channel);
		_subscriptions = new List<(AsyncDuplexStreamingCall<FetchRequest, FetchResponse>, CancellationTokenSource)>();
		_schemaCache = new Dictionary<string, RecordSchema>();
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}
	public async Task StartSubscriptionsAsync(HashSet<string> topics) {
		var (token, instanceUrl, tenantId) = await _oauthService.GetAccessTokenAsync(); //ArgumentNullException.ThrowIfNull(topics, nameof(topics));
		if (token == null) throw new InvalidOperationException("Failed to obtain access token.");
		try {
			await foreach (var topic in topics.ToAsyncEnumerable()) {
				if (string.IsNullOrWhiteSpace(topic)) {
					_logger.LogWarning("Skipping null or empty topic.");
					continue;
					}
				await SubscribeToTopicAsync(topic, token!, instanceUrl!, tenantId!, null);
				_logger.LogInformation("Subscribed to topic: {Topic}", topic);
				}
			OnProgressUpdated($"Subscribed to {topics.Count} topic(s) successfully.");
			} catch (Exception ex) {
			_logger.LogError(ex, "Failed to start subscriptions for topics.");
			throw;
			}
		}
	private void DecodeChangeEvent(byte[] payload, RecordSchema schema, List<string> fieldsToFilter) {
		var result = new CDCEventArgs();

		if (DebugBreak) Debugger.Break();

		try {
			using var stream = new MemoryStream(payload);
			var reader = new BinaryDecoder(stream);
			var datumReader = new GenericDatumReader<GenericRecord>(schema, schema);
			var record = datumReader.Read(null, reader);

			// Setup filter set if provided
			var filterSet = fieldsToFilter != null ? new HashSet<string>(fieldsToFilter) : new HashSet<string>();

			// Extract ChangeEventHeader
			if (!record.TryGetValue("ChangeEventHeader", out object headerObj) || headerObj is not GenericRecord header) {
				result.Error = "No Header on ChangeEvent";
				CDCEvent?.Invoke(this, result);
				return;
				}

			// Populate result from header
			result.ChangeType = getChangeType(header.ToString());
			result.RecordIds = header.TryGetValue("recordIds", out var rIds) && rIds is IList<object> idsList
				? idsList.Select(id => id.ToString()).ToList()
				: new List<string>();
			result.ChangedFields = header.TryGetValue("changedFields", out var cf) && cf is IList<object> cfList
				? cfList.Select(f => f.ToString()).ToList()
				: new List<string>();
			header.TryGetValue("ReplayId", out _); // ignored for now

			string entityName = header.TryGetValue("entityName", out var en) ? en?.ToString() ?? "Unknown" : "Unknown";

			//LogEmit?.Invoke(this,
			//	$"Event ChangeType={result.ChangeType}, ChangedFields={string.Join(",", result.ChangedFields)}, RecordIds={string.Join(",", result.RecordIds)}");
			_logger.LogInformation($"Event ChangeType={result.ChangeType}, ChangedFields={string.Join(",", result.ChangedFields)}, RecordIds={string.Join(",", result.RecordIds)}");

			// Handle DELETE differently (only need entity + ids)
			if (result.ChangeType == "DELETE") {
				var deleteTable = new DataTable(entityName);
				deleteTable.Columns.Add("Id", typeof(string));
				foreach (var id in result.RecordIds) {
					var row = deleteTable.NewRow();
					row["Id"] = id;
					deleteTable.Rows.Add(row);
					}
				deleteTable.PrimaryKey = new[] { deleteTable.Columns["Id"]! };
				result.DeltaFields = deleteTable;

				CDCEvent?.Invoke(this, result);
				return;
				}

			// CREATE / UPDATE / other events
			var table = new DataTable(entityName);
			table.Columns.Add("FieldName", typeof(string));
			table.Columns.Add("Value", typeof(string));
			table.Columns.Add("DataType", typeof(string));

			foreach (var field in schema.Fields) {
				if (field.Name == "ChangeEventHeader") continue;

				if (!record.TryGetValue(field.Name, out var value) || value == null) continue;
				if (filterSet.Count > 0 && !filterSet.Contains(field.Name)) continue;

				var row = table.NewRow();
				row["FieldName"] = field.Name;
				row["Value"] = value.ToString();

				// Try to parse datatype from documentation string
				string dataType = field.Documentation?.Split(':').LastOrDefault() ?? "Unknown";
				row["DataType"] = dataType;

				table.Rows.Add(row);
				}

			// Add Id row explicitly
			if (result.RecordIds.Count > 0) {
				var idRow = table.NewRow();
				idRow["FieldName"] = "Id";
				idRow["Value"] = result.RecordIds[0];
				idRow["DataType"] = "Id";
				table.Rows.Add(idRow);

				table.PrimaryKey = new[] { table.Columns["FieldName"]! };
				}

			result.DeltaFields = table;
			CDCEvent?.Invoke(this, result);
			} catch (Exception ex) {
			result.Error = ex.Message;
			CDCEvent?.Invoke(this, result);
			}
		}
	//private async Task SubscribeToTopicAsync(string topic, string token, string instanceUrl, string tenantId, List<string> fieldsToFilter) {
	//	OnProgressUpdated($"Subscribing to {topic}...");
	//	var fetchRequest = new FetchRequest {
	//		TopicName = topic,
	//		NumRequested = 10,
	//		ReplayPreset = ReplayPreset.Latest,

	//	};

	//	var callOptions = new CallOptions(credentials: CustomGrpcCredentials.Create(token, instanceUrl, tenantId));
	//	try {
	//		var streamingCall = _client.Subscribe(callOptions);

	//		await streamingCall.RequestStream.WriteAsync(fetchRequest);

	//		var cts = new CancellationTokenSource();
	//		_subscriptions.Add((streamingCall, cts));

	//		_ = Task.Run(async () => {
	//			try {
	//				while (await streamingCall.ResponseStream.MoveNext(cts.Token)) {
	//					var response = streamingCall.ResponseStream.Current;
	//					string responseJson = response.ToString();
	//					var jsonDoc = JsonDocument.Parse(responseJson);
	//					if (jsonDoc.RootElement.TryGetProperty("events", out var eventsElement)) {
	//						var eventsArray = eventsElement.EnumerateArray();
	//						foreach (var evt in eventsArray) {
	//							var eventObj = evt.GetProperty("event");
	//							string schemaId = eventObj.GetProperty("schemaId").GetString()!;
	//							string payloadBase64 = eventObj.GetProperty("payload").GetString()!;
	//							byte[] payload = Convert.FromBase64String(payloadBase64)!;
	//							RecordSchema schema = await GetSchemaAsync(schemaId, token, instanceUrl, tenantId);
	//							DecodeChangeEvent(payload, schema, fieldsToFilter);
	//						}
	//						await streamingCall.RequestStream.WriteAsync(fetchRequest);
	//					}
	//				}
	//			} catch (Exception ex) {
	//				Debug.WriteLine($"Error listening to {topic}: {ex.Message}\r\n");
	//			}
	//		});
	//	} catch (RpcException ex) {
	//		Debug.WriteLine($"gRPC Error for {topic}: {ex.StatusCode}, Detail: {ex.Status.Detail}\r\n");
	//	} catch (Exception ex) {
	//		Debug.WriteLine($"Unexpected error for {topic}: {ex.Message}\r\n");
	//	}
	//}

	private async Task SubscribeToTopicAsync(
	string topic,
	string token,
	string instanceUrl,
	string tenantId,
	List<string> fieldsToFilter) {
		OnProgressUpdated($"Subscribing to {topic}...");

		var fetchRequest = new FetchRequest {
			TopicName = topic,
			NumRequested = 10,
			ReplayPreset = ReplayPreset.Latest,
			};

		var callOptions = new CallOptions(
			credentials: CustomGrpcCredentials.Create(token, instanceUrl, tenantId));

		try {
			var streamingCall = _client.Subscribe(callOptions);
			var cts = new CancellationTokenSource();
			_subscriptions.Add((streamingCall, cts));

			// Send initial request
			await streamingCall.RequestStream.WriteAsync(fetchRequest);

			_ = Task.Run(async () => {
				try {
					while (await streamingCall.ResponseStream.MoveNext(cts.Token)) {
						var response = streamingCall.ResponseStream.Current;
						using var jsonDoc = JsonDocument.Parse(response.ToString());

						if (!jsonDoc.RootElement.TryGetProperty("events", out var eventsElement))
							continue;

						foreach (var evt in eventsElement.EnumerateArray()) {
							try {
								var eventObj = evt.GetProperty("event");
								string schemaId = eventObj.GetProperty("schemaId").GetString()
									?? throw new InvalidOperationException("SchemaId missing");
								string payloadBase64 = eventObj.GetProperty("payload").GetString()
									?? throw new InvalidOperationException("Payload missing");

								byte[] payload = Convert.FromBase64String(payloadBase64);
								RecordSchema schema = await GetSchemaAsync(schemaId, token, instanceUrl, tenantId);
								DecodeChangeEvent(payload, schema, fieldsToFilter);
								} catch (Exception innerEx) {
								Debug.WriteLine($"Error decoding event for {topic}: {innerEx.Message}\r\n");
								}
							}

						// Request next batch
						await streamingCall.RequestStream.WriteAsync(fetchRequest);
						}
					} catch (Exception ex) {
					Debug.WriteLine($"Error listening to {topic}: {ex.Message}\r\n");
					}
			}, cts.Token);
			} catch (RpcException ex) {
			Debug.WriteLine($"gRPC Error for {topic}: {ex.StatusCode}, Detail: {ex.Status.Detail}\r\n");
			} catch (Exception ex) {
			Debug.WriteLine($"Unexpected error for {topic}: {ex.Message}\r\n");
			}
		}

	public async Task<RecordSchema> GetSchemaAsync(string schemaId, string token, string instanceUrl, string tenantId) {
		if (_schemaCache.TryGetValue(schemaId, out RecordSchema cachedSchema)) {
			return cachedSchema;
			}
		var schemaRequest = new SchemaRequest { SchemaId = schemaId };
		var callOptions = new CallOptions(credentials: CustomGrpcCredentials.Create(token, instanceUrl, tenantId));
		try {
			var schemaResponse = await _client.GetSchemaAsync(schemaRequest, callOptions);
			var schema = Schema.Parse(schemaResponse.SchemaJson) as RecordSchema;
			_schemaCache[schemaId] = schema;
			return schema;
			} catch (RpcException ex) {
			throw;
			}
		}
	private string getChangeType(string headerString) {
		var match = Regex.Match(headerString, @"value:\s*(\w+)");
		return match.Success && match.Groups.Count > 1 ? match.Groups[1].Value : "Unknown";
		}
	protected virtual void OnProgressUpdated(string message) {
		ProgressUpdated?.Invoke(this, new ProgressUpdateEventArgs(message));
		}
	public void Dispose() {
		foreach (var (call, cts) in _subscriptions) {
			cts.Cancel();
			call.Dispose();
			}
		_subscriptions.Clear();
		_channel.Dispose();
		}
	}
