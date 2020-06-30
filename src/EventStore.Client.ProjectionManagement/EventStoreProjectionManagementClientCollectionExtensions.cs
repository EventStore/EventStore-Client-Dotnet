// ReSharper disable CheckNamespace

using System;
using System.Net.Http;
using EventStore.Client;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

#nullable enable
namespace Microsoft.Extensions.DependencyInjection {
	public static class EventStoreProjectionManagementClientCollectionExtensions {
		public static IServiceCollection AddEventStoreProjectionManagementClient(this IServiceCollection services, Uri address,
			Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddEventStoreProjectionManagementClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		public static IServiceCollection AddEventStoreProjectionManagementClient(this IServiceCollection services,
			Action<EventStoreClientSettings>? configureSettings = null) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			var settings = new EventStoreClientSettings();
			configureSettings?.Invoke(settings);

			services.TryAddSingleton(provider => {
				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new EventStoreProjectionManagementClient(settings);
			});

			return services;
		}
	}
}
// ReSharper restore CheckNamespace
