namespace EventStore.Client.Tests;

public static class TestCertificate {
	public static readonly UserCertificate UserAdminCertificate = new(
		Path.Combine(Environment.CurrentDirectory, "certs", "user-admin", "user-admin.crt"),
		Path.Combine(Environment.CurrentDirectory, "certs", "user-admin", "user-admin.key")
	);
	public static readonly UserCertificate BadUserCertificate = new(
		Path.Combine(Environment.CurrentDirectory, "certs", "user-invalid", "user-invalid.crt"),
		Path.Combine(Environment.CurrentDirectory, "certs", "user-invalid", "user-invalid.key")
	);
}
