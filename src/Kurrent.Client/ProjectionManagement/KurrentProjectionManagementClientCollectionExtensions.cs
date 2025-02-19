// ReSharper disable CheckNamespace

using System;
using System.Net.Http;
using EventStore.Client;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection {
	/// <summary>
	/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="KurrentProjectionManagementClient"/>.
	/// </summary>
	public static class KurrentProjectionManagementClientCollectionExtensions {
		/// <summary>
		/// Adds an <see cref="KurrentProjectionManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="address"></param>
		/// <param name="createHttpMessageHandler"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentProjectionManagementClient(this IServiceCollection services,
			Uri address,
			Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddKurrentProjectionManagementClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		/// <summary>
		/// Adds an <see cref="KurrentProjectionManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentProjectionManagementClient(this IServiceCollection services,
			Action<KurrentClientSettings>? configureSettings = null) =>
			services.AddKurrentProjectionManagementClient(new KurrentClientSettings(), configureSettings);

		/// <summary>
		/// Adds an <see cref="KurrentProjectionManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="connectionString"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentProjectionManagementClient(this IServiceCollection services,
			string connectionString, Action<KurrentClientSettings>? configureSettings = null) =>
			services.AddKurrentProjectionManagementClient(KurrentClientSettings.Create(connectionString),
				configureSettings);

		private static IServiceCollection AddKurrentProjectionManagementClient(this IServiceCollection services,
			KurrentClientSettings settings, Action<KurrentClientSettings>? configureSettings) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			configureSettings?.Invoke(settings);

			services.TryAddSingleton(provider => {
				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentProjectionManagementClient(settings);
			});

			return services;
		}
	}
}
// ReSharper restore CheckNamespace
