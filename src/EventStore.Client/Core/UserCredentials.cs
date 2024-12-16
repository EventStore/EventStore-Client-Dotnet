using System.Net.Http.Headers;
using System.Text;
using static System.Convert;

namespace EventStore.Client {
	/// <summary>
	/// Represents either a username/password pair or a JWT token used for authentication and
	/// authorization to perform operations on the EventStoreDB.
	/// </summary>
	public class UserCredentials {
		// ReSharper disable once InconsistentNaming
		static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(false);

		/// <summary>
		/// Constructs a new <see cref="UserCredentials"/>.
		/// </summary>
		public UserCredentials(string username, string password) {
			Username = username;
			Password = password;

			Authorization = new(
				Constants.Headers.BasicScheme,
				ToBase64String(UTF8NoBom.GetBytes($"{username}:{password}"))
			);
		}

		/// <summary>
		/// Constructs a new <see cref="UserCredentials"/>.
		/// </summary>
		public UserCredentials(string bearerToken) {
			Authorization = new(Constants.Headers.BearerScheme, bearerToken);
		}

		AuthenticationHeaderValue Authorization { get; }

		/// <summary>
		/// The username
		/// </summary>
		public string? Username { get; }

		/// <summary>
		/// The password
		/// </summary>
		public string? Password { get; }

		/// <inheritdoc />
		public override string ToString() => Authorization.ToString();

		/// <summary>
		/// Implicitly convert a <see cref="UserCredentials"/> to a <see cref="string"/>.
		/// </summary>
		public static implicit operator string(UserCredentials self) => self.ToString();
	}
}