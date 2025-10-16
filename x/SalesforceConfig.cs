using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Common;
public class SalesforceConfig {
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }
	public string Username { get; set; }
	public string Password { get; set; }
	public string ApiVersion { get; set; }
	public string LoginUrl { get; set; }
	public string GrpcUrl { get; set; }
	public string PubSubEndpoint { get; set; }
	public string SFSchemaName { get; set; }
	public List<Topic> Topics { get; set; }
	public string pfxPath { get; set; }
	public string pfxPassword { get; set; }
	}

public class Topic {
	public string Name { get; set; }
	public List<string> FieldsToFilter { get; set; }
	}
public static class TopicExtensions {
	public static List<string> GetFieldsToFilterByName(this List<Topic> topics, string name) {
		return topics.FirstOrDefault(t => t.Name == name)?.FieldsToFilter ?? new List<string>();
		}
	}

