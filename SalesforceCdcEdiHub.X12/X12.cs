
using Microsoft.Extensions.Logging;
using OopFactory.X12.Parsing.Model;
namespace SalesforceCdcEdiHub;

public class X12 {
	#region fields & properties
	private readonly ILogger<X12> _l;
	public string Version { get; set; }
	#endregion
	public X12(ILogger<X12> logger) {
		_l = logger;
		_l.Log(LogLevel.Debug, "Starting X12");
		}

	public Interchange CreateISA(int isaControlNumber, bool isProduction, string SenderId, string receiverId) {
		var interchange = new Interchange(DateTime.Now, isaControlNumber, isProduction);
		interchange.InterchangeSenderId = SenderId;
		interchange.InterchangeReceiverId = receiverId;


		return interchange;
		}

	}