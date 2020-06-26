using System;

namespace EventStore.Client {
	public class ConnectionStringParseException : Exception {
		public ConnectionStringParseException(string message) : base(message) { }
	}

	public class NoSchemeException : ConnectionStringParseException {
		public NoSchemeException()
			: base("Could not parse scheme from connection string") { }
	}

	public class InvalidSchemeException : ConnectionStringParseException {
		public InvalidSchemeException(string scheme, string[] supportedSchemes)
			: base($"Invalid scheme: '{scheme}'. Supported values are: {string.Join(",", supportedSchemes)}") { }
	}

	public class InvalidUserCredentialsException : ConnectionStringParseException {
		public InvalidUserCredentialsException(string userInfo)
			: base($"Invalid user credentials: '{userInfo}'. Username & password must be delimited by a colon") { }
	}

	public class InvalidHostException : ConnectionStringParseException {
		public InvalidHostException(string host)
			: base($"Invalid host: '{host}'") { }
	}

	public class InvalidKeyValuePairException : ConnectionStringParseException {
		public InvalidKeyValuePairException(string keyValuePair)
			: base($"Invalid key/value pair: '{keyValuePair}'") { }
	}

	public class InvalidSettingException : ConnectionStringParseException {
		public InvalidSettingException(string message) : base(message) { }
	}

}
