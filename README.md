# Salesforce CDC EDI Hub

## Overview

This application integrates with Salesforce using the **Pub/Sub API** to listen for real-time change events (Create, Update, Delete) on selected Salesforce objects. It also simulates submission of Salesforce EBikes orders to the manufacturer via the Pub/Sub API over the **OpenAS2 protocol**.  

When an event is received, it is **deserialized using Google Protobuf**, processed, and synchronized into a **SQL Server database**, ensuring that Salesforce data remains up-to-date and accessible locally.  

Additionally, the project implements **EDI X12 interactions via the AS2 protocol**. For example, when an order status changes to `'Submitted for Manufacturing'`, the system automatically generates and transmits the corresponding EDI message.

---

## Prerequisites

### Salesforce Setup
- Enable **Change Data Capture (CDC)** or relevant **Platform Events** for the target objects.  
- Ensure a Salesforce user with API access and sufficient permissions to subscribe to these events.

### SQL Server
- A reachable SQL Server instance.  
- A database with tables corresponding to the Salesforce objects you want to sync. The application can dynamically create or update tables if configured.

### Environment Configuration
- **SQL Server connection string** (configured in `appsettings.json`).  
- **Salesforce credentials** and OAuth configuration (in `appsettings.json`).  
- **Optional:** Folder for storing **Protobuf schema files** used for event deserialization.

---

## Features
- **Real-time subscription** to Salesforce change events via the Pub/Sub API.  
- **Automatic deserialization** of event payloads using Google Protobuf.  
- **Flexible processing pipeline** to insert or update records in SQL Server.  
- **EDI X12 support** over AS2 protocol for order submission and status updates.  
- **Logging and status reporting** to monitor synchronization and EDI processing.

## Order Status 
The **Order Status** tracks the progress of an order as it moves between Salesforce (sales side) and the Manufacturer (production side).
- **Draft** *(Salesforce)*  
  Order is newly created in Salesforce.

- **Submitted to Manufacturing** *(Salesforce → Manufacturer)*  
  Order is formally sent to the manufacturer for review and scheduling.

- **Revision Required** *(Manufacturer → Salesforce)*  
  Manufacturer requests changes due to missing/incorrect details, pricing/options issues, or capacity limits.  
  A separate email from the manufacturer provides specific revision details.

- **In Production** *(Manufacturer)*  
  Order is accepted and is actively being manufactured.

- **Completed** *(Manufacturer → Salesforce)*  
  Manufacturing is finished, and the order is marked as ready for delivery in Salesforce.

  ---



# Order Status Flow

The **Order Status** tracks the progress of an order as it moves between **Salesforce (sales side)** and the **Manufacturer (production side)**.

# Order Status Flow

The **Order Status** tracks the progress of an order as it moves between **Salesforce (sales side)** and **Manufacturer (production side)**. A decision point after submission determines if revisions are required or if the order is accepted.
# Order Status Sequence Diagram

The **Order Status** tracks the interactions between **Salesforce (sales side)** and **Manufacturer (production side)** as an order progresses through its lifecycle, including a decision point for revisions.

```mermaid
sequenceDiagram
    participant S as Salesforce
    participant AS2 as AS2 Hub
    participant M as Manufacturer

    S->>S: Create Draft Order
    S->>AS2: Submit Order for Review
    AS2->>M: Transmit Order (X12-850)
    M->>M: Review Order
    alt Review Required?
        M->>AS2: Request Revision (Email with details)
        note left of M: Email sent with required revision details 
        loop Revisions Needed
            S->>AS2: Submitted to Manufacturing
            AS2->>M: Transmit Revised Order
            M->>M: Review Revised Order
        end
    else No Revisions
        M->>M: Accept Order
        M->>AS2: Confirm Acceptance (X12-855)
        AS2->>S: In Production 
        
    end
    M->>M: Start Production
    M->>S: (Order) Completed (X12-856)