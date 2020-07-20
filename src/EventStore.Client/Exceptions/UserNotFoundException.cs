using System;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// The exception that is thrown when an operation is performed on an internal user that does not exist.
	/// </summary>
	public class UserNotFoundException : Exception {
		/// <summary>
		/// The login name of the user.
		/// </summary>
		public string LoginName { get; }

		/// <summary>
		/// Constructs a new <see cref="UserNotFoundException"/>.
		/// </summary>
		/// <param name="loginName"></param>
		/// <param name="exception"></param>
		public UserNotFoundException(string loginName, Exception? exception = null)
			: base($"User '{loginName}' was not found.", exception) {
			LoginName = loginName;
		}
	}
}
