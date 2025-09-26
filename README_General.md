### Salesforce EDIHUB Integration Application
#
# This application connects to Salesforce using the Pub/Sub API to listen for change events on specified Salesforce objects. When a change event is received, it processes the event and stores relevant data in a SQL Server database.
# 
# ## Configuration Sections
# 
# ### Salesforce Section
# 
# - `ClientId`: Your Salesforce connected app's consumer key.
# - `pfxPath`: Path to your PFX certificate file for JWT authentication.
# - `pfxPassword`: Password for the PFX certificate.
# - `Username`: Salesforce username for the connected app.
# - `ApiVersion`: Salesforce API version to use (e.g., "64.0").
# - `LoginUrl`: Salesforce login URL (e.g., "https://login.salesforce.com").
# - `GrpcUrl`: URL for the Salesforce Pub/Sub gRPC endpoint.
# - `PubSubEndpoint`: Endpoint for Pub/Sub connections.
# - `RedirectUri`: Redirect URI for OAuth flows (not required for JWT).
# - `SFSchemaName`: Schema name used in Salesforce (e.g., "sfo").
# 
[]: # ### Serilog Section
[]: # 
[]: # Configures logging using Serilog:
[]: # 
[]: # - `MinimumLevel`: Sets the minimum log level (e.g., "Verbose").
[]: # - `Using`: Specifies Serilog sinks to use (e.g., console).
[]: # - `Enrich`: Adds additional context to logs (e.g., caller info).
[]: # - `WriteTo`: Defines where logs are written (e.g., console with a specific output template).
[]: # 
[]: # ### ConnectionStrings Section
[]: # 
[]: # Contains database connection strings:
[]: # 
[]: # - `mssql`: Connection string for your SQL Server database.
[]: # 
[]: # ## Setup Instructions
[]: # 
[]: # 1. **Create a Salesforce Connected App**:
[]: #    - Set up a connected app in Salesforce with JWT Bearer Token flow enabled.
[]: #    - Upload your certificate and note the consumer key.
[]: # 
[]: # 2. **Prepare the PFX Certificate**:
[]: #
