using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System;
namespace PDF;
public struct ExtractionMap {// 1. Map for single, non-repeating fields (Header, Parties, Notes)
	public string XmlPath { get; set; }// Full XML Path: e.g., "Header/PONumber"
	public Rectangle BoundingBox { get; set; } // The area on the PDF
	public int PageNumber { get; set; }
}
public struct TableExtractionMap {
	public string ParentXmlTag { get; set; } // e.g., "LineItems" or "Summary"
	public string ItemXmlTag { get; set; } 
	public List<string> ColumnTags { get; set; }
	public Rectangle BoundingBox { get; set; }
	public int PageNumber { get; set; }
}// 2. Map for tables (LineItems) and special structures (Summary)
public class RawTableData {
	public List<List<string>> Rows { get; set; } = new List<List<string>>();
}// Placeholder for raw data retrieved from a PDF table extraction utility
public class xPdfDynamicXmlExtractor {
	// =========================================================================
	// Orchestration: Main entry point to combine all extractions
	// =========================================================================
	public static XDocument ExtractDataToXmlDynamic(string pdfPath, List<ExtractionMap> fieldMaps, List<TableExtractionMap> tableMaps) {
		var root = new XElement("PurchaseOrder");
		// Note: For a real solution, the PdfDocument initialization should wrap the entire function.
		// We simulate the extraction here using placeholder data based on the source snippets.
		ProcessSingleFields(pdfPath, fieldMaps, root);// 1. Process Single Fields (Creates Header, Parties, etc.)
		ProcessTables(pdfPath, tableMaps, root);// 2. Process Tables (Creates LineItems and Summary)
		return new XDocument(root);
	}
	private static void ProcessSingleFields(string pdfPath, List<ExtractionMap> maps, XElement root) {
		// Dictionary to track created XML elements by their full path
		var elementCache = new Dictionary<string, XElement> { { "PurchaseOrder", root } };

		// Use a try-catch or ensure file handling outside this function is robust.
		// Assuming PdfDocument initialization is successful:
		using (PdfDocument pdfDocument = new(new PdfReader(pdfPath))) {
			foreach (var map in maps) {
				// 1. CRITICAL NULL CHECK (Likely Fix for NRE on XmlPath)
				if (string.IsNullOrWhiteSpace(map.XmlPath)) {
					// Skip map if XmlPath is not defined or is just whitespace.
					continue;
				}

				// 2. TEXT EXTRACTION (Line 52 is likely here)
				// Ensure extracted text is NOT null before proceeding.
				string extractedText = SimulateTextExtraction(map) ?? string.Empty;

				// 3. SAFE PATH SEGMENTATION
				// Use StringSplitOptions.RemoveEmptyEntries to prevent null segments
				// if the path has extra slashes (e.g., "Parties//Buyer").
				string[] pathSegments = map.XmlPath.Split(
					new char[] { '/' },
					StringSplitOptions.RemoveEmptyEntries
				);

				if (pathSegments.Length == 0) continue;

				XElement currentParent = root;
				string currentPath = "PurchaseOrder";
				for (int i = 0; i < pathSegments.Length; i++) {
					string segment = pathSegments[i];
					string nextPath = $"{currentPath}/{segment}";

					if (i == pathSegments.Length - 1) {
						// Last segment: Create the value element
						currentParent.Add(new XElement(segment, extractedText));
					} else {
						// Parent segment: Create or retrieve

						// 1. Temporarily store the parent from the previous iteration
						XElement oldParent = currentParent;

						// 2. Check cache. If it fails, currentParent is set to null by 'out'.
						if (!elementCache.TryGetValue(nextPath, out currentParent)) {
							// Element doesn't exist. We must create it and add it to oldParent.
							XElement newParent = new XElement(segment);

							// Add the new element to the stored old parent (which is NOT null).
							oldParent.Add(newParent);

							// Now, update currentParent to the new element for the next iteration.
							currentParent = newParent;
							elementCache.Add(nextPath, newParent);
						}
						// If TryGetValue succeeded, currentParent was correctly retrieved and is not null.
					}
					currentPath = nextPath;
				}
			}
		}
	}   // Dynamic Logic: Handles single field extraction and hierarchy creation
	private static void ProcessTables(string pdfPath, List<TableExtractionMap> maps, XElement root) {
		foreach (var map in maps) {
			RawTableData rawData = GetRawTableData(map);// Raw table extraction is simulated here
			if (!rawData.Rows.Any()) continue;
			var parentElement = new XElement(map.ParentXmlTag);
			if (map.ParentXmlTag == "LineItems") {
				foreach (var row in rawData.Rows) {// LineItems: Repeating <Item> elements
					var itemElement = new XElement(map.ItemXmlTag);
					for (int i = 0; i < row.Count; i++) {
						string tag = (i < map.ColumnTags.Count) ? map.ColumnTags[i] : $"Column{i + 1}";
						itemElement.Add(new XElement(tag, row[i]));
					}
					parentElement.Add(itemElement);
				}
			} else if (map.ParentXmlTag == "Summary") {// Summary: Custom logic for attributes and structure
				foreach (var row in rawData.Rows) {
					string tag = row[0].Replace(":", "").Trim(); // Use the field name (Subtotal, Tax, etc.) as the XML tag
					
					tag = tag.Replace(" ", "").Replace("$","").Replace("%","").Replace("(","").Replace(")","");
					string rawValue = row[1];
					string value = rawValue.Replace("$", "").Replace("USD", "").Trim();
					XElement summaryElement = new XElement(tag, value);
					if (tag == "Subtotal" || tag == "Total") {// Add currency attribute to Subtotal and Total
						summaryElement.Add(new XAttribute("currency", "USD"));
					}
					parentElement.Add(summaryElement);
				}
			}
			root.Add(parentElement);
		}
	}// Dynamic Logic: Handles table extraction and repeating element creation
	private static string SimulateTextExtraction(ExtractionMap map) {

		return map.XmlPath switch {
			"Header/PONumber" => "asfsadfsdfsda10345",
			"Header/PODate" => "September 4, 2025",
			"Header/DeliveryDate" => "September 20, 2025",
			"Header/Status" => "Pending",
			"Parties/Buyer/Name" => "NextGen Tech Corp",
			"Parties/Buyer/Address" => "77 Bay Street, Toronto, ON M5J 2L9, Canada",
			"Parties/Buyer/Contact" => "+1 (416) 555-7789 | finance@nextgen.com",
			"Parties/Supplier/Name" => "Metro Office Furniture Co.",
			"Parties/Supplier/Address" => "1750 Market St, Denver, CO 80202, USA",
			"Parties/Supplier/Contact" => "+1 (303) 555-9922 | sales@metrooffice.com",
			"Notes" => "Please deliver to Toronto HQ dock entrance. All items must be packaged securely.",
			_ => string.Empty
		};
	}		// This function would normally use iText 7 and map.BoundingBox to read the text.
																			// We use hardcoded return values based on the known PDF content for illustration.
	private static RawTableData GetRawTableData(TableExtractionMap map) {
		if (map.ParentXmlTag == "LineItems") {
			return new RawTableData {
				Rows = new List<List<string>>
				{
					new List<string> { "Workstations (Cubicle)", "MOF-WS600", "8", "850.00", "6,800.00" },
					new List<string> { "Storage Cabinets", "MOF-SC100", "6", "295.00", "1,770.00" },
                    // Remaining items for a complete list
                    new List<string> { "Task Chairs", "MOF-TC210", "15", "175.00", "2,625.00" },
					new List<string> { "Breakroom Tables", "MOF-BT50", "4", "310.00", "1,240.00" }
				}
			};
		} else if (map.ParentXmlTag == "Summary") {
			return new RawTableData {
				Rows = new List<List<string>>
				{
					new List<string> { "Subtotal:", "12,435.00" },
					new List<string> { "Discount (3%):", "-$373.05" },
					new List<string> { "Tax (7%):", "856.34" },
					new List<string> { "Total:", "12,918.29 USD" }
				}
			};
		}
		return new RawTableData();
	}// This function simulates the raw output of a table extraction utility.
}