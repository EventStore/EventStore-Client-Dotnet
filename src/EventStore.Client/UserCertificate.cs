using System.Security.Cryptography.X509Certificates;

namespace EventStore.Client {
	/// <summary>
	/// Represents the user certificates used to authenticate and authorize operations on the EventStoreDB.
	/// </summary>
	public record UserCertificate {
		/// <summary>
		/// The user certificate
		/// </summary>
		public X509Certificate2? Certificate { get; }

		/// <summary>
		/// Constructs a new <see cref="UserCredentials"/>.
		/// </summary>
		public UserCertificate(X509Certificate2 userCertificate) {
			Certificate = userCertificate;
		}

		/// <summary>
		/// Constructs a new <see cref="UserCredentials"/>.
		/// </summary>
		public UserCertificate(string certificatePath, string privateKeyPath) {
			Certificate = CertificateUtils.LoadFromFile(
				certificatePath,
				privateKeyPath
			);
		}
	}
}
