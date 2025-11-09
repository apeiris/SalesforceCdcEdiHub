using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
//using MondayCom.PurchaseOrders.Exceptions;
using SalesforceCdcEdiHub.MondayCom;



public class MondayComClient : IDisposable {
	private readonly HttpClient _httpClient;
	private readonly string _apiToken;
	private bool _disposed;

	public MondayComClient(string apiToken, TimeSpan? timeout = null, string apiVersion = "2023-10") {
		_apiToken = apiToken ?? throw new ArgumentNullException(nameof(apiToken));

		_httpClient = new HttpClient(new HttpClientHandler {
			// Optional: fix SSL/TLS issues on older Windows
			// SslProtocols = System.Security.Authentication.SslProtocols.Tls12
		});

		// SET TIMEOUT
		_httpClient.Timeout = timeout ?? TimeSpan.FromSeconds(30);

		// FULL URL – avoid ""
		_httpClient.BaseAddress = new Uri("https://api.monday.com/v2/");

		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(apiToken);
		_httpClient.DefaultRequestHeaders.Add("API-Version", apiVersion);
		_httpClient.DefaultRequestHeaders.Accept.Add(
			new MediaTypeWithQualityHeaderValue("application/json"));
	}


	public async Task<T> ExecuteQueryAsync<T>(
	string query,
	JsonObject? variables = null,
	string? operationName = null,
	CancellationToken ct = default) {
		// ---- 1. Build the GraphQL envelope ---------------------------------
		var payload = new JsonObject {
			["query"] = query
		};
		if (variables != null) payload["variables"] = variables;
		if (operationName != null) payload["operationName"] = operationName;

		var content = new StringContent(
			payload.ToJsonString(),
			Encoding.UTF8,
			"application/json");

		// ---- 2. POST --------------------------------------------------------
		HttpResponseMessage resp;
		try { resp = await _httpClient.PostAsync("", content, ct); } catch (Exception ex) { throw new MondayApiException(0, null, $"HTTP error: {ex.Message}"); }

		// ---- 3. Read body ---------------------------------------------------
		string body;
		try { body = await resp.Content.ReadAsStringAsync(ct); } catch (Exception ex) { throw new MondayApiException((int)resp.StatusCode, null, $"Read error: {ex.Message}"); }

		// ---- 4. HTTP-level error --------------------------------------------
		if (!resp.IsSuccessStatusCode)
			throw new MondayApiException((int)resp.StatusCode, body,
				$"HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}");

		// ---- 5. Parse JSON --------------------------------------------------
		JsonDocument doc;
		try { doc = JsonDocument.Parse(body); } catch (JsonException ex) { throw new MondayApiException(400, body, $"Invalid JSON: {ex.Message}"); }

		var root = doc.RootElement;

		// ---- 6. GraphQL errors ----------------------------------------------
		if (root.TryGetProperty("errors", out var err))
			throw new MondayApiException(400, body,
				"GraphQL errors: " + err.GetRawText());

		// ---- 7. No data -----------------------------------------------------
		if (!root.TryGetProperty("data", out var data))
			throw new MondayApiException(400, body, "Missing 'data' field.");

		// ---- 8. Deserialize -------------------------------------------------
		try {
			var result = data.Deserialize<T>(new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			});

			return result ?? throw new InvalidOperationException("Deserialized result is null.");
		} catch (Exception ex) {
			throw new MondayApiException(400, body,
				$"Deserialize {typeof(T).Name} failed: {ex.Message}");
		}
	}


	public async Task<T> ExecuteMutationAsync<T>(
	string query,
	JsonObject? variables = null,
	CancellationToken cancellationToken = default) {
		var payload = new JsonObject {
			["query"] = query
		};
		if (variables != null)
			payload["variables"] = variables;

		var content = new StringContent(
			payload.ToJsonString(),
			Encoding.UTF8,
			"application/json");

		HttpResponseMessage response;
		try {
			// Use cancellation + full path
			response = await _httpClient.PostAsync("", content, cancellationToken)
										.ConfigureAwait(false);
		} catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested) {
			throw new MondayApiException(0, null, $"Request timed out after {_httpClient.Timeout}");
		} catch (Exception ex) {
			throw new MondayApiException(0, null, $"HTTP request failed: {ex.Message}");
		}

		string jsonString;
		try {
			jsonString = await response.Content.ReadAsStringAsync(cancellationToken)
									   .ConfigureAwait(false);
		} catch (Exception ex) {
			throw new MondayApiException((int)response.StatusCode, null,
				$"Failed to read response: {ex.Message}");
		}

		if (!response.IsSuccessStatusCode) {
			throw new MondayApiException((int)response.StatusCode, jsonString,
				$"HTTP {response.StatusCode}: {response.ReasonPhrase}");
		}

		JsonDocument doc;
		try {
			doc = JsonDocument.Parse(jsonString);
		} catch (JsonException ex) {
			throw new MondayApiException(400, jsonString, $"Invalid JSON: {ex.Message}");
		}

		var root = doc.RootElement;

		if (root.TryGetProperty("errors", out var errors)) {
			throw new MondayApiException(400, jsonString,
				"GraphQL errors: " + errors.GetRawText());
		}

		if (!root.TryGetProperty("data", out var dataProp)) {
			throw new MondayApiException(400, jsonString, "Missing 'data' in response.");
		}

		try {
			var result = dataProp.Deserialize<T>(new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			});

			if (result == null)
				throw new InvalidOperationException("Deserialized result is null.");

			return result;
		} catch (Exception ex) {
			throw new MondayApiException(400, jsonString,
				$"Deserialization failed for {typeof(T).Name}: {ex.Message}");
		}
	}


	public void Dispose() {
		if (!_disposed) {
			_httpClient?.Dispose();
			_disposed = true;
		}
	}
}