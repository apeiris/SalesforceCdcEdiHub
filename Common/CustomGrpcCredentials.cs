using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;


	public static class CustomGrpcCredentials {
		public static CallCredentials Create(string token, string instanceUrl, string tenantId) {
			if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));
			if (string.IsNullOrEmpty(instanceUrl)) throw new ArgumentNullException(nameof(instanceUrl));
			if (string.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));

			return CallCredentials.FromInterceptor((context, metadata) => {
				metadata.Add("accesstoken", token);
				metadata.Add("instanceurl", instanceUrl);
				metadata.Add("tenantid", tenantId);
				// Optionally include Authorization header, though accesstoken might suffice
				metadata.Add("Authorization", $"Bearer {token}");
				return Task.CompletedTask;
			});
			}
		}
	

