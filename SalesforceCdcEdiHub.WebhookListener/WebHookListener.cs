using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SalesforceCdcEdiHub;
public class KestrelWebhookListener : BackgroundService {
	private readonly ILogger<KestrelWebhookListener> _logger;
	private readonly IConfiguration _config;
	private readonly string _webhookUrl;

	public KestrelWebhookListener(ILogger<KestrelWebhookListener> logger, IConfiguration config) {
		_logger = logger;
		_config = config;
		// Try environment variable first, then appsettings.json
		_webhookUrl =Environment.GetEnvironmentVariable("VUE_APP_WEBHOOK_URL")?? _config.GetValue<string>("Webhook:Url")?? "http://0.0.0.0:5005";
		_logger.LogInformation("🛠️ Webhook listener configured for {WebhookUrl}", _webhookUrl);
	}
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		try {
			var builder = WebApplication.CreateBuilder();
			builder.WebHost.UseUrls(_webhookUrl);
			builder.Services.AddCors(options => { //⛔ ⚠️  Enable CORS
				options.AddDefaultPolicy(policy => {
					policy
						.AllowAnyOrigin()
						.AllowAnyHeader()
						.AllowAnyMethod();
				});
			});

			var app = builder.Build();
			app.UseCors(); // must be before MapPost
			app.MapPost("/webhook", async context => {// POST endpoint for webhook
				try {
					using var reader = new StreamReader(context.Request.Body);
					var body = await reader.ReadToEndAsync();
					_logger.LogInformation("✅ Received webhook: {Body}", body);
					context.Response.StatusCode = 200;
					await context.Response.WriteAsync("Webhook received");
				} catch (Exception ex) {
					_logger.LogError(ex, "Error processing webhook request");
					context.Response.StatusCode = 500;
					await context.Response.WriteAsync("Error");
				}
			});
			_logger.LogInformation("🚀 Starting Kestrel webhook listener at {WebhookUrl}", _webhookUrl);
			await app.RunAsync(stoppingToken);
		} catch (Exception ex) {
			_logger.LogError(ex, "❌ Failed to start webhook listener");
		}
	}
}

