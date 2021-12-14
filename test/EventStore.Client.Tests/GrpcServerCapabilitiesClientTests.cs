#if !GRPC_CORE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.Client.ServerFeatures;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventStore.Client {
	public class GrpcServerCapabilitiesClientTests {
		public static IEnumerable<object[]> ExpectedResultsCases() {
			yield return new object[] {new SupportedMethods(), new ServerCapabilitiesInfo()};
			yield return new object[] {
				new SupportedMethods {
					Methods = {
						new SupportedMethod {
							ServiceName = "event_store.client.streams.streams",
							MethodName = "batchappend"
						}
					}
				},
				new ServerCapabilitiesInfo(SupportsBatchAppend: true)
			};
			yield return new object[] {
				new SupportedMethods {
					Methods = {
						new SupportedMethod {
							ServiceName = "event_store.client.persistent_subscriptions.persistentsubscriptions",
							MethodName = "read",
							Features = { "all" }
						}
					}
				},
				new ServerCapabilitiesInfo(SupportsPersistentSubscriptionsToAll: true)
			};
			yield return new object[] {
				new SupportedMethods {
					Methods = {
						new SupportedMethod {
							ServiceName = "event_store.client.persistent_subscriptions.persistentsubscriptions",
							MethodName = "read"
						}
					}
				},
				new ServerCapabilitiesInfo()
			};
		}

		[Theory, MemberData(nameof(ExpectedResultsCases))]
		internal async Task GetAsyncReturnsExpectedResults(SupportedMethods supportedMethods,
			ServerCapabilitiesInfo expected) {
			using var kestrel = new TestServer(new WebHostBuilder()
				.ConfigureServices(services => services
					.AddRouting()
					.AddGrpc().Services
					.AddSingleton(new FakeServerFeatures(supportedMethods)))
				.Configure(app => app
					.UseRouting()
					.UseEndpoints(ep => ep.MapGrpcService<FakeServerFeatures>())));
			var sut = new GrpcServerCapabilitiesClient(new EventStoreClientSettings());

			var actual =
				await sut.GetAsync(ChannelFactory.CreateChannel(new EventStoreClientSettings {
					CreateHttpMessageHandler = kestrel.CreateHandler
				}, new UriBuilder().Uri));

			Assert.Equal(expected, actual);
		}

		private class FakeServerFeatures : ServerFeatures.ServerFeatures.ServerFeaturesBase {
			private readonly SupportedMethods _supportedMethods;

			public FakeServerFeatures(SupportedMethods supportedMethods) {
				_supportedMethods = supportedMethods;
			}

			public override Task<SupportedMethods> GetSupportedMethods(Empty request, ServerCallContext context) =>
				Task.FromResult(_supportedMethods);
		}
	}
}
#endif
