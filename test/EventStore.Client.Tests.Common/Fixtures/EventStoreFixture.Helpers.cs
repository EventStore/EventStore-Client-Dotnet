using System.Runtime.CompilerServices;
using System.Text;

namespace EventStore.Client.Tests;

public partial class EventStoreFixture {
	public const string TestEventType              = "test-event-type";
	public const string AnotherTestEventTypePrefix = "another";
	public const string AnotherTestEventType       = $"{AnotherTestEventTypePrefix}-test-event-type";

	public T NewClient<T>(Action<EventStoreClientSettings> configure) where T : EventStoreClientBase, new() =>
		(T)Activator.CreateInstance(typeof(T), [ClientSettings.With(configure)])!;

	public string GetStreamName([CallerMemberName] string? testMethod = null) =>
		$"{testMethod}-{Guid.NewGuid():N}";

	public ReadOnlyMemory<byte> CreateMetadataOfSize(int metadataSize) =>
		Encoding.UTF8.GetBytes($"\"{new string('$', metadataSize)}\"");

	public ReadOnlyMemory<byte> CreateTestJsonMetadata() => "{\"Foo\": \"Bar\"}"u8.ToArray();

	public ReadOnlyMemory<byte> CreateTestNonJsonMetadata() => "non-json-metadata"u8.ToArray();

	public IEnumerable<EventData> CreateTestEvents(
		int count = 1, string? type = null, ReadOnlyMemory<byte>? metadata = null, string? contentType = null
	) =>
		Enumerable.Range(0, count)
			.Select(index => CreateTestEvent(index, type ?? TestEventType, metadata, contentType));

	public EventData CreateTestEvent(
		string? type = null, ReadOnlyMemory<byte>? metadata = null, string? contentType = null
	) =>
		CreateTestEvent(0, type ?? TestEventType, metadata, contentType);

	public IEnumerable<EventData> CreateTestEventsThatThrowsException() {
		// Ensure initial IEnumerator.Current does not throw
		yield return CreateTestEvent(1);

		// Throw after enumerator advances
		throw new Exception();
	}

	protected static EventData CreateTestEvent(int index) => CreateTestEvent(index, TestEventType);

	protected static EventData CreateTestEvent(
		int index, string type, ReadOnlyMemory<byte>? metadata = null, string? contentType = null
	) =>
		new(
			Uuid.NewUuid(),
			type,
			Encoding.UTF8.GetBytes($$"""{"x":{{index}}}"""),
			metadata,
			contentType ?? "application/json"
		);

	public async Task<TestUser> CreateTestUser(bool withoutGroups = true, bool useUserCredentials = false) {
		var result = await CreateTestUsers(1, withoutGroups, useUserCredentials);
		return result.First();
	}

	public Task<TestUser[]> CreateTestUsers(
		int count = 3, bool withoutGroups = true, bool useUserCredentials = false
	) =>
		Fakers.Users
			.RuleFor(x => x.Groups, f => withoutGroups ? Array.Empty<string>() : f.Lorem.Words())
			.Generate(count)
			.Select(
				async user => {
					await Users.CreateUserAsync(
						user.LoginName,
						user.FullName,
						user.Groups,
						user.Password,
						userCredentials: useUserCredentials ? user.Credentials : TestCredentials.Root
					);

					return user;
				}
			).WhenAll();

	public async Task RestartService(TimeSpan delay) {
		await Service.Restart(delay);
		await Streams.WarmUp();
		Log.Information("Service restarted.");
	}
}
