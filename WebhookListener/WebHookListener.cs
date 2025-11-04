using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SalesforceCdcEdiHub;

public class WebHookEventArg : EventArgs {
	public string Message { get; set; }
	public WebHookEventArg(string message) => Message = message;
}

public class KestrelWebhookListener {
	private readonly ILogger<KestrelWebhookListener> _logger;
	private readonly string _webhookPath;

	public event EventHandler<WebHookEventArg> WebHookEvent;

	public KestrelWebhookListener(ILogger<KestrelWebhookListener> logger) {
		_logger = logger;
		_webhookPath = "/webhook"; // fixed path
		_logger.LogInformation("🛠️ Webhook listener initialized at path {WebhookPath}", _webhookPath);
	}

	// This method is called from Program.cs after building the app
	public void MapWebhook(WebApplication app) {
		// Enable CORS
		app.UseCors(policy => policy
			.AllowAnyOrigin()
			.AllowAnyHeader()
			.AllowAnyMethod());

		// Map POST endpoint
		app.MapPost(_webhookPath, async context => {
			try {
				using var reader = new StreamReader(context.Request.Body);
				var body = await reader.ReadToEndAsync();
				_logger.LogInformation("✅ Received webhook: {Body}", body);

				// Raise event for subscribers
				WebHookEvent?.Invoke(this, new WebHookEventArg(body));

				await context.Response.WriteAsync("Webhook received");
			} catch (Exception ex) {
				_logger.LogError(ex, "❌ Error processing webhook request");
				context.Response.StatusCode = 500;
				await context.Response.WriteAsync("Error processing webhook");
			}
		});

		_logger.LogInformation("🚀 Webhook endpoint mapped at {WebhookPath}", _webhookPath);
	}
}
