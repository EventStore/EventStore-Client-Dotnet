// ReSharper disable CheckNamespace

using System.Diagnostics;

namespace Kurrent.Diagnostics;

record ActivityStatus(ActivityStatusCode StatusCode, string? Description, Exception? Exception) {
	public static ActivityStatus Ok(string? description = null) =>
        new(ActivityStatusCode.Ok, description, null);

	public static ActivityStatus Error(Exception exception, string? description = null) =>
        new(ActivityStatusCode.Error, description ?? exception.Message, exception);
}
