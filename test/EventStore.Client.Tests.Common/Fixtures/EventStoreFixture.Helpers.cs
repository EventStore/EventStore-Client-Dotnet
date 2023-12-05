using System.Runtime.CompilerServices;
using System.Text;

namespace EventStore.Client.Tests;

public partial class EventStoreFixture {
	const string TestEventType = "-";

	public T NewClient<T>(Action<EventStoreClientSettings> configure) where T : EventStoreClientBase, new() =>
		(T)Activator.CreateInstance(typeof(T), new object?[] { ClientSettings.With(configure) })!;
	
	public string GetStreamName([CallerMemberName] string? testMethod = null) =>
		$"{testMethod}-{Guid.NewGuid():N}";

	public IEnumerable<EventData> CreateTestEvents(int count = 1, string? type = null, int metadataSize = 1) =>
		Enumerable.Range(0, count).Select(index => CreateTestEvent(index, type ?? TestEventType, metadataSize));

	protected static EventData CreateTestEvent(int index) => CreateTestEvent(index, TestEventType, 1);

	protected static EventData CreateTestEvent(int index, string type, int metadataSize) =>
		new(
			Uuid.NewUuid(),
			type,
			Encoding.UTF8.GetBytes($$"""{"x":{{index}}}"""),
			Encoding.UTF8.GetBytes($"\"{new string('$', metadataSize)}\"")
		);

	public async Task<TestUser> CreateTestUser(bool withoutGroups = true, bool useUserCredentials = false) {
		var result = await CreateTestUsers(1, withoutGroups, useUserCredentials);
		return result.First();
	}

	public Task<TestUser[]> CreateTestUsers(int count = 3, bool withoutGroups = true, bool useUserCredentials = false) =>
		Fakers.Users
			.RuleFor(x => x.Groups, f => withoutGroups ? Array.Empty<string>() : f.Lorem.Words())
			.Generate(count)
			.Select(
				async user => {
					await Users.CreateUserAsync(
						user.LoginName, user.FullName, user.Groups, user.Password,
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