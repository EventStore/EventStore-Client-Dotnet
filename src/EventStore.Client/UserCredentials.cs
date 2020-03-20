using System;

#nullable enable
namespace EventStore.Client {
	public class UserCredentials {
		public readonly string Username;
		public readonly string Password;

		public UserCredentials(string username, string password) {
			Username = username;
			Password = password;
		}
	}
}
