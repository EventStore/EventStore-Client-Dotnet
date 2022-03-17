using System.Threading.Tasks;
using Xunit;

namespace EventStore.Client.Security
{
	public class overriden_user_stream_security : IClassFixture<overriden_user_stream_security.Fixture> {
		private readonly Fixture _fixture;

		public overriden_user_stream_security(Fixture fixture) {
			_fixture = fixture;
		}

		public class Fixture : SecurityFixture {
			protected override Task When() {
				var settings = new SystemSettings(
					new StreamAcl("user1", "user1", "user1", "user1", "user1"));
				return Client.SetSystemSettingsAsync(settings, userCredentials: TestCredentials.TestAdmin);
			}
		}

		[Fact]
		public async Task operations_on_user_stream_succeeds_for_authorized_user() {
			var stream = _fixture.GetStreamName();
			await _fixture.AppendStream(stream, userCredentials: TestCredentials.TestUser1);

			await _fixture.ReadEvent(stream, userCredentials: TestCredentials.TestUser1);
			await _fixture.ReadStreamForward(stream, userCredentials: TestCredentials.TestUser1);
			await _fixture.ReadStreamBackward(stream, userCredentials: TestCredentials.TestUser1);

			await _fixture.ReadMeta(stream, userCredentials: TestCredentials.TestUser1);
			await _fixture.WriteMeta(stream, userCredentials: TestCredentials.TestUser1);

			await _fixture.SubscribeToStream(stream, userCredentials: TestCredentials.TestUser1);

			await _fixture.DeleteStream(stream, userCredentials: TestCredentials.TestUser1);
		}

		[Fact]
		public async Task operations_on_user_stream_fail_for_not_authorized_user() {
			var stream = _fixture.GetStreamName();
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadEvent(stream, TestCredentials.TestUser2));
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadStreamForward(stream, TestCredentials.TestUser2));
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadStreamBackward(stream, TestCredentials.TestUser2));

			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream(stream, TestCredentials.TestUser2));
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadMeta(stream, TestCredentials.TestUser2));
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.WriteMeta(stream, TestCredentials.TestUser2));

			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.SubscribeToStream(stream, TestCredentials.TestUser2));

			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.DeleteStream(stream, TestCredentials.TestUser2));
		}

		[Fact]
		public async Task operations_on_user_stream_fail_for_anonymous_user() {
			var stream = _fixture.GetStreamName();
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadEvent(stream));
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadStreamForward(stream));
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadStreamBackward(stream));

			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.AppendStream(stream));
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.ReadMeta(stream));
			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.WriteMeta(stream));

			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.SubscribeToStream(stream));

			await Assert.ThrowsAsync<AccessDeniedException>(() => _fixture.DeleteStream(stream));
		}

		[Fact]
		public async Task operations_on_user_stream_succeed_for_admin() {
			var stream = _fixture.GetStreamName();
			await _fixture.AppendStream(stream, userCredentials: TestCredentials.TestAdmin);

			await _fixture.ReadEvent(stream, userCredentials: TestCredentials.TestAdmin);
			await _fixture.ReadStreamForward(stream, userCredentials: TestCredentials.TestAdmin);
			await _fixture.ReadStreamBackward(stream, userCredentials: TestCredentials.TestAdmin);

			await _fixture.ReadMeta(stream, userCredentials: TestCredentials.TestAdmin);
			await _fixture.WriteMeta(stream, userCredentials: TestCredentials.TestAdmin);

			await _fixture.SubscribeToStream(stream, userCredentials: TestCredentials.TestAdmin);

			await _fixture.DeleteStream(stream, userCredentials: TestCredentials.TestAdmin);
		}
	}
}
