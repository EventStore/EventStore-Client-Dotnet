// ReSharper disable CheckNamespace

using System.Globalization;

namespace Kurrent.Diagnostics;

static class ExceptionExtensions {
    /// <summary>
    /// Returns a culture-independent string representation of the given <paramref name="exception"/> object,
    /// appropriate for diagnostics tracing.
    /// </summary>
    /// <param name="exception">Exception to convert to string.</param>
    /// <returns>Exception as string with no culture.</returns>
    public static string ToInvariantString(this Exception exception) {
        var originalUiCulture = Thread.CurrentThread.CurrentUICulture;

        try {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            return exception.ToString();
        }
        finally {
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
        }
    }
}
