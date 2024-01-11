#if NET
using System.Net;
using EventStore.Client.ServerFeatures;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace EventStore.Client.Tests;

public class GrpcServerCapabilitiesClientTests {
	public static IEnumerable<object?[]> ExpectedResultsCases() {
		yield return new object?[] { new SupportedMethods(), new ServerCapabilities() };
		yield return new object?[] {
			new SupportedMethods {
				Methods = {
					new SupportedMethod {
						ServiceName = "event_store.client.streams.streams",
						MethodName  = "batchappend"
					}
				}
			},
			new ServerCapabilities(true)
		};

		yield return new object?[] {
			new SupportedMethods {
				Methods = {
					new SupportedMethod {
						ServiceName = "event_store.client.persistent_subscriptions.persistentsubscriptions",
						MethodName  = "read",
						Features = {
							"all"
						}
					}
				}
			},
			new ServerCapabilities(SupportsPersistentSubscriptionsToAll: true)
		};

		yield return new object?[] {
			new SupportedMethods {
				Methods = {
					new SupportedMethod {
						ServiceName = "event_store.client.persistent_subscriptions.persistentsubscriptions",
						MethodName  = "read"
					}
				}
			},
			new ServerCapabilities()
		};
	}

	[Theory]
	[MemberData(nameof(ExpectedResultsCases))]
	internal async Task GetAsyncReturnsExpectedResults(
		SupportedMethods supportedMethods,
		ServerCapabilities expected
	) {
		using var kestrel = new TestServer(
			new WebHostBuilder()
				.ConfigureServices(
					services => services
						.AddRouting()
						.AddGrpc().Services
						.AddSingleton(new FakeServerFeatures(supportedMethods))
				)
				.Configure(
					app => app
						.UseRouting()
						.UseEndpoints(ep => ep.MapGrpcService<FakeServerFeatures>())
				)
		);

		var sut = new GrpcServerCapabilitiesClient(new());

		var actual =
			await sut.GetAsync(
				ChannelFactory
					.CreateChannel(
						new() {
							CreateHttpMessageHandler = kestrel.CreateHandler
						},
						new DnsEndPoint("localhost", 80)
					)
					.CreateCallInvoker(),
				default
			);

		Assert.Equal(expected, actual);
	}

	class FakeServerFeatures : ServerFeatures.ServerFeatures.ServerFeaturesBase {
		readonly SupportedMethods _supportedMethods;

		public FakeServerFeatures(SupportedMethods supportedMethods) => _supportedMethods = supportedMethods;

		public override Task<SupportedMethods> GetSupportedMethods(Empty request, ServerCallContext context) => Task.FromResult(_supportedMethods);
	}
}
#endif
