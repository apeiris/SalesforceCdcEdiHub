using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using SalesforceCdcEdiHub.MondayCom;
using MondayCom.PurchaseOrders.Models;
namespace SalesforceCdcEdiHub.PurchaseOrders;
public class PurchaseOrderService {
	private readonly MondayComClient _client;

	public PurchaseOrderService(MondayComClient client) {
		_client = client;
	}

	public async Task<CreateItemResponse> CreatePurchaseOrderAsync(
	long boardId,
	string poNumber,
	long vendorUserId,
	decimal amount,
	DateTime deliveryDate,
	string statusLabel = "Ordered") {
		// Build column values as JsonObject first
		var columnValuesObj = new JsonObject {
			["status"] = new JsonObject { ["label"] = statusLabel },
			["people"] = new JsonObject {
				["personsAndTeams"] = new JsonArray(
					new JsonObject { ["id"] = vendorUserId, ["kind"] = "person" }
				)
			},
			["numbers"] = amount,
			["date4"] = new JsonObject { ["date"] = deliveryDate.ToString("yyyy-MM-dd") }
		};

		// Convert to **escaped JSON string**
		string columnValuesString = columnValuesObj.ToJsonString();

		const string query = @"
        mutation CreatePO($boardId: ID!, $itemName: String!, $columnValues: JSON!) {
            create_item(
                board_id: $boardId
                item_name: $itemName
                column_values: $columnValues
            ) {
                id
                name
            }
        }";

		var variables = new JsonObject {
			["boardId"] = boardId.ToString(),
			["itemName"] = poNumber,
			["columnValues"] = columnValuesString  // ← STRING, not JsonNode
		};

		return await _client.ExecuteMutationAsync<CreateItemResponse>(query, variables);
	}

	public async Task<List<PurchaseOrderDto>> GetAllPurchaseOrdersAsync(long boardId, CancellationToken cancellationToken) {
		const string query = @"
            query GetPOs($boardId: ID!) {
                boards(ids: [$boardId]) {
                    items_page {
                        items {
                            id
                            name
                            column_values(ids: [""status"", ""people"", ""numbers"", ""date4""]) {
                                id
                                value
                                text
                            }
                        }
                    }
                }
            }";

		var variables = new JsonObject {
			["boardId"] = boardId.ToString()
		};

		var result = await _client.ExecuteMutationAsync<JsonObject>(query, variables);
		var board = result["boards"]?[0];

		if (board == null)
			throw new InvalidOperationException("Board not found or no access.");

		var items = board["items_page"]?["items"]?.AsArray() ?? new JsonArray();

		var pos = new List<PurchaseOrderDto>();

		foreach (var item in items.Cast<JsonObject>()) {
			var po = new PurchaseOrderDto {
				Id = item["id"]?.ToString() ?? "",
				Name = item["name"]?.ToString() ?? ""
			};

			var columns = item["column_values"]?.AsArray() ?? new JsonArray();

			foreach (var col in columns.Cast<JsonObject>()) {
				var colId = col["id"]?.ToString();
				var value = col["value"]?.ToString();
				var text = col["text"]?.ToString();

				switch (colId) {
					case "status":
						po.Status = ParseStatus(value) ?? text ?? "";
						break;
					case "people":
						po.VendorName = ParsePeople(value) ?? "";
						break;
					case "numbers":
						if (decimal.TryParse(value, out var amount))
							po.Amount = amount;
						break;
					case "date4":
						if (DateTime.TryParse(value, out var date))
							po.DeliveryDate = date;
						break;
				}
			}

			pos.Add(po);
		}

		return pos;
	}

	private static string? ParseStatus(string? json) {
		if (string.IsNullOrEmpty(json)) return null;
		try {
			var obj = JsonNode.Parse(json)?.AsObject();
			return obj?["label"]?.ToString();
		} catch { return null; }
	}

	private static string? ParsePeople(string? json) {
		if (string.IsNullOrEmpty(json)) return null;
		try {
			var obj = JsonNode.Parse(json)?.AsObject();
			var people = obj?["personsAndTeams"]?.AsArray();
			return people?.FirstOrDefault()?["name"]?.ToString()
				   ?? people?.FirstOrDefault()?["id"]?.ToString();
		} catch { return null; }
	}


}