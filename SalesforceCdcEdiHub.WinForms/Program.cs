using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Windows.Forms;
using SalesforceCdcEdiHub;
using SalesforceCdcEdiHub.Common;
using SalesforceCdcEdiHub.WinForms;
using SalesfroceCdcEdiHub.Common;
using WinForms;

namespace SalesforceCdcEdiHub.WinForms {
	internal static class Program {
		[STAThread]
		static void Main() {
			var nlogAssembly = typeof(NLog.LogManager).Assembly;
			

			Directory.CreateDirectory("logs");
			Console.SetOut(new DebugTextWriter());
		
			// WinForms initialization
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Initialize NLog
			var nlogLogger = LogManager.Setup()
				.LoadConfigurationFromFile("nlog.config", optional: true)
				.GetCurrentClassLogger();

			try {
				nlogLogger.Info("Starting WinForms host...");

				using var host = CreateHostBuilder().Build();

				// Start the host
				host.Start();

				var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
				var logger = loggerFactory.CreateLogger("Program");
				logger.LogInformation("Host started successfully.");

				// Resolve main form
				var form = host.Services.GetRequiredService<MainForm>();

				// Attach RichTextBoxTarget for NLog
				AttachRichTextBoxTarget(form);

				// Resolve webhook listener
				var webhookListener = host.Services.GetRequiredService<KestrelWebhookListener>();

				// Run Kestrel in background for webhook endpoint
				Task.Run(() => {
					var builder = WebApplication.CreateBuilder();

					// Register CORS for UseCors middleware
					builder.Services.AddCors(options => {
						options.AddDefaultPolicy(policy => {
							policy
								.AllowAnyOrigin()
								.AllowAnyHeader()
								.AllowAnyMethod();
						});
					});

					builder.WebHost.UseUrls("http://0.0.0.0:5005"); // listen on all interfaces
					var app = builder.Build();

					// Enable CORS
					app.UseCors();

					// Map webhook endpoint
					webhookListener.MapWebhook(app);

					app.Run();
				});

				// Subscribe to webhook events
				webhookListener.WebHookEvent += (sender, e) => {
					logger.LogInformation("Webhook received: {Message}", e.Message);
					// Optionally update MainForm UI here
				};

				// Start WinForms UI
				Application.Run(form);
			} catch (Exception ex) {
				nlogLogger.Error(ex, "Application stopped due to exception");
				throw;
			}
			finally {
				LogManager.Shutdown();
			}
		}

		static void AttachRichTextBoxTarget(MainForm form) {
			var rtbTarget = LogManager.Configuration.FindTargetByName<RichTextBoxTarget>("rtb");
			if (rtbTarget != null) {
				rtbTarget.FormName = form.Name;
				rtbTarget.ControlName = form.rtxLog.Name; // RichTextBox control
				LogManager.ReconfigExistingLoggers();
			}
		}

		static IHostBuilder CreateHostBuilder() =>
			Host.CreateDefaultBuilder()
				.ConfigureAppConfiguration((context, config) => {
					config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
						  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
						  .AddEnvironmentVariables();
				})
				.ConfigureServices((context, services) => {
					// Config sections
					services.Configure<SalesforceConfig>(context.Configuration.GetSection("Salesforce"));
					services.Configure<SqlServerConfig>(context.Configuration.GetSection("SqlServer"));
					services.Configure<OpenAs2Config>(context.Configuration.GetSection("OpenAs2")); // 👈 add this

					// Core services
					services.AddMemoryCache();
					services.AddScoped<ISalesforceService, SalesforceService>();
					services.AddScoped<PubSubService>();
					services.AddScoped<SqlServerLib>();
					services.AddScoped<X12>();
					services.AddHttpClient();

					// Webhook listener
					services.AddSingleton<KestrelWebhookListener>();

					// Main form
					services.AddScoped<MainForm>();

					// Logging
					services.AddLogging(loggingBuilder => {
						loggingBuilder.ClearProviders();
						loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
						loggingBuilder.AddNLog();
					});
				})
				.UseConsoleLifetime();
	}

	// Custom Debug TextWriter for Console output in Debug
	public class DebugTextWriter : TextWriter {
		public override Encoding Encoding => Encoding.UTF8;
		public override void Write(char value) => System.Diagnostics.Debug.Write(value);
		public override void Write(string? value) => System.Diagnostics.Debug.Write(value);
		public override void WriteLine(string? value) => System.Diagnostics.Debug.WriteLine(value);
	}
}
