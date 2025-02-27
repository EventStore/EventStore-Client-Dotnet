namespace Kurrent.Client.Tests.NeverLoadedAssembly;

/// <summary>
/// External event class used for testing unloaded assembly resolution
/// This event should never be referenced directly by the test project
/// </summary>
public record NotLoadedExternalEvent {
	public string Id   { get; set; } = null!;
	public string Name { get; set; } = null!;
}
