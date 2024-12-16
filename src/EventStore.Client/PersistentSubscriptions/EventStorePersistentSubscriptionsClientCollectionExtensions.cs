// ReSharper disable CheckNamespace

using System;
using System.Net.Http;
using EventStore.Client;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection {
	/// <summary>
	/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="EventStorePersistentSubscriptionsClient"/>.
	/// </summary>
	public static class EventStorePersistentSubscriptionsClientCollectionExtensions {
		/// <summary>
		/// Adds an <see cref="EventStorePersistentSubscriptionsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStorePersistentSubscriptionsClient(this IServiceCollection services,
			Uri address, Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddEventStorePersistentSubscriptionsClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		/// <summary>
		/// Adds an <see cref="EventStorePersistentSubscriptionsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStorePersistentSubscriptionsClient(this IServiceCollection services,
			Action<EventStoreClientSettings>? configureSettings = null) =>
			services.AddEventStorePersistentSubscriptionsClient(new EventStoreClientSettings(),
				configureSettings);

		/// <summary>
		/// Adds an <see cref="EventStorePersistentSubscriptionsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStorePersistentSubscriptionsClient(this IServiceCollection services,
			string connectionString, Action<EventStoreClientSettings>? configureSettings = null) =>
			services.AddEventStorePersistentSubscriptionsClient(EventStoreClientSettings.Create(connectionString),
				configureSettings);

		private static IServiceCollection AddEventStorePersistentSubscriptionsClient(this IServiceCollection services,
			EventStoreClientSettings settings, Action<EventStoreClientSettings>? configureSettings) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			configureSettings?.Invoke(settings);
			services.TryAddSingleton(provider => {
				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new EventStorePersistentSubscriptionsClient(settings);
			});
			return services;
		}
	}
}
// ReSharper restore CheckNamespace
