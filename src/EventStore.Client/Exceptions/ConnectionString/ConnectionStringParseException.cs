using System;

namespace EventStore.Client {
	public class ConnectionStringParseException : Exception {
		public ConnectionStringParseException(string message) : base(message) { }
	}
}
