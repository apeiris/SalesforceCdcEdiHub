using System;
using System.Collections.Generic;

namespace Models;

public class SqlTableEvent : SqlEvent {
	public string TableName { get; set; }
	public string OperationType { get; set; } // INSERT, UPDATE, DELETE
	public string RecordId { get; set; }
	public object ChangeData { get; set; }
	public bool Success { get; set; }
	public string ErrorMessage { get; set; }
	public Dictionary<string, object> Metadata { get; set; } = new();

	// Salesforce CDC specific fields
	public string ChangeEventHeaderJson { get; set; }
	public string EntityName { get; set; }
	public long SequenceNumber { get; set; }
	public DateTime ChangeEventTimestamp { get; set; }

	public SqlTableEvent() {
		Timestamp = DateTime.UtcNow;
		Source = "SqlServerLib";
		EventType = "SQL_TABLE_COMPLETION";
	}
}

// Models/SqlEvent.cs (Base Class)
public abstract class SqlEvent {
	public string EventId { get; set; } = Guid.NewGuid().ToString();
	public DateTime Timestamp { get; set; }
	public string Source { get; set; }
	public string EventType { get; set; }
	public EventStatus Status { get; set; }
	public string TransactionId { get; set; }
	public string CorrelationId { get; set; }
}

public enum EventStatus {
	Processing,
	Success,
	Failed,
	Retrying
}

