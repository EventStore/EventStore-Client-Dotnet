using System.Diagnostics;
using EventStore.Client;
using EventStore.Client.Extensions.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

#pragma warning disable CS8321 // Local function is declared but never used
#pragma warning disable CS1587 // XML comment is not placed on a valid language element

/**
# region import-required-packages
// required
dotnet add package EventStore.Client.Extensions.OpenTelemetry

// recommended
dotnet add package OpenTelemetry.Exporter.Jaeger
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry
dotnet add package Microsoft.Extensions.Hosting
dotnet add package OpenTelemetry.Extensions.Hosting
# endregion import-required-packages
**/

var settings = KurrentClientSettings.Create("esdb://localhost:2113?tls=false");

settings.OperationOptions.ThrowOnAppendFailure = false;

await using var client = new KurrentClient(settings);

await TraceAppendToStream(client);

return;

static async Task TraceAppendToStream(KurrentClient client) {
	const string serviceName = "sample";

	var host = Host.CreateDefaultBuilder()
		.ConfigureServices(
			(_, services) => {
				services.AddSingleton(new ActivitySource(serviceName));
				services
					.AddOpenTelemetry()
					.ConfigureResource(builder => builder.AddService(serviceName))
					.WithTracing(ConfigureTracerProviderBuilder);
			}
		)
		.Build();

	using (host) {
		# region setup-client-for-tracing

		host.Start();

		var eventData = new EventData(
			Uuid.NewUuid(),
			"some-event",
			"{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray()
		);

		await client.AppendToStreamAsync(
			Uuid.NewUuid().ToString(),
			StreamState.Any,
			new List<EventData> {
				eventData
			}
		);

		# endregion setup-client-for-tracing
	}

	return;

	static void ConfigureTracerProviderBuilder(TracerProviderBuilder tracerProviderBuilder) {
		#region register-instrumentation

		tracerProviderBuilder
			.AddKurrentClientInstrumentation();

		#endregion register-instrumentation

		#region setup-exporter

		tracerProviderBuilder
			.AddConsoleExporter()
			.AddJaegerExporter(
				options => {
					options.Endpoint = new Uri("http://localhost:4317");
					options.Protocol = JaegerExportProtocol.UdpCompactThrift;
				}
			);

		#endregion setup-exporter
	}
}
