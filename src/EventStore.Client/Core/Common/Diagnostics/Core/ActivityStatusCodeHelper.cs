// ReSharper disable CheckNamespace

using System.Diagnostics;
using System.Runtime.CompilerServices;

using static System.Diagnostics.ActivityStatusCode;
using static System.StringComparison;

namespace EventStore.Diagnostics;

static class ActivityStatusCodeHelper {
    public const string UnsetStatusCodeTagValue = "UNSET";
    public const string OkStatusCodeTagValue    = "OK";
    public const string ErrorStatusCodeTagValue = "ERROR";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetTagValueForStatusCode(ActivityStatusCode statusCode) =>
        statusCode switch {
            Unset => UnsetStatusCodeTagValue,
            Error => ErrorStatusCodeTagValue,
            Ok    => OkStatusCodeTagValue,
            _     => null
        };
}