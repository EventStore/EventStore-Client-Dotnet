using System.Net;

namespace EventStore.Client {
	/// <summary>
	/// 
	/// </summary>
	/// <param name="SupportsBatchAppend"></param>
	public record ServerCapabilities(bool SupportsBatchAppend = false);
}
