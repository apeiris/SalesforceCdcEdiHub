# Salesforce CDC EDI Hub

## Overview

This application integrates with Salesforce using the Pub/Sub API to listen for real-time change events (Create, Update, Delete) on selected Salesforce objects. When an event is received, it is deserialized using Google Protobuf, processed, and synchronized into a SQL Server database, ensuring that Salesforce data is always up-to-date and accessible locally.

## Prerequisites

### Salesforce Setup
- **Enable Change Data Capture (CDC)** or relevant Platform Events for the target objects.
- Ensure a Salesforce user with API access and sufficient permissions to subscribe to these events.

### SQL Server
- A reachable SQL Server instance.
- A database with tables corresponding to the Salesforce objects you want to sync (the application can create/update tables dynamically if configured).

### Environment Configuration
- **Connection string** for SQL Server (configured in `appsettings.json`).
- **Salesforce credentials** and OAuth configuration in `appsettings.json`.
- **Optional**: Folder for storing Protobuf schema files used for event deserialization.

## Features
- **Real-time subscription** to Salesforce change events via Pub/Sub API.
- **Automatic deserialization** of event payloads using Google Protobuf.
- **Flexible processing pipeline** to insert or update records in SQL Server.
- **Logging and status reporting** to monitor the synchronization process.
