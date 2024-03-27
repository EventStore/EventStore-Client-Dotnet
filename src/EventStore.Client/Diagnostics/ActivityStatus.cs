using System.Diagnostics;

namespace EventStore.Client.Diagnostics;

record ActivityStatus(ActivityStatusCode StatusCode, string? Description, Exception? Exception) {
	public static ActivityStatus Ok(string? description = null)
		=> new(ActivityStatusCode.Ok, description, null);

	public static ActivityStatus Error(Exception ex, string? description = null)
		=> new(ActivityStatusCode.Error, description, ex);
}
