using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace EventStore.Client {
	public class DependencyInjectionTests {
		[Fact]
		public void Register() =>
			new ServiceCollection()
				.AddEventStoreClient()
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

		[Fact]
		public void RegisterWithConnectionString() =>
			new ServiceCollection()
				.AddEventStoreClient("esdb://localhost:2113?tls=false")
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

		[Fact]
		public void RegisterWithConnectionStringFactory() =>
			new ServiceCollection()
				.AddEventStoreClient(connectionStringFactory: provider => "esdb://localhost:2113?tls=false")
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

		[Fact]
		public void RegisterWithUri() =>
			new ServiceCollection()
				.AddEventStoreClient(new Uri("https://localhost:1234"))
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

		[Fact]
		public void RegisterWithUriFactory() =>
			new ServiceCollection()
				.AddEventStoreClient(provider => new Uri("https://localhost:1234"))
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

		[Fact]
		public void RegisterWithSettings() =>
			new ServiceCollection()
				.AddEventStoreClient(settings => { })
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

		[Fact]
		public void RegisterWithSettingsFactory() =>
			new ServiceCollection()
				.AddEventStoreClient(provider => settings => { })
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

		[Fact]
		public void RegisterInterceptors() {
			bool interceptorResolved = false;
			new ServiceCollection()
				.AddEventStoreClient()
				.AddSingleton<ConstructorInvoked>(() => interceptorResolved = true)
				.AddSingleton<Interceptor, TestInterceptor>()
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

			Assert.True(interceptorResolved);
		}

		private delegate void ConstructorInvoked();

		private class TestInterceptor : Interceptor {
			public TestInterceptor(ConstructorInvoked invoked) {
				invoked.Invoke();
			}
		}
	}
}
