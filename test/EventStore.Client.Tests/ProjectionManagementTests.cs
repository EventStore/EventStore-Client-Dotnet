// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Local

using EventStore.Client.Tests.TestNode;
using EventStore.Client.Tests;

namespace EventStore.Client.Tests;

public class ProjectionManagementTests(ITestOutputHelper output, ProjectionManagementTests.CustomFixture fixture)
	: KurrentTemporaryTests<ProjectionManagementTests.CustomFixture>(output, fixture) {
	[Fact]
	public async Task status_is_aborted() {
		var name = Names.First();
		await Fixture.Projections.AbortAsync(name, userCredentials: TestCredentials.Root);
		var result = await Fixture.Projections.GetStatusAsync(name, userCredentials: TestCredentials.Root);
		Assert.NotNull(result);
		Assert.Contains(["Aborted/Stopped", "Stopped"], x => x == result.Status);
	}

	[Fact]
	public async Task one_time() =>
		await Fixture.Projections.CreateOneTimeAsync("fromAll().when({$init: function (state, ev) {return {};}});", userCredentials: TestCredentials.Root);

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task continuous(bool trackEmittedStreams) {
		var name = Fixture.GetProjectionName();

		await Fixture.Projections.CreateContinuousAsync(
			name,
			"fromAll().when({$init: function (state, ev) {return {};}});",
			trackEmittedStreams,
			userCredentials: TestCredentials.Root
		);
	}

	[Fact]
	public async Task transient() {
		var name = Fixture.GetProjectionName();

		await Fixture.Projections.CreateTransientAsync(
			name,
			"fromAll().when({$init: function (state, ev) {return {};}});",
			userCredentials: TestCredentials.Root
		);
	}

	[Fact]
	public async Task disable_projection() {
		var name = Names.First();
		await Fixture.Projections.DisableAsync(name, userCredentials: TestCredentials.Root);
		var result = await Fixture.Projections.GetStatusAsync(name, userCredentials: TestCredentials.Root);
		Assert.NotNull(result);
		Assert.Contains(["Aborted/Stopped", "Stopped"], x => x == result!.Status);
	}

	[Fact]
	public async Task enable_projection() {
		var name = Names.First();
		await Fixture.Projections.EnableAsync(name, userCredentials: TestCredentials.Root);
		var result = await Fixture.Projections.GetStatusAsync(name, userCredentials: TestCredentials.Root);
		Assert.NotNull(result);
		Assert.Equal("Running", result.Status);
	}

	[Fact]
	public async Task get_result() {
		var     name   = Fixture.GetProjectionName();
		Result? result = null;

		var projection = $$"""
		                   fromStream('{{name}}').when({
		                   	"$init": function() { return { Count: 0 }; },
		                   	"$any": function(s, e) { s.Count++; return s; }
		                   });
		                   """;

		await Fixture.Projections.CreateContinuousAsync(
			name,
			projection,
			userCredentials: TestCredentials.Root
		);

		await Fixture.Streams.AppendToStreamAsync(
			name,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		await AssertEx.IsOrBecomesTrue(
			async () => {
				result = await Fixture.Projections.GetResultAsync<Result>(name, userCredentials: TestCredentials.Root);
				return result.Count > 0;
			}
		);

		Assert.NotNull(result);
		Assert.Equal(1, result!.Count);
	}

	[Fact]
	public async Task get_state() {
		var name = Fixture.GetProjectionName();

		var projection = $$"""
		                   fromStream('{{name}}').when({
		                   	"$init": function() { return { Count: 0 }; },
		                   	"$any": function(s, e) { s.Count++; return s; }
		                   });
		                   """;

		Result? result = null;

		await Fixture.Projections.CreateContinuousAsync(
			name,
			projection,
			userCredentials: TestCredentials.Root
		);

		await Fixture.Streams.AppendToStreamAsync(
			name,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		await AssertEx.IsOrBecomesTrue(
			async () => {
				result = await Fixture.Projections.GetStateAsync<Result>(name, userCredentials: TestCredentials.Root);
				return result.Count > 0;
			}
		);

		Assert.NotNull(result);
		Assert.Equal(1, result!.Count);
	}

	[Fact]
	public async Task get_status() {
		var name   = Names.First();
		var result = await Fixture.Projections.GetStatusAsync(name, userCredentials: TestCredentials.Root);

		Assert.NotNull(result);
		Assert.Equal(name, result.Name);
	}

	[Fact]
	public async Task restart_subsystem_does_not_throw() =>
		await Fixture.Projections.RestartSubsystemAsync(userCredentials: TestCredentials.Root);

	[Fact]
	public async Task restart_subsystem_throws_when_given_no_credentials() =>
		await Assert.ThrowsAsync<NotAuthenticatedException>(() => Fixture.Projections.RestartSubsystemAsync(userCredentials: TestCredentials.TestUser1));

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task update_projection(bool? emitEnabled) {
		var name = Fixture.GetProjectionName();
		await Fixture.Projections.CreateContinuousAsync(
			name,
			"fromAll().when({$init: function (state, ev) {return {};}});",
			userCredentials: TestCredentials.Root
		);

		await Fixture.Projections.UpdateAsync(
			name,
			"fromAll().when({$init: function (s, e) {return {};}});",
			emitEnabled,
			userCredentials: TestCredentials.Root
		);
	}

	[Fact]
	public async Task list_one_time_projections() {
		await Fixture.Projections.CreateOneTimeAsync("fromAll().when({$init: function (state, ev) {return {};}});", userCredentials: TestCredentials.Root);

		var result = await Fixture.Projections.ListOneTimeAsync(userCredentials: TestCredentials.Root)
			.ToArrayAsync();

		var details = Assert.Single(result);
		Assert.Equal("OneTime", details.Mode);
	}

	[Fact]
	public async Task reset_projection() {
		var name = Names.First();
		await Fixture.Projections.ResetAsync(name, userCredentials: TestCredentials.Root);
		var result = await Fixture.Projections.GetStatusAsync(name, userCredentials: TestCredentials.Root);

		Assert.NotNull(result);
		Assert.Equal("Running", result.Status);
	}

	static readonly string[] Names = ["$streams", "$stream_by_category", "$by_category", "$by_event_type", "$by_correlation_id"];

	record Result {
		public int Count { get; set; }
	}

	public class CustomFixture : KurrentTemporaryFixture {
		public CustomFixture() : base(x => x.RunProjections()) { }
	}
}
