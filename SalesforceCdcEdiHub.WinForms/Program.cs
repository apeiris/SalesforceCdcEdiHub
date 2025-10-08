using System.Diagnostics;
using System.Text;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Windows.Forms;
using WinForms;

namespace SalesforceCdcEdiHub.WinForms;

internal static class Program {
	[STAThread]
	static void Main() {
		Directory.CreateDirectory("logs");
		Console.SetOut(new DebugTextWriter());
		Console.WriteLine($"Creating logs directory {Directory.GetCurrentDirectory()}");

		// WinForms initialization must come first
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);

		// Initialize NLog
		var nlogLogger = LogManager.Setup()
								  .LoadConfigurationFromFile("nlog.config", optional: true)
								  .GetCurrentClassLogger();

		try {
			nlogLogger.Info("Starting WinForms host...");

			// Build host
			using var host = CreateHostBuilder().Build();

			// Start host
			host.Start();

			// Logging works now via Microsoft.Extensions.Logging
			var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
			var logger = loggerFactory.CreateLogger("Program");
			logger.LogInformation("Host started successfully.");

			// Resolve main form
			var form = host.Services.GetRequiredService<MainForm>();

			// Attach RichTextBox target dynamically
			AttachRichTextBoxTarget(form);

			Application.Run(form);

			host.Dispose();
		} catch (Exception ex) {
			nlogLogger.Error(ex, "Application stopped because of exception");
			throw;
		}
		finally {
			LogManager.Shutdown();
		}
	}

	static void AttachRichTextBoxTarget(MainForm form) {
		//	var rtbTarget = LogManager.Configuration.FindTargetByName<NLog.Targets.RichTextBoxTarget>("rtb");
		var rtbTarget = LogManager.Configuration.FindTargetByName<RichTextBoxTarget>("rtb");

		if (rtbTarget != null) {
			rtbTarget.FormName = form.Name;
			rtbTarget.ControlName = form.rtxLog.Name; // Assuming property LogTextBox exists
			LogManager.ReconfigExistingLoggers();
		}
	}

	public class DebugTextWriter : TextWriter {
		public override Encoding Encoding => Encoding.UTF8;
		public override void Write(char value) => Debug.Write(value);
		public override void Write(string? value) => Debug.Write(value);
		public override void WriteLine(string? value) => Debug.WriteLine(value);
	}

	static IHostBuilder CreateHostBuilder() =>
		Host.CreateDefaultBuilder()
			.ConfigureAppConfiguration((context, config) => {
				config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
					  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
					  .AddEnvironmentVariables();
			})
			.ConfigureServices((context, services) => {
				// Config
				services.Configure<SalesforceConfig>(context.Configuration.GetSection("Salesforce"));
				services.Configure<SqlServerConfig>(context.Configuration.GetSection("SqlServer"));

				// Core services
				services.AddMemoryCache();
				services.AddScoped<ISalesforceService, SalesforceService>();
				services.AddScoped<PubSubService>();
				services.AddScoped<SqlServerLib>();
				services.AddScoped<X12>();
				services.AddHttpClient();

				// Hosted Kestrel-based WebhookListener
				services.AddHostedService<KestrelWebhookListener>();

				// MainForm
				services.AddScoped<MainForm>();

				// Logging
				services.AddLogging(loggingBuilder => {
					loggingBuilder.ClearProviders();
					loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
					loggingBuilder.AddNLog();
				});
			});
}

