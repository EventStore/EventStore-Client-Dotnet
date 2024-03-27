using JetBrains.Annotations;
using OpenTelemetry.Trace;

namespace EventStore.Client.Diagnostics.OpenTelemetry;

/// <summary>
/// Extension methods used to facilitate instrumentation of the EventStore .NET Client.
/// </summary>
[PublicAPI]
public static class TracerProviderBuilderExtensions {
	/// <summary>
	/// Enables instrumentation of the EventStore .NET Client on the OpenTelemetry TracerProvider.
	/// </summary>
	/// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
	/// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain configuration.</returns>
	public static TracerProviderBuilder AddEventStoreClientInstrumentation(this TracerProviderBuilder builder)
		=> builder.AddSource(EventStoreClientDiagnostics.ActivitySourceName);
}
