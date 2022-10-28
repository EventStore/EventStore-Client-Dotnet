// ReSharper disable CheckNamespace

using System;
using System.Net.Http;
using EventStore.Client;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection {
	/// <summary>
	/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="EventStoreUserManagementClient"/>.
	/// </summary>
	public static class EventStoreUserManagementClientCollectionExtensions {
#if GRPC_NETSTANDARD
		/// <summary>
		/// Adds an <see cref="EventStoreUserManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="address"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStoreUserManagementClient(this IServiceCollection services,
			Uri address)
			=> services.AddEventStoreUserManagementClient(options => {
				options.ConnectivitySettings.Address = address;
			});
#else
		/// <summary>
		/// Adds an <see cref="EventStoreUserManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="address"></param>
		/// <param name="createHttpMessageHandler"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStoreUserManagementClient(this IServiceCollection services,
			Uri address, Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddEventStoreUserManagementClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});
#endif

#if NETCOREAPP3_1
		/// <summary>
		/// Adds an <see cref="EventStoreUserManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="address"></param>
		/// <param name="createHttpMessageHandler"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		[Obsolete]
		public static IServiceCollection AddEventStoreUserManagementClient(this IServiceCollection services,
			// ReSharper disable once MethodOverloadWithOptionalParameter
			Uri address, Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddEventStoreUserManagementClient(options => {
				options.ConnectivitySettings.Address = address;
			});

#endif

		/// <summary>
		/// Adds an <see cref="EventStoreUserManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="connectionString"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStoreUserManagementClient(this IServiceCollection services,
			string connectionString, Action<EventStoreClientSettings>? configureSettings = null)
			=> services.AddEventStoreUserManagementClient(EventStoreClientSettings.Create(connectionString),
				configureSettings);


		/// <summary>
		/// Adds an <see cref="EventStoreUserManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddEventStoreUserManagementClient(this IServiceCollection services,
			Action<EventStoreClientSettings>? configureSettings = null) =>
			services.AddEventStoreUserManagementClient(new EventStoreClientSettings(), configureSettings);

		private static IServiceCollection AddEventStoreUserManagementClient(this IServiceCollection services,
			EventStoreClientSettings settings, Action<EventStoreClientSettings>? configureSettings = null) {
			configureSettings?.Invoke(settings);
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			services.TryAddSingleton(provider => {
				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new EventStoreUserManagementClient(settings);
			});

			return services;
		}
	}
}
// ReSharper restore CheckNamespace
