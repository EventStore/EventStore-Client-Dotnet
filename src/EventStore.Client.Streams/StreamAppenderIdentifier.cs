using System.Security.Cryptography.X509Certificates;

namespace EventStore.Client;

internal class StreamAppenderIdentifier(X509Certificate2? userCertificate) : IEquatable<StreamAppenderIdentifier> {
	X509Certificate2? UserCertificate { get; } = userCertificate;

	public bool Equals(StreamAppenderIdentifier? other) {
		if (other == null)
			return false;

		if (UserCertificate == null && other.UserCertificate == null)
			return true;

		if (UserCertificate == null || other.UserCertificate == null)
			return false;

		return UserCertificate.Equals(other.UserCertificate);
	}

	public override bool Equals(object? obj) {
		return Equals(obj as StreamAppenderIdentifier);
	}

	public override int GetHashCode() {
		return UserCertificate?.GetHashCode() ?? 0;
	}
}
