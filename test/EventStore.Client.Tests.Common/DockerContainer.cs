using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Polly;
using Serilog;

#nullable enable
namespace EventStore.Client {
	internal class DockerContainer : IAsyncDisposable {
		private const string UnixPipe = "unix:///var/run/docker.sock";
		private const string WindowsPipe = "npipe://./pipe/docker_engine";
		private static readonly Uri DockerUri = new Uri(Environment.OSVersion.IsWindows() ? WindowsPipe : UnixPipe);

		private static readonly DockerClientConfiguration DockerClientConfiguration =
			new DockerClientConfiguration(DockerUri);

		private static readonly ILogger Log = Serilog.Log.ForContext<DockerContainer>();

		private readonly IDictionary<int, int> _ports;
		private readonly string _image;
		private readonly string _tag;
		private readonly Func<CancellationToken, Task<bool>> _healthCheck;
		private readonly IDockerClient _dockerClient;

		private string ImageWithTag => $"{_image}:{_tag}";
		private string? _containerId;

		public DockerContainer(
			string image,
			string tag,
			Func<CancellationToken, Task<bool>> healthCheck,
			IDictionary<int, int> ports) {
			_dockerClient = DockerClientConfiguration.CreateClient();
			_ports = ports;
			_image = image;
			_tag = tag;
			_healthCheck = healthCheck;
			Env = new Dictionary<string, string>();
			Cmd = new List<string>();
		}

		public string ContainerName { get; set; } = Guid.NewGuid().ToString("n");
		public IDictionary<string, string> Env { get; set; }
		public IList<string> Cmd { get; set; }

		private static AsyncPolicy RetryPolicy => Policy.Handle<DockerApiException>()
			.WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

		public async Task Start(CancellationToken cancellationToken = default) {
			await RetryPolicy.ExecuteAsync(DownloadImage, cancellationToken);

			await RetryPolicy.ExecuteAsync(StopExistingContainer, cancellationToken);

			_containerId = await RetryPolicy.ExecuteAsync(CreateContainer, cancellationToken);

			await RetryPolicy.ExecuteAsync(StartContainer, cancellationToken);
		}

		private async Task DownloadImage(CancellationToken cancellationToken) {
			var images = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters {
				MatchName = ImageWithTag
			}, cancellationToken);

			if (images.Count == 0) {
				Log.Warning("Found 0 images matching {image}:{tag}. Downloading...", images.Count, _image, _tag);
				// No image found. Pulling latest ..
				var imagesCreateParameters = new ImagesCreateParameters {
					FromImage = _image,
					Tag = _tag,
				};

				await _dockerClient
					.Images
					.CreateImageAsync(imagesCreateParameters, null, IgnoreProgress.Forever, cancellationToken);
			} else {
				Log.Information("Found {count} images matching {image}:{tag}", images.Count, _image, _tag);
			}
		}

		private async Task StopExistingContainer(CancellationToken cancellationToken) {
			var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters {
				All = true
			}, cancellationToken);

			var eventStoreContainer = containers.FirstOrDefault(c => c.Names.Contains($"/{ContainerName}"));
			if (eventStoreContainer != null) {
				try {
					await _dockerClient.Containers.StopContainerAsync(eventStoreContainer.ID,
						new ContainerStopParameters {WaitBeforeKillSeconds = 5}, cancellationToken);
				} catch (DockerContainerNotFoundException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
					Log.Warning("Tried to stop container {containerId} but it was not found.", eventStoreContainer.ID);
				}
			}
		}

		private async Task<string> CreateContainer(CancellationToken cancellationToken) {
			var container = await _dockerClient.Containers.CreateContainerAsync(
				new CreateContainerParameters {
					Image = ImageWithTag,
					Name = ContainerName,
					Tty = false,
					Env = Env.Select(pair => $"{pair.Key}={pair.Value}").ToArray(),
					HostConfig = new HostConfig {
						PortBindings = _ports.ToDictionary(
							pair => $"{pair.Key}/tcp",
							pair => (IList<PortBinding>)new List<PortBinding> {
								new PortBinding {
									HostPort = pair.Value.ToString()
								}
							}),
						AutoRemove = true,
					},
					Cmd = Cmd
				},
				cancellationToken);
			return container.ID;
		}

		private async Task StartContainer(CancellationToken cancellationToken) {
			// Starting the container ...
			await _dockerClient.Containers.StartContainerAsync(
				_containerId,
				new ContainerStartParameters(),
				cancellationToken);
			while (!await _healthCheck(cancellationToken)) {
				await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
			}
		}

		private class IgnoreProgress : IProgress<JSONMessage> {
			public static readonly IProgress<JSONMessage> Forever = new IgnoreProgress();

			public void Report(JSONMessage value) => Log.Information("Downloading... {value}", value);
		}

		public ValueTask DisposeAsync() =>
			_containerId == null
				? new ValueTask(Task.CompletedTask)
				: new ValueTask(RetryPolicy.ExecuteAndCaptureAsync(() => _dockerClient.Containers.StopContainerAsync(
					_containerId,
					new ContainerStopParameters {
						WaitBeforeKillSeconds = 1
					})));
	}
}
