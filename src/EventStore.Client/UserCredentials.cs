using System;
using System.Net.Http.Headers;
using System.Text;

#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// Represents either a username/password pair or a JWT token used for authentication and
	/// authorization to perform operations on the EventStoreDB.
	/// </summary>
	public class UserCredentials {
		/// <summary>
		/// The username
		/// </summary>
		public string? Username => TryGetBasicAuth(0, out var value) ? value : null;
		/// <summary>
		/// The password
		/// </summary>
		public string? Password => TryGetBasicAuth(1, out var value) ? value : null;

		private readonly AuthenticationHeaderValue _authorization;

		/// <summary>
		/// Constructs a new <see cref="UserCredentials"/>.
		/// </summary>
		/// <param name="username"></param>
		/// <param name="password"></param>
		public UserCredentials(string username, string password) : this(new AuthenticationHeaderValue(
			Constants.Headers.BasicScheme,
			Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")))) {
		}

		/// <summary>
		/// Constructs a new <see cref="UserCredentials"/>.
		/// </summary>
		/// <param name="authToken"></param>
		public UserCredentials(string authToken) : this(new AuthenticationHeaderValue(Constants.Headers.BearerScheme,
			authToken)) {
		}

		private UserCredentials(AuthenticationHeaderValue authorization) => _authorization = authorization;

		private bool TryGetBasicAuth(int index, out string? value) {
			value = null;

			if (_authorization.Scheme != Constants.Headers.BasicScheme) {
				return false;
			}

			var parts = Encoding.ASCII.GetString(Convert.FromBase64String(_authorization.Parameter)).Split(':');
			if (parts.Length <= index) {
				return false;
			}

			value = parts[index];
			return true;
		}

		/// <inheritdoc />
		public override string ToString() => _authorization.ToString();
	}
}
