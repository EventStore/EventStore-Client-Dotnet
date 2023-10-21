using System;
using System.Net.Http.Headers;
using System.Text;
using static System.Convert;

namespace EventStore.Client {
    // /// <summary>
    // /// Represents either a username/password pair or a JWT token used for authentication and
    // /// authorization to perform operations on the EventStoreDB.
    // /// </summary>
    // public class UserCredentials2 {
    //        // ReSharper disable once InconsistentNaming
    //        static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(false);
    //        
    // 	/// <summary>
    // 	/// The username
    // 	/// </summary>
    // 	public string? Username => TryGetBasicAuth(0, out var value) ? value : null;
    // 	/// <summary>
    // 	/// The password
    // 	/// </summary>
    // 	public string? Password => TryGetBasicAuth(1, out var value) ? value : null;
    //
    // 	private readonly AuthenticationHeaderValue _authorization;
    //
    // 	/// <summary>
    // 	/// Constructs a new <see cref="UserCredentials"/>.
    // 	/// </summary>
    // 	/// <param name="username"></param>
    // 	/// <param name="password"></param>
    //        public UserCredentials(string username, string password) : this(
    //            new AuthenticationHeaderValue(
    //                Constants.Headers.BasicScheme,
    //                ToBase64String(UTF8NoBom.GetBytes($"{username}:{password}"))
    //            )
    //        ) { }
    //
    // 	/// <summary>
    // 	/// Constructs a new <see cref="UserCredentials"/>.
    // 	/// </summary>
    // 	/// <param name="authToken"></param>
    // 	public UserCredentials(string authToken) : this(new AuthenticationHeaderValue(Constants.Headers.BearerScheme, authToken)) { }
    //
    // 	private UserCredentials(AuthenticationHeaderValue authorization) => _authorization = authorization;
    //
    // 	private bool TryGetBasicAuth(int index, out string? value) {
    // 		value = null;
    //
    // 		if (_authorization.Scheme != Constants.Headers.BasicScheme) {
    // 			return false;
    // 		}
    //
    // 		if (_authorization.Parameter == null) {
    // 			return false;
    // 		}
    //
    // 		var parts = UTF8NoBom.GetString(FromBase64String(_authorization.Parameter)).Split(':');
    // 		if (parts.Length <= index) {
    // 			return false;
    // 		}
    //
    // 		value = parts[index];
    // 		return true;
    // 	}
    //
    // 	/// <inheritdoc />
    // 	public override string ToString() => _authorization.ToString();
    // }

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

        /// <summary>
        /// Constructs a new <see cref="UserCredentials"/>.
        /// </summary>
        public UserCredentials(AuthenticationHeaderValue authorization) {
            Authorization = authorization;

            if (authorization.Scheme != Constants.Headers.BasicScheme)
                return;

            var (username, password) = DecodeBasicCredentials(Authorization);

            Username = username;
            Password = password;
            
            return;

            static (string? Username, string? Password) DecodeBasicCredentials(AuthenticationHeaderValue value) {
                if (value.Parameter is null)
                    return (null, null);

                var credentials = UTF8NoBom.GetString(FromBase64String(value.Parameter)).AsSpan();
                
                var passwordStart = credentials.IndexOf(':') + 1;
                var password      = credentials[passwordStart..].ToString();
                
                var usernameLength = credentials.Length - password.Length - 1;
                var username       = credentials[..usernameLength].ToString();

                return (username, password);

                // var decoded = UTF8NoBom.GetString(FromBase64String(header.Parameter));
                // var parts   = decoded.Split(':');
                //
                // return parts.Length == 2
                //     ? (parts[0], parts[1])
                //     : (null, null); // should we throw?
            }
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