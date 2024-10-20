using AutoFixture;

namespace EventStore.Client.Tests;

public class PositionTests() : ValueObjectTests<Position>(new ScenarioFixture()) {
	[Fact]
	public void IsComparable() => Assert.IsAssignableFrom<IComparable<Position>>(_fixture.Create<Position>());

	[Theory]
	[AutoScenarioData(typeof(ScenarioFixture))]
	public void StartIsLessThanAll(Position other) => Assert.True(Position.Start < other);

	[Theory]
	[AutoScenarioData(typeof(ScenarioFixture))]
	public void LiveIsGreaterThanAll(Position other) => Assert.True(Position.End > other);

	[Fact]
	public void ToStringReturnsExpectedResult() {
		var sut = _fixture.Create<Position>();
		Assert.Equal($"C:{sut.CommitPosition}/P:{sut.PreparePosition}", sut.ToString());
	}

	public static IEnumerable<object?[]> ArgumentOutOfRangeTestCases() {
		const string commitPosition  = nameof(commitPosition);
		const string preparePosition = nameof(preparePosition);

		yield return [5, 6, commitPosition];
		yield return [ulong.MaxValue - 1, 6, commitPosition];
		yield return [ulong.MaxValue, ulong.MaxValue - 1, preparePosition];
		yield return [(ulong)long.MaxValue + 1, long.MaxValue, commitPosition];
	}

	[Theory]
	[MemberData(nameof(ArgumentOutOfRangeTestCases))]
	public void ArgumentOutOfRange(ulong commitPosition, ulong preparePosition, string name) {
		var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Position(commitPosition, preparePosition));
		Assert.Equal(name, ex.ParamName);
	}

	[Theory]
	[MemberData(nameof(ParseTestCases))]
	public void TryParse(string s, bool success, Position? expected) {
		Position? p;
		Assert.Equal(success, Position.TryParse(s, out p));
		Assert.Equal(expected, p);
	}

	public static IEnumerable<object?[]> ParseTestCases() {
		yield return ["", false, null];
		yield return ["CP", false, null];
		yield return ["C:6\\P:5", false, null];
		yield return [Position.Start.ToString(), true, Position.Start];
		yield return [Position.End.ToString(), true, Position.End];
		yield return ["C:6/P:5", true, new Position(6, 5)];
		yield return ["C: 6/P:5", true, new Position(6, 5)];
	}

	class ScenarioFixture : Fixture {
		public ScenarioFixture() => Customize<Position>(composer => composer.FromFactory<ulong>(value => new(value, value)));
	}
}
