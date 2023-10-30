using AutoFixture;

namespace EventStore.Client.Tests;

public class FromStreamTests : ValueObjectTests<FromStream> {
	public FromStreamTests() : base(new ScenarioFixture()) { }

	[Fact]
	public void IsComparable() => Assert.IsAssignableFrom<IComparable<FromStream>>(_fixture.Create<FromStream>());

	[Theory]
	[AutoScenarioData(typeof(ScenarioFixture))]
	public void StartIsLessThanAll(FromStream other) => Assert.True(FromStream.Start < other);

	[Theory]
	[AutoScenarioData(typeof(ScenarioFixture))]
	public void LiveIsGreaterThanAll(FromStream other) => Assert.True(FromStream.End > other);

	public static IEnumerable<object?[]> ToStringCases() {
		var fixture  = new ScenarioFixture();
		var position = fixture.Create<StreamPosition>();
		yield return new object?[] { FromStream.After(position), position.ToString() };
		yield return new object?[] { FromStream.Start, "Start" };
		yield return new object?[] { FromStream.End, "Live" };
	}

	[Theory]
	[MemberData(nameof(ToStringCases))]
	public void ToStringReturnsExpectedResult(FromStream sut, string expected) => Assert.Equal(expected, sut.ToString());

	[Fact]
	public void AfterLiveThrows() => Assert.Throws<ArgumentException>(() => FromStream.After(StreamPosition.End));

	[Fact]
	public void ToUInt64ReturnsExpectedResults() {
		var position = _fixture.Create<StreamPosition>();
		Assert.Equal(position.ToUInt64(), FromStream.After(position).ToUInt64());
	}

	class ScenarioFixture : Fixture {
		public ScenarioFixture() {
			Customize<StreamPosition>(composer => composer.FromFactory<ulong>(value => new(value)));
			Customize<FromStream>(composter => composter.FromFactory<StreamPosition>(FromStream.After));
		}
	}
}