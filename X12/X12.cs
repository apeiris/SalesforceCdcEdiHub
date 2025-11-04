
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using OopFactory.X12.Parsing.Model;
namespace SalesforceCdcEdiHub;

public class X12 {
	#region fields & properties
	private readonly ILogger<X12> _l;
	public string Version { get; set; }
	public static readonly Dictionary<string, string> _tSetNames = new() { { "850", "PO" }, { "856", "ASN" }, { "810", "INV" }, { "997", "FA" } };

	#endregion
	public X12(ILogger<X12> logger) {
		_l = logger;
		_l.Log(LogLevel.Debug, "Starting X12");
	}
	public void PrepareDoc(string payLoadXml) {
		try {
			XDocument xdoc = XDocument.Parse(payLoadXml);
			_l.LogDebug($"XML Document prepared document type=[xdoc.Root.Name.LocalName={xdoc.Root.Name.LocalName}] ");
			if (xdoc.Root.Elements().Count() > 1) {
				throw new Exception("Only one root document allowed ");
			}
	
			
		switch (xdoc.Root.Name.LocalName.ToLower()) {
				case "orders":
					_l.LogDebug("[Preparing Order - 850]");
					var order = xdoc.Descendants("Order").First();
					var account = order.Element("Account");
					var items = order.Elements("OrderItem").ToList();
					string controlNumber = order.Element("EDI_Seed")!.Value;
					Interchange interchange = new Interchange(DateTime.Now, int.Parse(controlNumber), false);
					break;
			}


		} catch (Exception ex) {
			_l.LogError(ex.Message);
		}
	}
	public Interchange CreateISA(string PayLoadXml) {
		//return CreateISA(transaction, isaControlNumber, isProduction, "SENDERID", "RECEIVERID");

		return null;
	}
	public Interchange CreateISA(string transaction, int isaControlNumber, bool isProduction, string SenderId, string receiverId) {
		string tSetName = _tSetNames.ContainsKey(transaction) ? _tSetNames[transaction] : throw new ArgumentException($"Transaction Set {transaction} not supported");



		var interchange = new Interchange(DateTime.Now, isaControlNumber, isProduction) {
			InterchangeSenderId = SenderId,
			InterchangeReceiverId = receiverId,

		};



		return interchange;
	}

}