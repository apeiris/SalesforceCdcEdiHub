# Salesforce / Monday.Com → OpenAS2 → SQL Server Integration
## Overview
This application demonstrates seamless integration between **Salesforce / Monday.Com** , **OpenAS2**, and **SQL Server**.  
It leverages the **Salesforce Pub/Sub API** to listen for real-time change events—**Create**, **Update**, and **Delete**—on selected Salesforce objects.  
The system also simulates the submission of **Salesforce E-Bikes** orders to the manufacturer via the Pub/Sub API, transmitted over the **OpenAS2 protocol**.

#### Sql server integration is optional and can be disabled if not needed.


### Key Features
- Real-time CDC (Change Data Capture) from Salesforce
- EDI X12 payload generation from monday.com Purchase Orders
- Secure AS2 transmission with encryption and MDN
- Local SQL Server persistence (Dockerized)
- WinForms UI for monitoring and manual control

---

## monday.com → EDI X12 Trigger Workflow

**The X12 EDI payload generation from monday.com can be configured to trigger when a Purchase Order status transitions from any given state to another.**

### Example:
> **`Awaiting Approval` → `Approved`**

Once this **predefined state change** occurs:
1. The system detects the status update via **monday.com webhook or polling**.
2. An **EDI X12 850 (Purchase Order)** message is automatically generated.
3. The payload is sent to a **pre-configured AS2 endpoint**.

### AS2 Endpoint Flexibility
The target endpoint can be:
- **On-Premise**: Local OpenAS2 server (e.g., behind firewall)
- **Cloud-Hosted**: AWS, Azure, or any public AS2 gateway

> Configuration is fully customizable via UI or config files (`SalesforceConfig.cs`, environment variables).

---
---

## Architecture
```mermaid
flowchart LR
    %% External Systems
    SF["Salesforce<br/>(Pub/Sub API)"]
    MC["monday.com<br/>(Purchase Orders)"]

    %% Integration Hub (Core)
    HUB["Integration Hub<br/>(This Application)"]
    X12["X12 Parser<br/>(Internal Layer)"]

    %% Optional & Partner Systems
    SQL["Local SQL Server<br/>(On Docker - Optional)"]
    AS2["OpenAS2<br/>(Trading Partner<br>MDN, Encryption)"]

    %% Group Hub Components
    subgraph hub_container ["Integration Hub"]
        direction TB
        HUB
        X12
    end

    %% Core Required Flows
    SF -->|"Real-Time Events<br>(Protobuf)"| HUB
    MC <-->|"Create/Update PO<br>(GraphQL API)"| HUB
    HUB -->|"EDI X12 Messages"| X12
    X12 <-->|"AS2 Transport<br>(MDN, Encryption)"| AS2

    %% Optional Sync (Dashed Line)
    HUB -.->|"Optional Data Sync"| SQL

    %% Styles
    style SF fill:#00A1E0,stroke:#036,stroke-width:1px,color:#fff
    style MC fill:#00C7B7,stroke:#006D5B,stroke-width:1px,color:#fff
    style HUB fill:#8C52FF,stroke:#3A0070,stroke-width:1px,color:#fff
    style X12 fill:#C13EFF,stroke:#5A0099,stroke-width:1px,color:#fff
    style SQL fill:#0078D7,stroke:#003C7E,stroke-width:1px,color:#fff,opacity:0.7
    style AS2 fill:#FF7B00,stroke:#703500,stroke-width:1px,color:#fff
    style hub_container fill:#E6F7FF,stroke:#8C52FF,stroke-dasharray: 5 5

    %% Emphasize SQL is optional
    classDef optional fill:#0078D7,stroke:#003C7E,stroke-dasharray: 3 3,opacity:0.6
    class SQL optional
```
---


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
    AS2->>M: Transmit Order (X12-850-PO)
    M->>M: Review Order
    alt Revision Required?
        M->>AS2: Request Revision (Email with details)
        note left of M: Email sent with required revision details
        loop Revisions Needed
            S->>AS2: Submit Revised Order
            AS2->>M: Transmit Revised Order
            M->>M: Review Revised Order
        end
    else No Revisions
        M->>M: Accept Order
        M->>AS2: Confirm Acceptance (X12-855-ACK)
        AS2->>S: Update Status: In Production
    end
    M->>M: Start Production
    M->>M: Complete Production
    M->>M: Prepare Shipment
    M->>AS2: Send ShipNotice (X12-856-ASN)
    AS2->>S: Update Status: Update Shipping Info
    M->>AS2: Send Invoice (x12-810-INV)
    AS2->>S: Update Status: Order Completed
```

---
## Partnership defintions for EDI Integration
The E-Bikes Sales(E-Bikes-S) and E-Bikes Manufacturing (E-Bikes-M) are seperate entities. They operate independently but collaborate closely on order processing and fulfillment. The E-Bikes Sales team focuses on customer interactions, order management, and sales strategies, while the E-Bikes Manufacturing team handles production, quality control, and logistics. Their partnership ensures a seamless experience for customers from order placement to delivery.<br>

**E-Bikes-S**: Manages customer orders, sales processes, and order submissions to the manufacturer.

**E-Bikes-M**: Responsible for producing the bikes, managing inventory, and fulfilling orders received from E-Bikes Sales.

Based on the partnership, the E-Bikes Sales team submits orders to the E-Bikes Manufacturing team for production and fulfillment. The manufacturing team reviews, processes, and ships the orders back to the sales team for delivery to customers.
as such, the E-Bikes-S needs to maintain agreed upon partnership details with E-Bikes-M for EDI transactions.
susch as AS2 identifiers, certificates, and endpoint URLs. ensure these details are correctly configured in the application settings to facilitate smooth EDI communication.
###
Creating Partnership profile in the **E-Bikes-S** system:
1. **AS2 Identifier**: Unique identifier for E-Bikes-S in AS2 communications (e.g., `EBIKES-S`). And the corresponding identifier for E-Bikes-M (e.g., `EBIKES-M`).

```mermaid
flowchart LR
    A[OpenAS2 Partnership Details] -->|REST API| B[Middleware / Integration Service]
    B -->|Salesforce REST API| C[Salesforce Custom Object: Trading_Partner__c]

    


























