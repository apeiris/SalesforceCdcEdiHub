// Models/PurchaseOrderDto.cs

public class PurchaseOrderDto {
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty; // PO Number
	public string Status { get; set; } = string.Empty;
	public string VendorName { get; set; } = string.Empty;
	public decimal Amount { get; set; }
	public DateTime? DeliveryDate { get; set; }
}