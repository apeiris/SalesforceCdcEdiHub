
using System.Data;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Salesforce;
//using Salesforce.SDK.Net;
using static System.Net.WebRequestMethods;
using HttpMethod = System.Net.Http.HttpMethod;
using System.IdentityModel.Tokens.Jwt;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Microsoft.IdentityModel.Tokens;
using Common;

//namespace SalesforceCdcEdiHub.SalesforceService;
namespace SalesforceCdcEdiHub;
public class SalesforceService : ISalesforceService {
	private readonly HttpClient _httpClient;
	private readonly SalesforceConfig _settings;
	private readonly ILogger<SalesforceService> _logger;
	private static readonly object _tokenLock = new object();
	private static string _cachedToken;
	private static string _cachedInstanceUrl;
	private static DateTimeOffset? _tokenExpiresAt;
	public event EventHandler<AuthenticationEventArgs> AuthenticationAttempt;
	public SalesforceService(HttpClient httpClient, IOptions<SalesforceConfig> settings, ILogger<SalesforceService> logger) {
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_logger.LogDebug("SalesforceService initialized with settings: {Settings}", _settings);
		}
	public class FieldMeta {
		public string Name { get; set; }
		public string Type { get; set; }
		public string Label { get; set; }
		public uint Length { get; set; }
		public bool Nullable { get; set; }
		public string DefaultValue { get; set; }
		public string RelationshipName { get; set; }
		public List<string> ReferenceTo { get; set; }
		}
	public async Task<JsonElement> CreatePlatformEventChannelMemberFromEntityDefinition(string entityDefinitionId, string platformEventChannelId, CancellationToken cancellationToken = default) {
		var (token, instanceUrl, expiresAt) = await GetAccessTokenAsync();
		string objectName = "PlatformEventChannelMember";
		string url = $"{instanceUrl}/services/data/v{_settings.ApiVersion}/tooling/sobjects/PlatformEventChannelMember";
		_logger.LogDebug("Posting to {ObjectName} at {Url}", objectName, url);

		// Construct the payload for PlatformEventChannelMember
		var payload = new {
			EntityDefinitionId = entityDefinitionId,
			PlatformEventChannelId = platformEventChannelId
			// Add other required fields for PlatformEventChannelMember if needed
			};
		string jsonPayload = JsonSerializer.Serialize(payload);

		using var request = new HttpRequestMessage(HttpMethod.Post, url);
		request.Headers.Add("Authorization", $"Bearer {token}");
		request.Headers.Add("Accept", "application/json");
		request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

		try {
			using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
			if (!response.IsSuccessStatusCode) {
				string errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("API POST failed for {ObjectName}: Status={StatusCode}, Content={Content}",
					objectName, response.StatusCode, errorContent);
				throw new Exception($"Failed to POST to url:{url}\n : {response.ReasonPhrase}");
				}

			await using var stream = await response.Content.ReadAsStreamAsync();
			JsonElement result = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: cancellationToken);
			return result;
			} catch (Exception ex) {
			_logger.LogError("Error during POST for {ObjectName}: {Message}", objectName, ex.Message);
			throw;
			}
		}
	public async Task<JsonElement> GetPlaformEventChannelMembers(CancellationToken cancellationToken = default) {
		var (token, instanceUrl, expiresAt) = await GetAccessTokenAsync();
		string objectName = "PlatformEventChannel";
		string url = $"{instanceUrl}/services/data/v{_settings.ApiVersion}/tooling/sobjects/PlatformEventChannelMember";
		_logger.LogDebug("Posting to {ObjectName} at {Url}", objectName, url);

		using var request = new HttpRequestMessage(HttpMethod.Post, url);
		request.Headers.Add("Authorization", $"Bearer {token}");
		request.Headers.Add("Accept", "application/json");
		request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
		try {
			using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
			if (!response.IsSuccessStatusCode) {
				string errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("API POST failed for {ObjectName}: Status={StatusCode}, Content={Content}",
					objectName, response.StatusCode, errorContent);
				throw new Exception($"Failed to POST to url:{url}\n : {response.ReasonPhrase}");
				}
			await using var stream = await response.Content.ReadAsStreamAsync();
			JsonElement result = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: cancellationToken);
			return result;
			} catch (Exception ex) {
			_logger.LogError("Error during POST for {ObjectName}: {Message}", objectName, ex.Message);
			throw;
			}
		}
	public async Task<JsonElement> GetObjectSchemaAsync(string objectName, CancellationToken cancellationToken = default, bool useTooling = false) {
		if (string.IsNullOrWhiteSpace(objectName)) {
			throw new ArgumentException("Object name cannot be empty or null", nameof(objectName));
			}
		var (token, instanceUrl, expiresAt) = await GetAccessTokenAsync();
		string apiVersion = string.IsNullOrEmpty(_settings.ApiVersion) ? "63.0" : _settings.ApiVersion;
		string url = !useTooling ? $"{instanceUrl}/services/data/v{apiVersion}/sobjects/{Uri.EscapeDataString(objectName)}/describe/" : $"{instanceUrl}/services/data/v{_settings.ApiVersion}/tooling/sobjects/{Uri.EscapeDataString(objectName)}/describe/";
		_logger.LogDebug("Fetching schema for {ObjectName} from {Url}", objectName, url);
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Add("Authorization", $"Bearer {token}");
		request.Headers.Add("Accept", "application/json");
		try {
			using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
			if (!response.IsSuccessStatusCode) {
				string errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("API request failed for {ObjectName}: Status={StatusCode}, Content={Content}",
					objectName, response.StatusCode, errorContent);
				throw new Exception($"Failed to retrieve schema for {objectName}: {response.ReasonPhrase}");
				}
			await using var stream = await response.Content.ReadAsStreamAsync();
			JsonElement schema;
			try {
				schema = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: cancellationToken);
				} catch (JsonException jsonEx) {
				_logger.LogError("Failed to deserialize schema for {ObjectName}: {Message}", objectName, jsonEx.Message);
				throw new Exception($"Invalid JSON response for {objectName}", jsonEx);
				}
			if (!schema.TryGetProperty("fields", out _) || !schema.TryGetProperty("childRelationships", out _)) {
				_logger.LogError("Schema for {ObjectName} missing required properties", objectName);
				throw new Exception($"Invalid schema for {objectName}: missing fields or childRelationships");
				}
			_logger.LogDebug("Successfully retrieved schema for {ObjectName}", objectName);
			return schema;
			} catch (HttpRequestException httpEx) {
			_logger.LogError("HTTP error retrieving schema for {ObjectName}: {Message}", objectName, httpEx.Message);
			throw new Exception($"Failed to retrieve schema for {objectName}: {httpEx.Message}", httpEx);
			} catch (TaskCanceledException timeoutEx) {
			_logger.LogError("Request timed out for {ObjectName}: {Message}", objectName, timeoutEx.Message);
			throw new Exception($"Schema request for {objectName} timed out", timeoutEx);
			} catch (Exception ex) {
			_logger.LogError("Unexpected error retrieving schema for {ObjectName}: {Message}", objectName, ex.Message);
			throw;
			}
		}
	private AccessTokenCache _tokenCache;
	/*
	public async Task<(string token, string instanceUrl, string tenantId)> GetAccessTokenAsync() {
		if (_tokenCache != null && _tokenCache.Expiry > DateTime.UtcNow.AddMinutes(5)) {// Check if cache exists and is not expired, with 5-minute buffer to account for call processing time
			return (_tokenCache.Token, _tokenCache.InstanceUrl, _tokenCache.TenantId);
		}
			var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.LoginUrl}/services/oauth2/token") {
				Content = new FormUrlEncodedContent(new Dictionary<string, string>
			{
						{ "grant_type", "password" }, // Use password grant type
				//	{"grant_type", "authorization_code" }, 
					{ "client_id", _settings.ClientId },
				{ "client_secret", _settings.ClientSecret },
				{ "username", _settings.Username },
				{ "password", _settings.Password }
			})
		};

		var response = await _httpClient.SendAsync(request);
		var responseContent = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode) {
			RaiseAuthenticationAttempt(LogLevel.Error, $"Authentication failed: {responseContent}");
			return (null, null, null);
		}

		var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);
		var tenantId = data!["id"].Split('/')[^2];
		_tokenCache = new AccessTokenCache {// Cache the token with 1 hour expiry, minus 5-minute buffer
			Token = data["access_token"],
			InstanceUrl = data["instance_url"],
			TenantId = tenantId,
			Expiry = DateTime.UtcNow.AddMinutes(115) // 2 hour expiry with 5-minute buffer
		};
		RaiseAuthenticationAttempt(LogLevel.Information, $"Authenticated {responseContent}");
		return (_tokenCache.Token, _tokenCache.InstanceUrl, _tokenCache.TenantId);
	}
	*/
	//private X509Certificate2 getCertifcate() {
	//	try {
	//		var cert = new X509Certificate2(_settings.pfxPath, _settings.pfxPassword, X509KeyStorageFlags.UserKeySet);
	//		return cert;
	//	} catch (Exception ex) {
	//		_logger.Log(LogLevel.Error, ex.Message);

	//		return null;
	//	}
	//}
	private string GenerateJwt() {
		var cert = new X509Certificate2(_settings.pfxPath, _settings.pfxPassword, X509KeyStorageFlags.UserKeySet);
		var rsa = cert.GetRSAPrivateKey();

		var header = new JwtHeader(new Microsoft.IdentityModel.Tokens.SigningCredentials(
		new RsaSecurityKey(rsa),
		SecurityAlgorithms.RsaSha256));
		var payload = new JwtPayload
		{
				{ "iss", _settings.ClientId }, // Replace with (Consumer Key) Client ID 
				{ "sub", _settings.Username },   // Replace with Integration User username
				{ "aud", _settings.LoginUrl }, // Use test.salesforce.com for sandbox
				{ "exp", DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds() } // 2-hour expiration
			};


		var token = new JwtSecurityToken(header, payload);
		var handler = new JwtSecurityTokenHandler();
		return handler.WriteToken(token); // Serialize the token to a string
		}
	public async Task<(string token, string instanceUrl, string tenantId)> GetAccessTokenAsync() {
		if (_tokenCache != null && _tokenCache.Expiry > DateTime.UtcNow.AddMinutes(5)) {// Check if cache exists and is not expired, with 5-minute buffer to account for call processing time
			return (_tokenCache.Token, _tokenCache.InstanceUrl, _tokenCache.TenantId);
			}

		string jwt = GenerateJwt(); // Generate JWT token
		using var client = new HttpClient();
		var content = new FormUrlEncodedContent(new[]
		{
				new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
				new KeyValuePair<string, string>("assertion", jwt)
			});


		var response = await client.PostAsync($"{_settings.LoginUrl}/services/oauth2/token", content);


		var responseContent = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode) {
			RaiseAuthenticationAttempt(LogLevel.Error, $"Authentication failed: {responseContent}");
			return (null, null, null);
			}

		var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);
		var tenantId = data!["id"].Split('/')[^2];
		_tokenCache = new AccessTokenCache {// Cache the token with 1 hour expiry, minus 5-minute buffer
			Token = data["access_token"],
			InstanceUrl = data["instance_url"],
			TenantId = tenantId,
			Expiry = DateTime.UtcNow.AddMinutes(115) // 2 hour expiry with 5-minute buffer
			};
		RaiseAuthenticationAttempt(LogLevel.Information, $"Authenticated {responseContent}");
		return (_tokenCache.Token, _tokenCache.InstanceUrl, _tokenCache.TenantId);
		}
	private void RaiseAuthenticationAttempt(LogLevel ll, string message, string instanceUrl = null,
	[CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) {
		string detailedMessage = $"{message} [Method: {callerMemberName}, Line: {callerLineNumber}]";
		AuthenticationAttempt?.Invoke(this, new AuthenticationEventArgs(ll, detailedMessage, instanceUrl));
		}
	public async Task<DataTable> GetAllObjects() {
		var (token, instanceUrl, tenantId) = await GetAccessTokenAsync();
		string url = $"{instanceUrl}/services/data/v{_settings.ApiVersion}/sobjects";
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Add("Authorization", $"Bearer {token}");
		request.Headers.Add("Accept", "application/json");

		try {
			HttpResponseMessage response = await _httpClient.SendAsync(request);
			response.EnsureSuccessStatusCode();
			string jsonResponse = await response.Content.ReadAsStringAsync();
			using var jdoc = JsonDocument.Parse(jsonResponse);

			var objects = jdoc.RootElement
				.GetProperty("sobjects")
				.EnumerateArray()
				.Where(obj => {
					string name = obj.GetProperty("name").GetString();
					return name != "Name";
				})
				.Select(obj => new { Name = obj.GetProperty("name").GetString() });
			var root = new XElement("Objects",
				objects.Select(f => new XElement("Object",
					new XElement("Name", f.Name)
				))
			);
			var dataSet = new DataSet();
			using (var reader = root.CreateReader()) dataSet.ReadXml(reader);
			if (dataSet.Tables.Count > 0) {
				dataSet.Tables[0].TableName = "sobjects";
				return dataSet.Tables[0];
				} else throw new Exception("No table created from XML.");
			} catch (HttpRequestException ex) {
			throw new Exception($"Failed to retrieve schema from {url}: {ex.Message}", ex);
			} catch (JsonException ex) {
			throw new Exception($"Failed to parse JSON response:\n{ex.Message}", ex);
			} catch (Exception ex) {
			throw new Exception($"Unexpected error: {ex.Message}", ex);
			}
		}
	public async Task<string> GetObjectSchemaSummaryAsync(string objectName) {
		JsonElement schema = await GetObjectSchemaAsync(objectName);
		var sb = new System.Text.StringBuilder();
		sb.AppendLine($"Object: {schema.GetProperty("name").GetString()}");
		sb.AppendLine($"Label: {schema.GetProperty("label").GetString()}");
		sb.AppendLine("Fields:");
		foreach (JsonElement field in schema.GetProperty("fields").EnumerateArray()) {  // Iterate through fields
			string fieldName = field.GetProperty("name").GetString();
			string fieldType = field.GetProperty("type").GetString();
			string fieldLabel = field.GetProperty("label").GetString();
			string fieldLength = field.GetProperty("length").GetUInt32().ToString();
			bool isCustom = field.GetProperty("custom").GetBoolean();
			sb.AppendLine($"- {fieldName} ({fieldType}): {fieldLabel}:{fieldLength}: {(isCustom ? "[Custom]" : "")}");
			}
		return sb.ToString();
		}
	public static List<FieldMeta> GetNonNullableFields(JsonElement schema) {
		try {
			return schema
				.GetProperty("fields")
				.EnumerateArray()
				//.Where(field => !field.GetProperty("nillable").GetBoolean()) // Only non-nullable fields
				.Select(field => {
					// Safely handle defaultValue
					string defaultValue = null;
					if (field.TryGetProperty("defaultValue", out var defaultProp)) {
						switch (defaultProp.ValueKind) {
							case JsonValueKind.String:
								defaultValue = $"'{defaultProp.GetString()}'";
								break;
							case JsonValueKind.Number:
								defaultValue = defaultProp.GetRawText();
								break;
							case JsonValueKind.True:
							case JsonValueKind.False:
								//defaultValue = defaultProp.GetBoolean().ToString().ToLower();
								defaultValue = defaultProp.GetBoolean() ? "1" : "0";// so this match the T-Sql
								break;
							case JsonValueKind.Null:
								defaultValue = null; // or "No default value" if preferred
								break;
							default:
								defaultValue = defaultProp.GetRawText(); // Fallback for unexpected types
								break;
							}
						}

					return new FieldMeta {
						Name = field.GetProperty("name").GetString(),
						Type = field.GetProperty("type").GetString(),
						Label = field.GetProperty("label").GetString(),
						Length = field.GetProperty("length").GetUInt32(),
						Nullable = field.GetProperty("nillable").GetBoolean(),
						DefaultValue = defaultValue,
						RelationshipName = field.TryGetProperty("relationshipName", out var relProp) && relProp.ValueKind != JsonValueKind.Null
							? relProp.GetString()
							: null,
						ReferenceTo = field.TryGetProperty("referenceTo", out var refProp) && refProp.ValueKind == JsonValueKind.Array
							? refProp.EnumerateArray().Select(e => e.GetString()).ToList()
							: new List<string>()
						};
				})
				.ToList();
			} catch (Exception ex) {
			throw new InvalidOperationException($"Error processing Salesforce describe schema: {ex.Message}", ex);
			}
		}
	public async Task<DataSet> GetObjectSchemaAsDataSetAsync(string objectName, bool useTooling = false) {
		var schemax = await GetObjectSchemaAsync(objectName, default, useTooling);
		JsonDocument schema = JsonDocument.Parse(schemax.GetRawText());
		DataSet ds = new DataSet();
		try {
			var fields = GetNonNullableFields(schema.RootElement);
			var childRelationNames = schema.RootElement
						.GetProperty("childRelationships")
						.EnumerateArray()
						.Where(rel => rel.TryGetProperty("relationshipName", out var prop) && prop.ValueKind == JsonValueKind.String)
						.Select(rel => new {
							Name = rel.GetProperty("relationshipName").GetString(),
							Child = rel.GetProperty("childSObject").GetString(),
							field = rel.GetProperty("field").GetString(),
							Cascade = rel.GetProperty("cascadeDelete").GetBoolean()
							});
			XElement schemaRoot = new XElement(objectName,
					fields.Select(f => new XElement(objectName,
						new XElement("Name", f.Name ?? ""),
						new XElement("Type", f.Type ?? ""),
						new XElement("Label", f.Label ?? ""),
						new XElement("Length", f.Length),
						new XElement("Nullable", f.Nullable),
						new XElement("Default", f.DefaultValue),
						new XElement("relationshipName", f.RelationshipName ?? ""),
						new XElement("referenceTo", string.Join(",", f.ReferenceTo))
					)
				),
			childRelationNames.Select(cr => new XElement($"relations",
							new XElement("Name", cr.Name ?? ""),
							new XElement("ChildSObject", cr.Child ?? ""),
							new XElement("Field", cr.field ?? ""),
							new XElement("Cascade", cr.Cascade)
						)));
			using (XmlReader xr = schemaRoot.CreateReader()) ds.ReadXml(xr);
			} catch (Exception ex) {
			_logger.LogError(ex.Message);
			_logger.LogError(ex.StackTrace);
			return null;
			}
		return ds;
		}
	public async Task<DataTable> GetSalesforceRecord(string objectName, string recordId) {
		try {
			var (token, instanceUrl, tenantId) = await GetAccessTokenAsync();// Get access token and instance URL
			string endpoint = $"{instanceUrl}/services/data/v{_settings.ApiVersion}/sobjects/{objectName}/{recordId}";// Construct the REST API endpoint for the record
			_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);   // Set up the HTTP request
			var response = await _httpClient.GetAsync(endpoint);
			if (!response.IsSuccessStatusCode) throw new Exception($"Failed to retrieve data: {response.StatusCode}");
			string jsonResponse = await response.Content.ReadAsStringAsync();
			using var jsonDoc = JsonDocument.Parse(jsonResponse);
			var fields = jsonDoc.RootElement.EnumerateObject()// Project JSON properties to a dictionary (avoiding foreach)
				.Where(prop => prop.Name != "attributes") // Exclude metadata
				.Select(prop => new KeyValuePair<string, string>(prop.Name, prop.Value.ToString()))
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			var xmlElement = new XElement("Record", fields.Select(kvp => new XElement(kvp.Key, kvp.Value)));
			string xmlString = xmlElement.ToString();
			DataTable dataTable = new DataTable(objectName);// Load XML into DataTable
			using (var xmlReader = new System.IO.StringReader(xmlString)) {
				var xdoc = XDocument.Parse(xmlString);
				var columns = xdoc.Descendants("Record").Elements()
					.Select(e => e.Name.LocalName)
					.Distinct();
				columns.ToList().ForEach(col => dataTable.Columns.Add(col));// Create columns in DataTable
				var row = dataTable.NewRow();// Add row to DataTable
				fields.ToList().ForEach(kvp => row[kvp.Key] = kvp.Value);
				dataTable.Rows.Add(row);
				}
			return dataTable;
			} catch (Exception ex) {
			throw new Exception($"Error retrieving Salesforce data: {ex.Message}", ex);
			}
		}
	public async Task<bool> DeleteToolingRecord(string oName, string recordId) {
		if (string.IsNullOrEmpty(recordId)) { return false; } // cant do it
		var (token, instanceUrl, _) = await GetAccessTokenAsync();
		string url = $"{instanceUrl}/services/data/v{_settings.ApiVersion}/tooling/sobjects/{Uri.EscapeDataString(oName)}/{Uri.EscapeDataString(recordId)}";
		var request = new HttpRequestMessage(HttpMethod.Delete, url);
		request.Headers.Add("Authorization", $"Bearer {token}");
		request.Headers.Add("Accept", "application/json");
		try {
			_logger.LogDebug("Sending DELETE request to: {Url}", url);
			using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken: default);
			if (response.IsSuccessStatusCode) {
				_logger.LogInformation("Successfully deleted record ID {RecordId} for object {ObjectName}", recordId, oName);
				return true;
				} else {
				string errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("DELETE request failed: Status={StatusCode}, Content={Content}", response.StatusCode, errorContent);
				throw new Exception($"Failed to delete record: {response.ReasonPhrase}. Content: {errorContent}");
				}
			} catch (Exception ex) {
			_logger.LogError("Error deleting record ID {RecordId} for object {ObjectName}: {Message}", recordId, oName, ex.Message);
			throw;
			}
		}
	public async Task<string> GetEventDefinitions() {

		var (token, instanceUrl, _) = await GetAccessTokenAsync();
		var request = new HttpRequestMessage(HttpMethod.Get, $"{instanceUrl}/services/data/v{_settings.ApiVersion}/event/eventSchema");
		request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		request.Headers.Add("Accept", "application/json");
		var response = await _httpClient.SendAsync(request, cancellationToken: default);
		response.EnsureSuccessStatusCode();
		string x = await response.Content.ReadAsStringAsync();
		return x;
		}
	//public class SalesforcePubSub {
	//	private static readonly HttpClient client = new HttpClient();

	//	public async Task<string> GetEventDefinitions(string accessToken, string instanceUrl, string apiVersion) {
	//		var request = new HttpRequestMessage(HttpMethod.Get, $"{instanceUrl}/services/data/v{apiVersion}/event/eventSchema");
	//		request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

	//		var response = await client.SendAsync(request);
	//		response.EnsureSuccessStatusCode();

	//		return await response.Content.ReadAsStringAsync();
	//	}
	//}
	public async Task<JsonElement> DescribeToolingObject(string objectName) {
		var (token, instanceUrl, _) = await GetAccessTokenAsync();
		string url = $"{instanceUrl}/services/data/v{_settings.ApiVersion}/tooling/sobjects/{Uri.EscapeDataString(objectName)}/describe/";
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Add("Authorization", $"Bearer {token}");
		request.Headers.Add("Accept", "application/json");
		try {
			using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken: default);
			if (!response.IsSuccessStatusCode) {
				string errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("SOQL query failed: Status={StatusCode}, Content={Content}", response.StatusCode, errorContent);
				throw new Exception($"Failed to execute SOQL query: {response.ReasonPhrase}");
				}
			await using var stream = await response.Content.ReadAsStreamAsync();
			var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: default);
			_logger.LogDebug("Successfully executed SOQL query");
			return jsonDoc.RootElement;
			} catch (Exception ex) {
			_logger.LogError("Error executing SOQL query: {Message}", ex.Message);
			throw;
			}
		}
	public async Task<DataTable> DescribeToolingObjectToDataTable(string objectName) {
		JsonElement re = await DescribeToolingObject(objectName);
		DataTable dt = JsonElementToDataTable(re, tableName: objectName); // new DataTable(getObjectNameFromSoql(soql));
		return dt;
		}
	public async Task<JsonElement> ExecuteSoqlQueryRawAsync(string soqlQuery, CancellationToken cancellationToken = default, bool useTooling = true, HttpMethod? method = null) {
		method ??= HttpMethod.Get;
		using (HttpRequestMessage request = await setupSoqlHeader(soqlQuery, HttpMethod.Get, useTooling)) {
			try {
				using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
				if (!response.IsSuccessStatusCode) {
					string errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogError("SOQL query failed: Status={StatusCode}, Content={Content}", response.StatusCode, errorContent);
					//throw new Exception($"Failed to execute SOQL query: {response.ReasonPhrase}");
					}
				await using var stream = await response.Content.ReadAsStreamAsync();
				var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
				_logger.LogDebug("Successfully executed SOQL query");
				return jsonDoc.RootElement; // Return JsonElement, which is not disposedss
				} catch (Exception ex) {
				_logger.LogError("Error executing SOQL query: {Message}", ex.Message);
				throw;
				}
			}
		}
	public async Task<DataTable> ExecSoqlToTable(string soql, bool useTooling) {
		JsonElement re = await ExecuteSoqlQueryRawAsync(soql, cancellationToken: default, useTooling: useTooling);
		DataTable dt = JsonElementToDataTable(re, tableName: getObjectNameFromSoql(soql)); // new DataTable(getObjectNameFromSoql(soql));
		return dt;
		}
	public async Task<DataTable> GetCDCEnabledEntitiesAsync(CancellationToken cancellationToken = default) {
		//			string query = "SELECT Id,QualifiedApiName,developerName  FROM EntityDefinition WHERE PublisherId = 'CDC' ORDER BY QualifiedApiName";
		string query = "SELECT QualifiedApiName,developerName  FROM EntityDefinition WHERE PublisherId = 'CDC' ORDER BY QualifiedApiName";

		JsonElement rootElement = await ExecuteSoqlQueryRawAsync(query, cancellationToken);
		string tableName = getObjectNameFromSoql(query);//FROM is case sensitive
		DataTable dt = new DataTable(tableName);
		try {
			if (!rootElement.TryGetProperty("records", out var records) || records.GetArrayLength() == 0) {
				_logger.LogWarning("No records returned from SOQL query");
				return dt;
				}
			var firstRecord = records.EnumerateArray().First();// Project column names and types from the first record
			var columns = firstRecord.EnumerateObject()
				.Where(prop => prop.Name != "attributes")
				.Select(prop => new {
					Name = prop.Name,
					Type = prop.Value.ValueKind switch {
						JsonValueKind.String => typeof(string),
						JsonValueKind.True or JsonValueKind.False => typeof(bool),
						JsonValueKind.Number => typeof(double),
						_ => typeof(string)
						}
					})
				.ToList();
			columns.ForEach(col => dt.Columns.Add(col.Name, col.Type));// Add columns, including baseObject
			dt.Columns.Add("name", typeof(string));//the name is used to compare registered objects
			var rows = records.EnumerateArray()             // Project records into rows
				.Select(record => {
					var row = dt.NewRow();
					string qualifiedApiName = null;
					foreach (var prop in record.EnumerateObject().Where(p => dt.Columns.Contains(p.Name))) {
						row[prop.Name] = prop.Value.ValueKind switch {
							JsonValueKind.Null => DBNull.Value,
							JsonValueKind.String => prop.Value.GetString(),
							JsonValueKind.True => true,
							JsonValueKind.False => false,
							JsonValueKind.Number => prop.Value.GetDouble(),
							_ => prop.Value.ToString()
							};
						if (prop.Name == "QualifiedApiName")
							qualifiedApiName = prop.Value.GetString();
						}
					row["name"] = PlatformEventChannelMemeberToObjectName(qualifiedApiName ?? "");
					return row;
				})
				.ToList();
			rows.ForEach(row => dt.Rows.Add(row));
			_logger.LogDebug("Successfully executed SOQL query, retrieved {Count} CDC-enabled EntityDefinitions with {ColumnCount} columns",
				dt.Rows.Count, dt.Columns.Count);
			return dt;
			} catch (Exception ex) {
			_logger.LogError("Error processing SOQL query results for EntityDefinition: {Message}", ex.Message);
			throw;
			}
		}
	public static string PlatformEventChannelMemeberToObjectName(string selectedEntity) {
		if (string.IsNullOrWhiteSpace(selectedEntity))
			throw new ArgumentNullException(nameof(selectedEntity));
		var regex = new Regex(@"^(.*?)(?:__)?ChangeEvent$", RegexOptions.Compiled);         // Regex pattern: Match optional trailing __ before ChangeEvent
		var match = regex.Match(selectedEntity);// Group 1 captures the base name (e.g., Account, Order, Product_Family)
		if (!match.Success) throw new ArgumentException("Invalid ChangeEvent format.", nameof(selectedEntity));
		string baseName = match.Groups[1].Value;
		bool isCustom = selectedEntity.Contains("__ChangeEvent");
		return isCustom ? $"{baseName}__c" : baseName;// Return baseName + __c for custom objects
		}
	public static string ObjectNameToChangeEvent(string objectName) {
		if (string.IsNullOrWhiteSpace(objectName))
			throw new ArgumentNullException(nameof(objectName));
		var match = Regex.Match(objectName, @"^(?<base>.+?)(__c)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);// Match custom object: ends with __c
		if (!match.Success) throw new ArgumentException("Invalid object name format.", nameof(objectName));
		string baseName = match.Groups["base"].Value;
		bool isCustom = objectName.EndsWith("__c", StringComparison.OrdinalIgnoreCase);
		return isCustom ? $"{baseName}__ChangeEvent" : $"{baseName}ChangeEvent";
		}
	public static string ChangeEventToObjectName(string changeEventName) {
		if (string.IsNullOrWhiteSpace(changeEventName))
			throw new ArgumentNullException(nameof(changeEventName));
		var match = Regex.Match(changeEventName, @"^(?<base>.+?)(?:__)?ChangeEvent$", RegexOptions.Compiled | RegexOptions.IgnoreCase); // Match both standard and custom ChangeEvent names
		if (!match.Success)
			throw new ArgumentException("Invalid ChangeEvent name format.", nameof(changeEventName));
		string baseName = match.Groups["base"].Value;
		bool isCustom = changeEventName.EndsWith("__ChangeEvent", StringComparison.OrdinalIgnoreCase);// If the original was a custom object, it ends with __ (custom objects have __ChangeEvent suffix)
		return isCustom ? $"{baseName}__c" : baseName;
		}
	public async Task DeleteSobject(string objectName, string recordId, bool useTooling = false) {
		if (string.IsNullOrEmpty(objectName))
			throw new ArgumentNullException(nameof(objectName));
		if (string.IsNullOrEmpty(recordId))
			throw new ArgumentNullException(nameof(recordId));
		var (token, instanceUrl, _) = await GetAccessTokenAsync();
		string divertTooling = useTooling ? "tooling/sobjects" : "sobjects";
		string url = $"{instanceUrl}/services/data/v{_settings.ApiVersion}/{divertTooling}/{objectName}";

		using var request = new HttpRequestMessage(HttpMethod.Delete, url);
		request.Headers.Add("Authorization", $"Bearer {token}");
		try {
			using var response = await _httpClient.SendAsync(request);
			if (response.IsSuccessStatusCode) // 204 No Content
			{
				_logger.LogInformation("Successfully deleted {ObjectName} record with Id {RecordId}", objectName, recordId);
				} else {
				string errorContent = await response.Content.ReadAsStringAsync();
				string errorMessage = ParseSalesforceError(errorContent);
				_logger.LogError("Failed to delete {ObjectName} record with Id {RecordId}: {StatusCode} - {ErrorMessage}",
					objectName, recordId, response.StatusCode, errorMessage);
				throw new Exception($"Failed to delete {objectName} record: {response.StatusCode} - {errorMessage}");
				}
			} catch (Exception ex) {
			_logger.LogError("Error deleting {ObjectName} record with Id {RecordId}: {Message}",
				objectName, recordId, ex.Message);
			throw;
			}
		}
	public async Task<string> IdOfPlatformEventChannelMember(string objectName, CancellationToken cancellationToken = default) {
		if (string.IsNullOrWhiteSpace(objectName)) throw new ArgumentNullException(nameof(objectName));
		string changeEventName = ObjectNameToChangeEvent(objectName);
		string query = $"SELECT Id FROM PlatformEventChannelMember WHERE SelectedEntity = '{changeEventName}' LIMIT 1";
		JsonElement result = await ExecuteSoqlQueryRawAsync(query, cancellationToken: cancellationToken, useTooling: true);
		if (result.TryGetProperty("records", out JsonElement records) && records.GetArrayLength() > 0) {
			return records[0].GetProperty("Id").GetString();
			}
		return null;
		}
	public async Task<DataTable> UpsertSobject(string objectName, string recordId, string jsonFields, bool useTooling = false) {
		var (token, instanceUrl, tenantId) = await GetAccessTokenAsync();
		string url;
		HttpMethod method;
		var options = new JsonSerializerOptions {
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true,
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
			};
		string divertTooling = useTooling ? "tooling/sobjects" : "sobjects";
		url = string.IsNullOrEmpty(recordId) ? $"{instanceUrl}/services/data/v{_settings.ApiVersion}/{divertTooling}/{objectName}" : $"{instanceUrl}/services/data/v{_settings.ApiVersion}/{divertTooling}/{objectName}/{recordId}";
		method = string.IsNullOrEmpty(recordId) ? HttpMethod.Post : HttpMethod.Patch;// If recordId NOT provided then insert(POST), otherwise Update (PATCH)  
		var fields = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonFields);
		jsonFields = JsonSerializer.Serialize(fields, new JsonSerializerOptions { WriteIndented = true });
		using var request = new HttpRequestMessage(method, url);
		request.Headers.Add("Authorization", $"Bearer {token}");
		request.Headers.Add("Accept", "application/json");
		request.Content = new StringContent(jsonFields, Encoding.UTF8, "application/json");
		try {
			using var response = await _httpClient.SendAsync(request);
			string newRecordId = recordId;
			if (response.StatusCode == System.Net.HttpStatusCode.NoContent && !string.IsNullOrEmpty(recordId)) {
				// Update successful (204 No Content)
				_logger.LogInformation("Updated {ObjectName} record with Id {RecordId}", objectName, recordId);
				} else if (response.StatusCode == System.Net.HttpStatusCode.Created && string.IsNullOrEmpty(recordId)) {// Insert successful (201 Created)
				string responseContent = await response.Content.ReadAsStringAsync();
				using JsonDocument responseDoc = JsonDocument.Parse(responseContent);
				newRecordId = responseDoc.RootElement.GetProperty("id").GetString();
				_logger.LogInformation("Created new {ObjectName} record with Id {RecordId}", objectName, newRecordId);
				} else {        // Handle error response
				string errorContent = await response.Content.ReadAsStringAsync();
				string errorMessage = ParseSalesforceError(errorContent);
				string mx = method.ToString();
				_logger.LogError("Failed to {method} {ObjectName} record: {StatusCode} - {ErrorMessage}",
					mx, objectName, response.StatusCode, errorMessage);
				throw new Exception($"Failed to {mx} {objectName} record: {response.StatusCode} - {errorMessage}");
				}
			DataTable resultTable = new DataTable(objectName);// Create a DataTable with the upserted record
			resultTable.Columns.Add("Id", typeof(string));
			foreach (var field in fields) {
				resultTable.Columns.Add(field.Key, typeof(string)); // Adjust type as needed
				}
			DataRow row = resultTable.NewRow();
			row["Id"] = newRecordId;
			foreach (var field in fields) {
				row[field.Key] = field.Value != null ? field.Value.ToString() : DBNull.Value;
				}
			resultTable.Rows.Add(row);
			return resultTable;
			} catch (Exception ex) {
			_logger.LogError("Error upserting {ObjectName} record: {Message}", objectName, ex.Message);
			throw;
			}
		}
	private static bool isCustomObject(string sObject) {
		return Regex.IsMatch(sObject, @"__c$", RegexOptions.IgnoreCase);
		}
	public static string? GetPlatformEventChannelMemberFullName(string sObject) {
		if (string.IsNullOrWhiteSpace(sObject)) return null;
		return isCustomObject(sObject) ? $"ChangeEvents_{sObject.Replace("__c", "")}_ChangeEvent" : $"ChangeEvents_{sObject}ChangeEvent"; // e.g., Order__ChangeEvent or OrderChangeEvent
		}
	public async Task AddCDCChannelMember(string sObject) {
		string masterLabel = ObjectNameToChangeEvent(sObject); // e.g., Order__ChangeEvent
		string fullName = GetPlatformEventChannelMemberFullName(sObject)!;
		var metadata = new {
			enrichedFields = new string[] { },
			eventChannel = "ChangeEvents",
			selectedEntity = masterLabel,
			urls = (string)null
			};
		var payload = new Dictionary<string, object?> {
			["FullName"] = fullName, // Use underscore instead of dot
			["Metadata"] = metadata
			};
		try {
			string jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions {
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				WriteIndented = true
				});
			await UpsertSobject("PlatformEventChannelMember", null, jsonPayload, useTooling: true);
			_logger.LogInformation("Successfully enabled CDC for {SObject} with channel {MasterLabel}", sObject, masterLabel);
			} catch (Exception ex) {
			_logger.LogError("Failed to enable CDC for {SObject}: {Message}", sObject, ex.Message);
			throw;
			}
		}
	#region helpers
	private string getObjectNameFromSoql(string soqlQuery) {
		if (string.IsNullOrWhiteSpace(soqlQuery)) {
			_logger.LogWarning("SOQL query is empty or null");
			return null;
			}

		// Regex: Match "FROM" (case-insensitive) followed by whitespace and capture the table name
		string pattern = @"FROM\s+([a-zA-Z0-9_]+)\b";
		Match match = Regex.Match(soqlQuery, pattern, RegexOptions.IgnoreCase);

		if (match.Success) {
			string objectName = match.Groups[1].Value;
			_logger.LogDebug("Extracted object name: {ObjectName} from SOQL query", objectName);
			return objectName;
			}

		_logger.LogWarning("No object name found in SOQL query: {Query}", soqlQuery);
		return null;
		}
	public class AuthenticationEventArgs : EventArgs {
		public LogLevel LogLevel { get; }
		public string Message { get; }
		public string InstanceUrl { get; }

		public AuthenticationEventArgs(LogLevel ll, string message, string instanceUrl = null) {
			LogLevel = ll;
			Message = message;
			InstanceUrl = instanceUrl;
			}
		}
	public class SchemaFetchedEventArgs : EventArgs {
		public string ObjectName { get; }
		public DataTable Schema { get; }
		public bool Success { get; }
		public string ErrorMessage { get; }

		public SchemaFetchedEventArgs(string objectName, DataTable schema, bool success, string errorMessage = null) {
			ObjectName = objectName;
			Schema = schema;
			Success = success;
			ErrorMessage = errorMessage;
			}
		}


	#endregion	helpers
	//public class TokenResponse {
	//	public string access_token { get; set; }
	//	public string instance_url { get; set; }
	//	public string id { get; set; }
	//}
	public class AccessTokenCache {
		public string Token { get; set; }
		public string InstanceUrl { get; set; }
		public string TenantId { get; set; }
		public DateTime Expiry { get; set; }
		}
	private DataTable JsonElementToDataTable(JsonElement rootElement, string tableName) {
		DataTable dt = new DataTable(tableName);
		try {
			if (!rootElement.TryGetProperty("records", out var records) || records.GetArrayLength() == 0) {
				_logger.LogWarning("No records returned from SOQL query");
				return dt;
				}
			var firstRecord = records.EnumerateArray().First();// Project column names and types from the first record
			var columns = firstRecord.EnumerateObject()
				.Where(prop => prop.Name != "attributes")
				.Select(prop => new {
					Name = prop.Name,
					Type = prop.Value.ValueKind switch {
						JsonValueKind.String => typeof(string),
						JsonValueKind.True or JsonValueKind.False => typeof(bool),
						JsonValueKind.Number => typeof(double),
						_ => typeof(string)
						}
					})
				.ToList();
			columns.ForEach(col => dt.Columns.Add(col.Name, col.Type));// Add columns, including baseObject

			var rows = records.EnumerateArray()             // Project records into rows
				.Select(record => {
					var row = dt.NewRow();
					string qualifiedApiName = null;
					foreach (var prop in record.EnumerateObject().Where(p => dt.Columns.Contains(p.Name))) {
						row[prop.Name] = prop.Value.ValueKind switch {
							JsonValueKind.Null => DBNull.Value,
							JsonValueKind.String => prop.Value.GetString(),
							JsonValueKind.True => true,
							JsonValueKind.False => false,
							JsonValueKind.Number => prop.Value.GetDouble(),
							_ => prop.Value.ToString()
							};
						if (prop.Name == "QualifiedApiName")
							qualifiedApiName = prop.Value.GetString();
						}
					//row["name"] = qualifiedApiName?.Replace("ChangeEvent", "") ?? "";
					return row;
				}).ToList();
			rows.ForEach(row => dt.Rows.Add(row));
			_logger.LogDebug("Successfully executed SOQL query, retrieved {Count} CDC-enabled EntityDefinitions with {ColumnCount} columns",
				dt.Rows.Count, dt.Columns.Count);
			return dt;
			} catch (Exception ex) {
			_logger.LogError("Error processing SOQL query results for EntityDefinition: {Message}", ex.Message);
			throw;
			}
		}
	private string ParseSalesforceError(string errorContent) {
		try {
			using JsonDocument doc = JsonDocument.Parse(errorContent);
			var errors = doc.RootElement.EnumerateArray();
			var errorMessages = new List<string>();
			foreach (var error in errors) {
				string message = error.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
				string errorCode = error.TryGetProperty("errorCode", out var code) ? code.GetString() : "N/A";
				string fields = error.TryGetProperty("fields", out var fieldsProp) ? string.Join(", ", fieldsProp.EnumerateArray().Select(f => f.GetString())) : "";
				errorMessages.Add($"Error: {message} (Code: {errorCode}, Fields: {fields})");
				}
			return string.Join("; ", errorMessages);
			} catch {
			return errorContent; // Fallback to raw content if parsing fails
			}
		}
	private async Task<HttpRequestMessage> setupSoqlHeader(string query, HttpMethod method, bool useTooling = false) {
		var (token, instanceUrl, _) = await GetAccessTokenAsync();
		string url = useTooling
			? $"{instanceUrl}/services/data/v{_settings.ApiVersion}/tooling/query/?q={Uri.EscapeDataString(query)}"
			: $"{instanceUrl}/services/data/v{_settings.ApiVersion}/query/?q={Uri.EscapeDataString(query)}";
		var request = new HttpRequestMessage(method, url);
		request.Headers.Add("Authorization", $"Bearer {token}");
		request.Headers.Add("Accept", "application/json");
		return request;
		}
	}