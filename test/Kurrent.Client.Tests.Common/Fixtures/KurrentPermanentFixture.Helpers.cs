using System.Runtime.CompilerServices;
using System.Text;
using EventStore.Client;
using Kurrent.Client.Core.Serialization;

namespace Kurrent.Client.Tests;

public partial class KurrentPermanentFixture {
	public const string TestEventType              = "test-event-type";
	public const string AnotherTestEventTypePrefix = "another";
	public const string AnotherTestEventType       = $"{AnotherTestEventTypePrefix}-test-event-type";

	public T NewClient<T>(Action<KurrentClientSettings> configure) where T : KurrentClientBase, new() =>
		(T)Activator.CreateInstance(typeof(T), [ClientSettings.With(configure)])!;

	public string GetStreamName([CallerMemberName] string? testMethod = null) =>
		$"stream-{testMethod}-{Guid.NewGuid():N}";

	public string GetGroupName([CallerMemberName] string? testMethod = null) =>
		$"group-{testMethod}-{Guid.NewGuid():N}";

	public UserCredentials GetUserCredentials([CallerMemberName] string? testMethod = null) => new UserCredentials(
		$"user-{testMethod}-{Guid.NewGuid():N}", "pa$$word"
	);

	public string GetProjectionName([CallerMemberName] string? testMethod = null) =>
		$"projection-{testMethod}-{Guid.NewGuid():N}";

	public ReadOnlyMemory<byte> CreateMetadataOfSize(int metadataSize) =>
		Encoding.UTF8.GetBytes($"\"{new string('$', metadataSize)}\"");

	public ReadOnlyMemory<byte> CreateTestJsonMetadata() => "{\"Foo\": \"Bar\"}"u8.ToArray();

	public ReadOnlyMemory<byte> CreateTestNonJsonMetadata() => "non-json-metadata"u8.ToArray();

	public (IEnumerable<EventData> Events, uint size) CreateTestEventsUpToMaxSize(uint maxSize) {
		var size = 0;

		var events = CreateTestEvents(int.MaxValue)
			.TakeWhile(evt => (size += evt.Data.Length) < maxSize)
			.ToList();

		return (events, (uint)size);
	}

	public IEnumerable<EventData> CreateTestEvents(
		int count = 1, string? type = null, ReadOnlyMemory<byte>? metadata = null, string? contentType = null
	) =>
		Enumerable.Range(0, count)
			.Select(index => CreateTestEvent(index, type ?? TestEventType, metadata, contentType));


	public IEnumerable<Message> CreateTestMessages(int count = 1, object? metadata = null) =>
		Enumerable.Range(0, count)
			.Select(index => CreateTestMessage(index, metadata));

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

	protected static Message CreateTestMessage(int index, object? metadata = null) =>
		Message.From(
			new DummyEvent(index),
			metadata,
			Uuid.NewUuid()
		);
	
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

	public record DummyEvent(int X);
}
