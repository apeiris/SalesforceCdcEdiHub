# Application Configuration (`appsettings.json`)

> **Note:** This file is **excluded from the GitHub repository** . You must create or edit manually before running the application.

The configuration is organized into three main sections:

---

## Salesforce

Holds Salesforce-related credentials and connection settings.

```json
{
  "Salesforce": {
    "ClientId": "<YOUR_SALESFORCE_CONSUMER_KEY>",
    "pfxPath": "<PATH_TO_YOUR_PFX_CERTIFICATE>",
    "pfxPassword": "<CERT_PASSWORD>",
    "Username": "<SALESFORCE_USERNAME>",
    "ApiVersion": "64.0",
    "LoginUrl": "https://login.salesforce.com",
    "GrpcUrl": "https://api.pubsub.salesforce.com:7443",
    "PubSubEndpoint": "https://api.pubsub.salesforce.com:7443",
    "RedirectUri": "https://localhost:500/callback",
    "RdirectUriNote":"not required",
    "SFSchemaName": "sfo"
  },
  "Serilog": {
    "MinimumLevel": { "Default": "Verbose" },
    "Using": ["Serilog.Sinks.Console"],
    "Enrich": ["WithCaller", "FromLogContext"],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Level:u3}] {Message} (at {CallerMemberName} in {CallerFilePath}){NewLine}"
        }
      }
    ]
  },
  "ConnectionStrings": {
    "mssql": "<YOUR_SQL_SERVER_CONNECTION_STRING>"
  }
}

