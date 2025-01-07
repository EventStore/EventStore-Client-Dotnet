// ReSharper disable CheckNamespace

using System;
using System.Net.Http;
using EventStore.Client;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection {
	/// <summary>
	/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="KurrentPersistentSubscriptionsClient"/>.
	/// </summary>
	public static class KurrentPersistentSubscriptionsClientCollectionExtensions {
		/// <summary>
		/// Adds an <see cref="KurrentPersistentSubscriptionsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentPersistentSubscriptionsClient(this IServiceCollection services,
			Uri address, Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddKurrentPersistentSubscriptionsClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		/// <summary>
		/// Adds an <see cref="KurrentPersistentSubscriptionsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentPersistentSubscriptionsClient(this IServiceCollection services,
			Action<KurrentClientSettings>? configureSettings = null) =>
			services.AddKurrentPersistentSubscriptionsClient(new KurrentClientSettings(),
				configureSettings);

		/// <summary>
		/// Adds an <see cref="KurrentPersistentSubscriptionsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentPersistentSubscriptionsClient(this IServiceCollection services,
			string connectionString, Action<KurrentClientSettings>? configureSettings = null) =>
			services.AddKurrentPersistentSubscriptionsClient(KurrentClientSettings.Create(connectionString),
				configureSettings);

		private static IServiceCollection AddKurrentPersistentSubscriptionsClient(this IServiceCollection services,
			KurrentClientSettings settings, Action<KurrentClientSettings>? configureSettings) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			configureSettings?.Invoke(settings);
			services.TryAddSingleton(provider => {
				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentPersistentSubscriptionsClient(settings);
			});
			return services;
		}
	}
}
// ReSharper restore CheckNamespace
