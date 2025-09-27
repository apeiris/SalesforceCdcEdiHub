### Adding new *Salesforce* Status to facilitate EDI integration

Add new picklist values (`Completed` and `Revision Required`) to the `Status__c` field in Salesforce.  
These values correspond to the order status flow shown in the sequence diagram, tracking the order lifecycle between Salesforce and the Manufacturer.

---

## Steps to Add Picklist Values

1. **Navigate to Object Manager in Setup**
   - In the **Quick Find** box, search for **Order__c** (or your custom object) and select it.

2. **Access the Status__c Field**
   - In **Order__c**, click **Fields & Relationships**.
   - Click the `Status__c` field name to edit.

3. **Add New Picklist Values**
   - In the **Picklist Values** section, click **New**.
   - Enter the values:
     - `Completed`
     - `Revision Required`
   - (Optional) Set a default value for new orders (e.g., `Draft`).
   - Click **Save**.

4. **Update Record Types (If Applicable)**
   - If your org uses record types, ensure the new values are assigned to the relevant record types.
   - Go to **Object Manager** > **Order__c** > **Record Types**.
   - Select a record type and edit the picklist values for `Status__c`.
   - Add `Completed` and `Revision Required` to the **Selected Values** list.
   - Save.

5. **Verify Field-Level Security**
   - Ensure the `Status__c` field is accessible to the relevant profiles:
     - Click **Set Field-Level Securi**
