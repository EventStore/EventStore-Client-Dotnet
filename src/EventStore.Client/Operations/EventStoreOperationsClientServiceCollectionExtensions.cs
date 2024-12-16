// ReSharper disable CheckNamespace

using System;
using System.Net.Http;
using EventStore.Client;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using EventStoreOperationsClient = EventStore.Client.EventStoreOperationsClient;

namespace Microsoft.Extensions.DependencyInjection {
	/// <summary>
	/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="EventStoreOperationsClient"/>.
	/// </summary>
	public static class EventStoreOperationsClientServiceCollectionExtensions {

		/// <summary>
		/// Adds an <see cref="EventStoreOperationsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="address"></param>
		/// <param name="createHttpMessageHandler"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStoreOperationsClient(this IServiceCollection services, Uri address,
			Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddEventStoreOperationsClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		/// <summary>
		/// Adds an <see cref="EventStoreOperationsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configureOptions"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStoreOperationsClient(this IServiceCollection services,
			Action<EventStoreClientSettings>? configureOptions = null) =>
			services.AddEventStoreOperationsClient(new EventStoreClientSettings(), configureOptions);

		/// <summary>
		/// Adds an <see cref="EventStoreOperationsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="connectionString"></param>
		/// <param name="configureOptions"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStoreOperationsClient(this IServiceCollection services,
			string connectionString, Action<EventStoreClientSettings>? configureOptions = null) =>
			services.AddEventStoreOperationsClient(EventStoreClientSettings.Create(connectionString), configureOptions);

		private static IServiceCollection AddEventStoreOperationsClient(this IServiceCollection services,
			EventStoreClientSettings options, Action<EventStoreClientSettings>? configureOptions) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			configureOptions?.Invoke(options);

			services.TryAddSingleton(provider => {
				options.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				options.Interceptors ??= provider.GetServices<Interceptor>();

				return new EventStoreOperationsClient(options);
			});

			return services;
		}
	}
}
// ReSharper restore CheckNamespace
