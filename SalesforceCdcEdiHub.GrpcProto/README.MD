## SalesforceCdcEdiHub.GrpcProto Project

This project contains the gRPC client for Salesforce CDC Pub/Sub events. It generates C# gRPC classes from the `Salesforce.proto` file and makes them available to other projects in the solution.

### Project Overview

- **Target Framework:** .NET 9.0 (`net9.0`)  
- **Nullable Reference Types:** Enabled  
- **Implicit Usings:** Enabled  

### gRPC / Protobuf Configuration

- **Generated Code Output Folder:** `Generated`  
- **Protobuf Compilation:** The `Salesforce.proto` file is compiled into C# client classes (`FetchRequest`, `FetchResponse`, `PubSubClient`, etc.) inside the `Generated` folder.  
- **Generated Files Included in Project:** Explicitly included in Visual Studio with `AutoGen` and `DesignTime` flags.

### NuGet Packages

| Package | Version | Description |
|---------|---------|-------------|
| `Google.Protobuf` | 3.32.1 | Provides Protocol Buffers support for C#. |
| `Grpc.Net.Client` | 2.71.0 | gRPC client for .NET applications. |
| `Grpc.Tools` | 2.72.0 | Tooling for compiling `.proto` files into C# code. |

### Project File Highlights

- **Prevent Duplicate Compilation:**  
  `EnableDefaultCompileItems=false` avoids duplicate `Compile` items for generated files.  

- **Automatic Generated Folder:**  
  Ensures `Generated` folder exists before compiling `.proto` files.  

- **Explicit C# File Inclusion:**  
  Normal C# files (`**\*.cs`) are included, excluding generated files. Generated files are included separately.

### Folder Structure

```text
SalesforceCdcEdiHub.GrpcProto/
│   SalesforceCdcEdiHub.GrpcProto.csproj
├── Proto/
│   └── Salesforce.proto
├── Generated/
│   ├── SalesforceGrpc.cs
│   └── Salesforce.cs
└── Other C# source files

### How it Works
1. The .proto file (Proto\Salesforce.proto) is compiled during build using Grpc.Tools.

2. Generated C# classes are placed in the Generated folder.

3. The PubSubClient and message classes (FetchRequest, FetchResponse, etc.) can be consumed by other projects in the solution.