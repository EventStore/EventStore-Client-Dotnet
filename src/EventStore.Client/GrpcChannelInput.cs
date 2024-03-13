namespace EventStore.Client {
	internal record GrpcChannelInput(
		ReconnectionRequired ReconnectionRequired,
		UserCredentials? UserCredentials = null
	) {
		public virtual bool Equals(GrpcChannelInput? other) {
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ReconnectionRequired.Equals(other.ReconnectionRequired) &&
			       Equals(UserCredentials, other.UserCredentials);
		}

		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = hash * 23 + ReconnectionRequired.GetHashCode();
				hash = hash * 23 + (UserCredentials?.GetHashCode() ?? 0);
				return hash;
			}
		}
	}
}
