using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Services;
using Polly;

#nullable enable
namespace EventStore.Client {
	public class EventStoreTestServerCluster : IEventStoreTestServer {
		private readonly ICompositeService _eventStoreCluster;
		private readonly HttpClient _httpClient;

		public EventStoreTestServerCluster(
			string hostCertificatePath,
			Uri address,
			IDictionary<string, string>? envOverrides) {

			envOverrides ??= new Dictionary<string, string>();
			envOverrides["ES_CERTS_CLUSTER"] = hostCertificatePath;

			_eventStoreCluster = BuildCluster(envOverrides);

			_httpClient = new HttpClient(
#if NETFRAMEWORK
					new HttpClientHandler {
						ServerCertificateCustomValidationCallback = delegate { return true; }
					}
#else
					new SocketsHttpHandler {
						SslOptions = {
							RemoteCertificateValidationCallback = delegate { return true; }
						}
					}
#endif
				) {
				BaseAddress = address,
			};
		}

		private ICompositeService BuildCluster(IDictionary<string, string>? envOverrides = null) {
			var env = GlobalEnvironment.EnvironmentVariables(envOverrides);
			return new Builder()
				.UseContainer()
				.UseCompose()
				.WithEnvironment(env.Select(pair => $"{pair.Key}={pair.Value}").ToArray())
				.FromFile("docker-compose.yml")
				.ForceRecreate()
				.RemoveOrphans()
				.Build();
		}

		public async Task StartAsync(CancellationToken cancellationToken = default) {
			try {
				// don't know why, sometimes the default network (e.g. net50_default) remains
				// from previous cluster and prevents docker-compose up from executing successfully
				Policy.Handle<FluentDockerException>()
					.WaitAndRetry(
						retryCount: 10,
						sleepDurationProvider: retryCount => TimeSpan.FromSeconds(2),
						onRetry: (ex, _) => {
							BuildCluster().Dispose();
							_eventStoreCluster.Start();
						})
					.Execute(() => {
						_eventStoreCluster.Start();
					});

				await Policy.Handle<Exception>()
					.WaitAndRetryAsync(200, retryCount => TimeSpan.FromMilliseconds(100))
					.ExecuteAsync(async () => {
						using var response = await _httpClient.GetAsync("/health/live", cancellationToken);
						if (response.StatusCode >= HttpStatusCode.BadRequest) {
							throw new Exception($"Health check failed with status code: {response.StatusCode}.");
						}
					});
			} catch (Exception) {
				_eventStoreCluster.Dispose();
				throw;
			}
		}

		public void Stop() {
			_eventStoreCluster.Stop();
		}

		public ValueTask DisposeAsync() {
			_eventStoreCluster.Dispose();
			return new ValueTask(Task.CompletedTask);
		}
	}
}
