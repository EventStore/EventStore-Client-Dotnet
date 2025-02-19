// ReSharper disable CheckNamespace

using System;
using System.Net.Http;
using EventStore.Client;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection {
	/// <summary>
	/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="KurrentClient"/>.
	/// </summary>
	public static class KurrentClientServiceCollectionExtensions {
		/// <summary>
		/// Adds an <see cref="KurrentClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="address"></param>
		/// <param name="createHttpMessageHandler"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentClient(this IServiceCollection services, Uri address,
			Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddKurrentClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		/// <summary>
		/// Adds an <see cref="KurrentClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="addressFactory"></param>
		/// <param name="createHttpMessageHandler"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentClient(this IServiceCollection services,
			Func<IServiceProvider, Uri> addressFactory,
			Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddKurrentClient(provider => options => {
				options.ConnectivitySettings.Address = addressFactory(provider);
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		/// <summary>
		/// Adds an <see cref="KurrentClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentClient(this IServiceCollection services,
			Action<KurrentClientSettings>? configureSettings = null) =>
			services.AddKurrentClient(new KurrentClientSettings(), configureSettings);

		/// <summary>
		/// Adds an <see cref="KurrentClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentClient(this IServiceCollection services,
			Func<IServiceProvider, Action<KurrentClientSettings>> configureSettings) =>
			services.AddKurrentClient(new KurrentClientSettings(),
				configureSettings);

		/// <summary>
		/// Adds an <see cref="KurrentClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="connectionString"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentClient(this IServiceCollection services,
			string connectionString, Action<KurrentClientSettings>? configureSettings = null) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			return services.AddKurrentClient(KurrentClientSettings.Create(connectionString), configureSettings);
		}

		/// <summary>
		/// Adds an <see cref="KurrentClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="connectionStringFactory"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentClient(this IServiceCollection services,
			Func<IServiceProvider, string> connectionStringFactory,
			Action<KurrentClientSettings>? configureSettings = null) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			return services.AddKurrentClient(provider => KurrentClientSettings.Create(connectionStringFactory(provider)), configureSettings);
		}

		private static IServiceCollection AddKurrentClient(this IServiceCollection services,
			KurrentClientSettings settings,
			Action<KurrentClientSettings>? configureSettings) {
			configureSettings?.Invoke(settings);

			services.TryAddSingleton(provider => {
				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentClient(settings);
			});

			return services;
		}

		private static IServiceCollection AddKurrentClient(this IServiceCollection services,
			Func<IServiceProvider, KurrentClientSettings> settingsFactory,
			Action<KurrentClientSettings>? configureSettings = null) {

			services.TryAddSingleton(provider => {
				var settings = settingsFactory(provider);
				configureSettings?.Invoke(settings);

				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentClient(settings);
			});

			return services;
		}

		private static IServiceCollection AddKurrentClient(this IServiceCollection services,
			KurrentClientSettings settings,
			Func<IServiceProvider, Action<KurrentClientSettings>> configureSettingsFactory) {

			services.TryAddSingleton(provider => {
				configureSettingsFactory(provider).Invoke(settings);

				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentClient(settings);
			});

			return services;
		}
	}
}
// ReSharper restore CheckNamespace
