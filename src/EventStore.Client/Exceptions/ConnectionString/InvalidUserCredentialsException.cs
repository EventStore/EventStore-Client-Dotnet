namespace EventStore.Client {
	public class InvalidUserCredentialsException : ConnectionStringParseException {
		public InvalidUserCredentialsException(string userInfo)
			: base($"Invalid user credentials: '{userInfo}'. Username & password must be delimited by a colon") { }
	}
}
