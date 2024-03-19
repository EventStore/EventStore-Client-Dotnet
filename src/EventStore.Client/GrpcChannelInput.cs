using System.Security.Cryptography.X509Certificates;

namespace EventStore.Client {
	internal record GrpcChannelInput(
		ReconnectionRequired ReconnectionRequired,
		X509Certificate2? UserCertificate = null
	) {
		public virtual bool Equals(GrpcChannelInput? other) {
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ReconnectionRequired.Equals(other.ReconnectionRequired) &&
			       Equals(UserCertificate, other.UserCertificate);
		}

		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = hash * 23 + ReconnectionRequired.GetHashCode();
				hash = hash * 23 + (UserCertificate?.GetHashCode() ?? 0);
				return hash;
			}
		}
	}
}
