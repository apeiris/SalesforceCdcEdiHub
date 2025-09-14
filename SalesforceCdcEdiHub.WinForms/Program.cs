using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using WinForms;
using Common;
namespace SalesforceCdcEdiHub.WinForms;
static class Program {
	[STAThread]
	static void Main() {
		Directory.CreateDirectory("logs");
		Console.SetOut(new DebugTextWriter());
		Console.WriteLine("Creating logs directory");
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		using (var host = CreateHostBuilder().Build()) {

			var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
			var logger = loggerFactory.CreateLogger("TestFrm.Program");
			logger.LogInformation("Host built successfully.");
			host.Start();

			var sqlServerLib = host.Services.GetRequiredService<SqlServerLib>();
			//var form = host.Services.GetRequiredService<MainForm>();
			var form = host.Services.GetRequiredService<MainForm>(); // Manually inject SqlServerLib
			Application.Run(form);
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
						  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
				})
				.ConfigureServices((context, services) => {
					services.Configure<SalesforceConfig>(context.Configuration.GetSection("Salesforce"));
					services.Configure<SqlServerConfig>(context.Configuration.GetSection("SqlServer")); // Addcd  SqlServerConfig
					services.AddMemoryCache(); // For IMemoryCache
					services.AddScoped<ISalesforceService, SalesforceService>();
					services.AddScoped<PubSubService>(); // Register PubSubService	
					services.AddScoped<SqlServerLib>();
					services.AddScoped<X12>();
					services.AddHttpClient();

					services.AddScoped<MainForm>(); // Register the form
					services.AddLogging(loggingBuilder => {
						loggingBuilder.ClearProviders();
						loggingBuilder.SetMinimumLevel(LogLevel.Debug);
						loggingBuilder.AddNLog();
					});
				});

	}
