using System;

namespace EventStore.Client;

/// <summary>
/// 
/// </summary>
public readonly struct Operation {
	/// <summary>
	/// 
	/// </summary>
	public string Resource { init; get; }
	/// <summary>
	/// 
	/// </summary>
	public string Action { init; get; }
	/// <summary>
	/// 
	/// </summary>
	public ReadOnlyMemory<Parameter> Parameters { init; get; }
}
