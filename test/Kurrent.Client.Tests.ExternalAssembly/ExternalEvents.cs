namespace Kurrent.Client.Tests.ExternalAssembly;

/// <summary>
/// External event class used for testing loaded assembly resolution
/// This assembly will be explicitly loaded during tests
/// </summary>
public class ExternalEvent {
	public string Id   { get; set; } = null!;
	public string Name { get; set; } = null!;
}
