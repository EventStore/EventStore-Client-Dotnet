using AutoFixture;

namespace EventStore.Client; 

public class FromAllTests : ValueObjectTests<FromAll> {
    public FromAllTests() : base(new ScenarioFixture()) { }

    [Fact]
    public void IsComparable() =>
        Assert.IsAssignableFrom<IComparable<FromAll>>(_fixture.Create<FromAll>());

    [Theory, AutoScenarioData(typeof(ScenarioFixture))]
    public void StartIsLessThanAll(FromAll other) => Assert.True(FromAll.Start < other);

    [Theory, AutoScenarioData(typeof(ScenarioFixture))]
    public void LiveIsGreaterThanAll(FromAll other) => Assert.True(FromAll.End > other);

    public static IEnumerable<object?[]> ToStringCases() {
        var fixture  = new ScenarioFixture();
        var position = fixture.Create<Position>();
        yield return new object?[] {FromAll.After(position), position.ToString()};
        yield return new object?[] {FromAll.Start, "Start"};
        yield return new object?[] {FromAll.End, "Live"};
    }

    [Theory, MemberData(nameof(ToStringCases))]
    public void ToStringReturnsExpectedResult(FromAll sut, string expected) =>
        Assert.Equal(expected, sut.ToString());

    [Fact]
    public void AfterLiveThrows() =>
        Assert.Throws<ArgumentException>(() => FromAll.After(Position.End));

    [Fact]
    public void ToUInt64ReturnsExpectedResults() {
        var position = _fixture.Create<Position>();
        Assert.Equal(
            (position.CommitPosition, position.PreparePosition),
            FromAll.After(position).ToUInt64()
        );
    }

    class ScenarioFixture : Fixture {
        public ScenarioFixture() {
            Customize<Position>(composer => composer.FromFactory<ulong>(value => new Position(value, value)));
            Customize<FromAll>(composter => composter.FromFactory<Position>(FromAll.After));
        }
    }
}