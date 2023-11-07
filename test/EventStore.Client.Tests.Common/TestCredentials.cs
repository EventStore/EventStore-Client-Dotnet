namespace EventStore.Client.Tests;

public static class TestCredentials {
	public static readonly UserCredentials Root        = new("admin", "changeit");
	public static readonly UserCredentials TestUser1   = new("user1", "pa$$1");
	public static readonly UserCredentials TestUser2   = new("user2", "pa$$2");
	public static readonly UserCredentials TestAdmin   = new("adm", "admpa$$");
	public static readonly UserCredentials TestBadUser = new("badlogin", "badpass");
}