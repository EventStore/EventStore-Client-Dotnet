// ReSharper disable CheckNamespace

namespace Kurrent.Diagnostics.Telemetry;

// The attributes below match the specification of v1.24.0 of the Open Telemetry semantic conventions.
// Some attributes are ignored where not required or relevant.
// https://github.com/open-telemetry/semantic-conventions/blob/v1.24.0/docs/general/trace.md
// https://github.com/open-telemetry/semantic-conventions/blob/v1.24.0/docs/database/database-spans.md
// https://github.com/open-telemetry/semantic-conventions/blob/v1.24.0/docs/exceptions/exceptions-spans.md

static partial class TelemetryTags {
    public static class Database {
        public const string User      = "db.user";
        public const string System    = "db.system";
        public const string Operation = "db.operation";
    }
    
    public static class Server {
        public const string Address       = "server.address";
        public const string Port          = "server.port";
        public const string SocketAddress = "server.socket.address"; // replaces: "net.peer.ip" (AttributeNetPeerIp)
    }
    
    public static class Exception {
        public const string EventName  = "exception";
        public const string Type       = "exception.type";
        public const string Message    = "exception.message";
        public const string Stacktrace = "exception.stacktrace";
    }

    public static class Otel {
        public const string StatusCode        = "otel.status_code";
        public const string StatusDescription = "otel.status_description";
    }
}
