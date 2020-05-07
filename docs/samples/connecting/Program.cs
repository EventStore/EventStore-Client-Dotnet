using System;
using System.Net.Http;
using EventStore.Client;

namespace connecting {
	class Program {
		static void Main(string[] args) {
		}

		private static void SimpleConnection() {
			//creating-simple-connection
			var settings = new EventStoreClientSettings {
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				}
			};

			var connection = new EventStoreClient(settings);
			//creating-simple-connection
		}

		private static void SpecifyingAConnectionName() {
			//setting-the-connection-name
			var settings = new EventStoreClientSettings {
				ConnectionName = "Some Connection",
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				}
			};
			//setting-the-connection-name

			var connection = new EventStoreClient(settings);
		}

		private static void OverridingTheTimeout() {
			//overriding-timeout
			var settings = new EventStoreClientSettings {
				OperationOptions = new EventStoreClientOperationOptions {
					TimeoutAfter = TimeSpan.FromSeconds(30)
				},
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				}
			};
			//overriding-timeout

			var connection = new EventStoreClient(settings);
		}

		private static void CreatingAnInterceptor() {
			//adding-an-interceptor
			var settings = new EventStoreClientSettings {
				Interceptors = new[] {new DemoInterceptor()},
				ConnectivitySettings = {
					Address = new Uri("https://localhost:2113")
				}
			};
			//adding-an-interceptor

			var connection = new EventStoreClient(settings);
		}

		private static void CustomHttpMessageHandler() {
			//adding-an-custom-http-message-handler
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
			//adding-an-custom-http-message-handler

			var connection = new EventStoreClient(settings);
		}
	}
}
