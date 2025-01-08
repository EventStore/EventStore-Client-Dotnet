using EventStore.Client.Diagnostics;
using JetBrains.Annotations;
using OpenTelemetry.Trace;

namespace EventStore.Client.Extensions.OpenTelemetry;

/// <summary>
/// Extension methods used to facilitate tracing instrumentation of the EventStore Client.
/// </summary>
[PublicAPI]
public static class TracerProviderBuilderExtensions {
	/// <summary>
	/// Adds the EventStore client ActivitySource name to the list of subscribed sources on the <see cref="TracerProviderBuilder"/>
	/// </summary>
	/// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
	/// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain configuration.</returns>
	public static TracerProviderBuilder AddKurrentClientInstrumentation(this TracerProviderBuilder builder) =>
		builder.AddSource(KurrentClientDiagnostics.InstrumentationName);
}
