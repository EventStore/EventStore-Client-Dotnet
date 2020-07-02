namespace EventStore.Client {
	public class InvalidSettingException : ConnectionStringParseException {
		public InvalidSettingException(string message) : base(message) { }
	}
}
