namespace EventStore.Client.Tests;

public static class TestCredentials {
	public static readonly UserCredentials Root        = new("admin", "changeit");
	public static readonly UserCredentials TestUser1   = new("user1", "pa$$1");
	public static readonly UserCredentials TestUser2   = new("user2", "pa$$2");
	public static readonly UserCredentials TestAdmin   = new("adm", "admpa$$");
	public static readonly UserCredentials TestBadUser = new("badlogin", "badpass");

	public static readonly UserCredentials UserAdminCertificate = new(
		new UserCertificate(
			Path.Combine(Environment.CurrentDirectory, "certs", "user-admin", "user-admin.crt"),
			Path.Combine(Environment.CurrentDirectory, "certs", "user-admin", "user-admin.key")
		)
	);
	public static readonly UserCredentials BadUserCertificate = new(
		new UserCertificate(
			Path.Combine(Environment.CurrentDirectory, "certs", "user-invalid", "user-invalid.crt"),
			Path.Combine(Environment.CurrentDirectory, "certs", "user-invalid", "user-invalid.key")
		)
	);
}
