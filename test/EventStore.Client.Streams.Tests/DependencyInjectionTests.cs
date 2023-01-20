using System;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventStore.Client {
	public class DependencyInjectionTests {
		[Fact]
		public void Register() =>
			new ServiceCollection()
				.AddEventStoreClient()
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

		[Fact]
		public void RegisterWithServiceProvider() =>
			new ServiceCollection()
				.AddEventStoreClient(provider => { })
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

		[Fact]
		public void RegisterSimple() =>
			new ServiceCollection()
				.AddEventStoreClient(new Uri("https://localhost:1234"))
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

		[Fact]
		public void RegisterSimpleWithServiceProvider() =>
			new ServiceCollection()
				.AddEventStoreClient(provider => new Uri("https://localhost:1234"))
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

		[Fact]
		public void RegisterWithAction() =>
			new ServiceCollection()
				.AddEventStoreClient(settings => { })
				.BuildServiceProvider()
				.GetRequiredService<EventStoreClient>();

		[Fact]
		public void RegisterWithActionAndServiceProvider() =>
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
