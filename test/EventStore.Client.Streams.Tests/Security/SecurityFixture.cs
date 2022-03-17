using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EventStore.Client.Security {
	public abstract class SecurityFixture : EventStoreClientFixture {
		public const string NoAclStream = nameof(NoAclStream);
		public const string ReadStream = nameof(ReadStream);
		public const string WriteStream = nameof(WriteStream);
		public const string MetaReadStream = nameof(MetaReadStream);
		public const string MetaWriteStream = nameof(MetaWriteStream);
		public const string AllStream = SystemStreams.AllStream;
		public const string NormalAllStream = nameof(NormalAllStream);
		public const string SystemAllStream = "$" + nameof(SystemAllStream);
		public const string SystemAdminStream = "$" + nameof(SystemAdminStream);
		public const string SystemAclStream = "$" + nameof(SystemAclStream);
		private const int TimeoutMs = 1000;

		public EventStoreUserManagementClient UserManagementClient { get; }

		protected SecurityFixture() {
			UserManagementClient = new EventStoreUserManagementClient(Settings);
		}

		protected override async Task OnServerUpAsync() {
			await base.OnServerUpAsync();

			await UserManagementClient.WarmUpAsync();

			await UserManagementClient.CreateUserWithRetry(TestCredentials.TestUser1.Username!,
				nameof(TestCredentials.TestUser1), Array.Empty<string>(), TestCredentials.TestUser1.Password!,
				TestCredentials.Root).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

			await UserManagementClient.CreateUserWithRetry(TestCredentials.TestUser2.Username!,
				nameof(TestCredentials.TestUser2), Array.Empty<string>(), TestCredentials.TestUser2.Password!,
				TestCredentials.Root).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

			await UserManagementClient.CreateUserWithRetry(TestCredentials.TestAdmin.Username!,
				nameof(TestCredentials.TestAdmin), new[] {SystemRoles.Admins}, TestCredentials.TestAdmin.Password!,
				userCredentials: TestCredentials.Root).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
		}

		protected override async Task Given() {
			await Client.SetStreamMetadataAsync(NoAclStream, StreamState.NoStream, new StreamMetadata(),
				userCredentials: TestCredentials.TestAdmin).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
			await Client.SetStreamMetadataAsync(
				ReadStream,
				StreamState.NoStream,
				new StreamMetadata(acl: new StreamAcl(TestCredentials.TestUser1.Username)),
				userCredentials: TestCredentials.TestAdmin).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
			await Client.SetStreamMetadataAsync(
				WriteStream,
				StreamState.NoStream,
				new StreamMetadata(acl: new StreamAcl(writeRole: TestCredentials.TestUser1.Username)),
				userCredentials: TestCredentials.TestAdmin).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
			await Client.SetStreamMetadataAsync(
				MetaReadStream,
				StreamState.NoStream,
				new StreamMetadata(acl: new StreamAcl(metaReadRole: TestCredentials.TestUser1.Username)),
				userCredentials: TestCredentials.TestAdmin).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
			await Client.SetStreamMetadataAsync(
				MetaWriteStream,
				StreamState.NoStream,
				new StreamMetadata(acl: new StreamAcl(metaWriteRole: TestCredentials.TestUser1.Username)),
				userCredentials: TestCredentials.TestAdmin).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

			await Client.SetStreamMetadataAsync(
				AllStream,
				StreamState.Any,
				new StreamMetadata(acl: new StreamAcl(readRole: TestCredentials.TestUser1.Username)),
				userCredentials: TestCredentials.TestAdmin).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

			await Client.SetStreamMetadataAsync(
				SystemAclStream,
				StreamState.NoStream,
				new StreamMetadata(acl: new StreamAcl(
					writeRole: TestCredentials.TestUser1.Username,
					readRole: TestCredentials.TestUser1.Username,
					metaWriteRole: TestCredentials.TestUser1.Username,
					metaReadRole: TestCredentials.TestUser1.Username)),
				userCredentials: TestCredentials.TestAdmin).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

			await Client.SetStreamMetadataAsync(
				SystemAdminStream,
				StreamState.NoStream,
				new StreamMetadata(acl: new StreamAcl(
					writeRole: SystemRoles.Admins,
					readRole: SystemRoles.Admins,
					metaWriteRole: SystemRoles.Admins,
					metaReadRole: SystemRoles.Admins)),
				userCredentials: TestCredentials.TestAdmin).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

			await Client.SetStreamMetadataAsync(
				NormalAllStream,
				StreamState.NoStream,
				new StreamMetadata(acl: new StreamAcl(
					writeRole: SystemRoles.All,
					readRole: SystemRoles.All,
					metaWriteRole: SystemRoles.All,
					metaReadRole: SystemRoles.All)),
				userCredentials: TestCredentials.TestAdmin).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

			await Client.SetStreamMetadataAsync(
				SystemAllStream,
				StreamState.NoStream,
				new StreamMetadata(acl: new StreamAcl(
					writeRole: SystemRoles.All,
					readRole: SystemRoles.All,
					metaWriteRole: SystemRoles.All,
					metaReadRole: SystemRoles.All)),
				userCredentials: TestCredentials.TestAdmin).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
		}

		public Task ReadEvent(string streamId, UserCredentials? userCredentials = default) =>
			Client.ReadStreamAsync(Direction.Forwards, streamId, StreamPosition.Start, 1, resolveLinkTos: false,
					userCredentials: userCredentials)
				.ToArrayAsync()
				.AsTask()
				.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		public Task ReadStreamForward(string streamId, UserCredentials? userCredentials = default) =>
			Client.ReadStreamAsync(Direction.Forwards, streamId, StreamPosition.Start, 1, resolveLinkTos: false,
					userCredentials: userCredentials)
				.ToArrayAsync()
				.AsTask()
				.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		public Task ReadStreamBackward(string streamId, UserCredentials? userCredentials = default) =>
			Client.ReadStreamAsync(Direction.Backwards, streamId, StreamPosition.Start, 1, resolveLinkTos: false,
					userCredentials: userCredentials)
				.ToArrayAsync()
				.AsTask()
				.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		public Task<IWriteResult> AppendStream(string streamId, UserCredentials? userCredentials = default) =>
			Client.AppendToStreamAsync(streamId, StreamState.Any, CreateTestEvents(3),
					userCredentials: userCredentials)
				.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		public Task ReadAllForward(UserCredentials? userCredentials = default) =>
			Client.ReadAllAsync(Direction.Forwards, Position.Start, 1, resolveLinkTos: false,
					userCredentials: userCredentials)
				.ToArrayAsync()
				.AsTask()
				.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		public Task ReadAllBackward(UserCredentials? userCredentials = default) =>
			Client.ReadAllAsync(Direction.Backwards, Position.End, 1, resolveLinkTos: false,
					userCredentials: userCredentials)
				.ToArrayAsync()
				.AsTask()
				.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		public Task<StreamMetadataResult> ReadMeta(string streamId, UserCredentials? userCredentials = default) =>
			Client.GetStreamMetadataAsync(streamId, userCredentials: userCredentials)
				.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		public Task<IWriteResult> WriteMeta(string streamId, UserCredentials? userCredentials = default,
			string? role = default) =>
			Client.SetStreamMetadataAsync(streamId, StreamState.Any,
					new StreamMetadata(acl: new StreamAcl(
						writeRole: role,
						readRole: role,
						metaWriteRole: role,
						metaReadRole: role)),
					userCredentials: userCredentials)
				.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		public async Task SubscribeToStream(string streamId, UserCredentials? userCredentials = default) {
			var source = new TaskCompletionSource<bool>();
			using (await Client.SubscribeToStreamAsync(streamId, FromStream.Start, (_, _, _) => {
					       source.TrySetResult(true);
					       return Task.CompletedTask;
				       },
				       subscriptionDropped: (_, _, ex) => {
					       if (ex == null) source.TrySetResult(true);
					       else source.TrySetException(ex);
				       }, userCredentials: userCredentials).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs))) {
				await source.Task.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
			}
		}

		public async Task SubscribeToAll(UserCredentials? userCredentials = default) {
			var source = new TaskCompletionSource<bool>();
			using (await Client.SubscribeToAllAsync(FromAll.Start,
				       (_, _, _) => {
					       source.TrySetResult(true);
					       return Task.CompletedTask;
				       }, false, (_, _, ex) => {
					       if (ex == null) source.TrySetResult(true);
					       else source.TrySetException(ex);
				       },
				       userCredentials: userCredentials).WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs))) {
				await source.Task.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
			}
		}

		public async Task<string> CreateStreamWithMeta(StreamMetadata metadata,
			[CallerMemberName] string streamId = "<unknown>") {
			await Client.SetStreamMetadataAsync(streamId, StreamState.NoStream,
					metadata, userCredentials: TestCredentials.TestAdmin)
				.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));
			return streamId;
		}

		public Task<DeleteResult> DeleteStream(string streamId, UserCredentials? userCredentials = default) =>
			Client.TombstoneAsync(streamId, StreamState.Any, userCredentials: userCredentials)
				.WithTimeout(TimeSpan.FromMilliseconds(TimeoutMs));

		public async override Task DisposeAsync() {
			await UserManagementClient.DisposeAsync();
			await base.DisposeAsync();
		}
	}
}
