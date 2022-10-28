// ReSharper disable CheckNamespace

using System;
using System.Net.Http;
using EventStore.Client;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection {
	/// <summary>
	/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="EventStoreClient"/>.
	/// </summary>
	public static class EventStoreClientServiceCollectionExtensions {
#if GRPC_NETSTANDARD
		/// <summary>
		/// Adds an <see cref="EventStoreClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="address"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStoreClient(this IServiceCollection services, Uri address)
			=> services.AddEventStoreClient(options => {
				options.ConnectivitySettings.Address = address;
			});
#else
		/// <summary>
		/// Adds an <see cref="EventStoreClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="address"></param>
		/// <param name="createHttpMessageHandler"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStoreClient(this IServiceCollection services, Uri address,
			Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddEventStoreClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});
#endif

#if NETCOREAPP3_1
		/// <summary>
		/// Adds an <see cref="EventStoreClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="address"></param>
		/// <param name="createHttpMessageHandler"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStoreClient(this IServiceCollection services, Uri address,
			// ReSharper disable once MethodOverloadWithOptionalParameter
			Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddEventStoreClient(options => {
				options.ConnectivitySettings.Address = address;
			});
#endif
		/// <summary>
		/// Adds an <see cref="EventStoreClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStoreClient(this IServiceCollection services,
			Action<EventStoreClientSettings>? configureSettings = null) =>
			services.AddEventStoreClient(new EventStoreClientSettings(), configureSettings);


		/// <summary>
		/// Adds an <see cref="EventStoreClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="connectionString"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStoreClient(this IServiceCollection services,
			string connectionString, Action<EventStoreClientSettings>? configureSettings = null) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			return services.AddEventStoreClient(EventStoreClientSettings.Create(connectionString), configureSettings);
		}


		private static IServiceCollection AddEventStoreClient(this IServiceCollection services,
			EventStoreClientSettings settings,
			Action<EventStoreClientSettings>? configureSettings) {
			configureSettings?.Invoke(settings);

			services.TryAddSingleton(provider => {
				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new EventStoreClient(settings);
			});

			return services;
		}
	}
}
// ReSharper restore CheckNamespace
