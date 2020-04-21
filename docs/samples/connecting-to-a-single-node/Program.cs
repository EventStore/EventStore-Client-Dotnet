using System;
using System.Net.Http;
using EventStore.Client;

namespace connecting_to_a_single_node {
	class Program {
		static void Main(string[] args) {
		}

		private static void SimpleConnection() {
			#region creating-simple-connection
			var settings = new EventStoreClientSettings {
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				}
			};

			var client = new EventStoreClient(settings);
			#endregion creating-simple-connection
		}

		private static void ProvidingDefaultCredentials() {
			#region providing-default-credentials
			var settings = new EventStoreClientSettings {
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				},
				DefaultCredentials = new UserCredentials("admin", "changeit")
			};

			var client = new EventStoreClient(settings);
			#endregion providing-default-credentials
		}

		private static void SpecifyingAConnectionName() {
			#region setting-the-connection-name
			var settings = new EventStoreClientSettings {
				ConnectionName = "Some Connection",
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				}
			};
			#endregion setting-the-connection-name

			var client = new EventStoreClient(settings);
		}

		private static void OverridingTheTimeout() {
			#region overriding-timeout
			var settings = new EventStoreClientSettings {
				OperationOptions = new EventStoreClientOperationOptions {
					TimeoutAfter = TimeSpan.FromSeconds(30)
				},
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				}
			};
			#endregion overriding-timeout

			var client = new EventStoreClient(settings);
		}

		private static void CreatingAnInterceptor() {
			#region adding-an-interceptor
			var settings = new EventStoreClientSettings {
				Interceptors = new[] {new DemoInterceptor()},
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				}
			};
			#endregion adding-an-interceptor

			var client = new EventStoreClient(settings);
		}

		private static void CustomHttpMessageHandler() {
			#region adding-an-custom-http-message-handler
			var settings = new EventStoreClientSettings {
				CreateHttpMessageHandler = () =>
					new HttpClientHandler {
						ServerCertificateCustomValidationCallback =
							(sender, cert, chain, sslPolicyErrors) => true
					},
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				}
			};
			#endregion adding-an-custom-http-message-handler

			var client = new EventStoreClient(settings);
		}
	}
}
