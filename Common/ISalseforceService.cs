using System.Data;
using System.Text.Json;
namespace Common;
public interface ISalesforceService {
	Task<(string token, string instanceUrl, string tenantId)> GetAccessTokenAsync();
	Task<JsonElement> GetObjectSchemaAsync(string objectName, CancellationToken cancellationToken = default, bool useTooling = false);

	Task<DataSet> GetEventSchema(string eventName);
	Task<string> GetObjectSchemaSummaryAsync(string objectName);
	Task<DataSet> GetObjectSchemaAsDataSetAsync(string objectName, bool useTooling = false);
	Task<DataTable> GetAllObjects();
	Task<DataTable> GetSalesforceRecord(string objectName, string recordId);
	Task<JsonElement> GetPlaformEventChannelMembers(CancellationToken cancellationToken = default);
	Task<DataTable> GetCDCEnabledEntitiesAsync(CancellationToken cancellationToken = default);
	//Task<JsonElement> ExecuteSoqlQueryRawAsync(string soqlQuery, CancellationToken cancellationToken = default,bool useTooling =false);
	Task<DataTable> ExecSoqlToTable(string soql, bool useTooling);
	Task<JsonElement> ExecuteSoqlQueryRawAsync(string soqlQuery, CancellationToken cancellationToken = default, bool useTooling = true, HttpMethod? method = null);
	Task<DataTable> UpsertSobject(string objectName, string recordId, string jsonFields, bool useTooling = false);

	Task<JsonElement> DescribeToolingObject(string objectName);
	Task<DataTable> DescribeToolingObjectToDataTable(string objectName);
	Task<bool> DeleteToolingRecord(string oName, string recordId);
	Task AddCDCChannelMember(string sObject);
	Task DeleteSobject(string objectName, string recordId, bool useTooling = false);
	Task<string> IdOfPlatformEventChannelMember(string objectName, CancellationToken cancellationToken = default);

	public class PlatformEventInfo {
		public string Name { get; set; }
		public string Label { get; set; }
		}
	Task<DataTable> GetPlatformEventList();

	}