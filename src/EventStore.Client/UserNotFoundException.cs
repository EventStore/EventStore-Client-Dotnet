using System;

#nullable enable
namespace EventStore.Client {
	public class UserNotFoundException : Exception {
		public string LoginName { get; }

		public UserNotFoundException(string loginName, Exception? exception = null)
			: base($"User '{loginName}' was not found.", exception) {
			LoginName = loginName;
		}
	}
}
