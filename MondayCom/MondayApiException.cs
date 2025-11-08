using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace SalesforceCdcEdiHub.MondayCom;
public class MondayApiException : Exception {
	public int StatusCode { get; }
	public string? ResponseBody { get; }

	public MondayApiException(int statusCode, string? responseBody, string message)
		: base(message) {
		StatusCode = statusCode;
		ResponseBody = responseBody;
	}
}