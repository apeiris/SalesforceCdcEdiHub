using System;
using Microsoft.Extensions.Logging;
namespace SalesforceCdcEdiHub;
public static class LoggerEmailExtensions {
	private static readonly LogLevel EmailLevel = LogLevel.Error; // reuse Error internally

	public static void LogEmail(this ILogger logger, string message, params object[] args) {
		logger.Log(EmailLevel, "[EMAIL] " + message, args);
		}

	public static void LogEmail(this ILogger logger, Exception exception, string message, params object[] args) {
		logger.Log(EmailLevel, exception, "[EMAIL] " + message, args);
		}
	}