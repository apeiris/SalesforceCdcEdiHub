using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
//using Newtonsoft.Json; 
public static class Axios {
	private static readonly HttpClient client = new HttpClient();
	public static async Task<T> GetAsync<T>(string url, string username = null, string password = null) {
		AddAuthHeader(username, password);
		var response = await client.GetAsync(url);
		response.EnsureSuccessStatusCode();
		string json = await response.Content.ReadAsStringAsync();
		return System.Text.Json.JsonSerializer.Deserialize<T>(json);
	}


	public static async Task<XmlDocument> GetXmlDocumentAsync(string url, string username = null, string password = null) {
		AddAuthHeader(username, password);
		var response = await client.GetAsync(url);
		response.EnsureSuccessStatusCode();

		string json = await response.Content.ReadAsStringAsync();

		// Convert JSON to XmlDocument
		XmlDocument xmlDoc = JsonConvert.DeserializeXmlNode(json, "Root");
		return xmlDoc;
	}
	public static async Task<XDocument> GetXDocumentAsync(string url, string username = null, string password = null) {
		XmlDocument xmlDoc = await GetXmlDocumentAsync(url, username, password);

		using (var nodeReader = new XmlNodeReader(xmlDoc)) {
			XDocument xDoc = XDocument.Load(nodeReader);
			return xDoc;
		}
	}

	public static async Task<T> PostAsync<T>(string url, object data, string username = null, string password = null) {
		AddAuthHeader(username, password);
		var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
		var response = await client.PostAsync(url, content);
		response.EnsureSuccessStatusCode();
		string json = await response.Content.ReadAsStringAsync();
		return System.Text.Json.JsonSerializer.Deserialize<T>(json);
	}
	private static void AddAuthHeader(string username, string password) {
		client.DefaultRequestHeaders.Clear();
		if (!string.IsNullOrEmpty(username)) {
			var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
		}
	}
}

