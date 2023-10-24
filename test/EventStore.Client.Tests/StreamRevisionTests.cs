using AutoFixture;

namespace EventStore.Client;

public class StreamRevisionTests : ValueObjectTests<StreamRevision> {
    public StreamRevisionTests() : base(new ScenarioFixture()) { }

    [Fact]
    public void IsComparable() => Assert.IsAssignableFrom<IComparable<StreamRevision>>(_fixture.Create<StreamRevision>());

    [Fact]
    public void AdditionOperator() {
        var sut = new StreamRevision(0);
        Assert.Equal(new(1), sut + 1);
        Assert.Equal(new(1), 1 + sut);
    }

    [Fact]
    public void NextReturnsExpectedResult() {
        var sut = new StreamRevision(0);
        Assert.Equal(sut + 1, sut.Next());
    }

    public static IEnumerable<object?[]> AdditionOutOfBoundsCases() {
        yield return new object?[] { new StreamRevision(long.MaxValue), long.MaxValue + 2UL };
    }

    [Theory]
    [MemberData(nameof(AdditionOutOfBoundsCases))]
    public void AdditionOutOfBoundsThrows(StreamRevision streamRevision, ulong operand) {
        Assert.Throws<OverflowException>(() => streamRevision + operand);
        Assert.Throws<OverflowException>(() => operand + streamRevision);
    }

    [Fact]
    public void SubtractionOperator() {
        var sut = new StreamRevision(1);
        Assert.Equal(new(0), sut - 1);
        Assert.Equal(new(0), 1 - sut);
    }

    public static IEnumerable<object?[]> SubtractionOutOfBoundsCases() {
        yield return new object?[] { new StreamRevision(1), 2 };
        yield return new object?[] { new StreamRevision(0), 1 };
    }

    [Theory]
    [MemberData(nameof(SubtractionOutOfBoundsCases))]
    public void SubtractionOutOfBoundsThrows(StreamRevision streamRevision, ulong operand) {
        Assert.Throws<OverflowException>(() => streamRevision - operand);
        Assert.Throws<OverflowException>(() => (ulong)streamRevision - new StreamRevision(operand));
    }

    public static IEnumerable<object?[]> ArgumentOutOfRangeTestCases() {
        yield return new object?[] { long.MaxValue + 1UL };
        yield return new object?[] { ulong.MaxValue - 1UL };
    }

    [Theory]
    [MemberData(nameof(ArgumentOutOfRangeTestCases))]
    public void ArgumentOutOfRange(ulong value) {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new StreamRevision(value));
        Assert.Equal(nameof(value), ex.ParamName);
    }

    [Fact]
    public void FromStreamPositionEndThrows() => 
        Assert.Throws<ArgumentOutOfRangeException>(() => StreamRevision.FromStreamPosition(StreamPosition.End));

    [Fact]
    public void FromStreamPositionReturnsExpectedResult() {
        var result = StreamRevision.FromStreamPosition(StreamPosition.Start);

        Assert.Equal(new(0), result);
    }

    [Fact]
    public void ExplicitConversionToUInt64ReturnsExpectedResult() {
        const ulong value = 0UL;

        var actual = (ulong)new StreamRevision(value);

        Assert.Equal(value, actual);
    }

    [Fact]
    public void ImplicitConversionToUInt64ReturnsExpectedResult() {
        const ulong value = 0UL;

        ulong actual = new StreamRevision(value);

        Assert.Equal(value, actual);
    }

    [Fact]
    public void ExplicitConversionToStreamRevisionReturnsExpectedResult() {
        const ulong value = 0UL;

        var expected = new StreamRevision(value);
        var actual   = (StreamRevision)value;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ImplicitConversionToStreamRevisionReturnsExpectedResult() {
        const ulong value = 0UL;

        var expected = new StreamRevision(value);

        StreamRevision actual = value;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToStringExpectedResult() {
        var expected = 0UL.ToString();

        Assert.Equal(expected, new StreamRevision(0UL).ToString());
    }

    [Fact]
    public void ToUInt64ExpectedResult() {
        var expected = 0UL;

        Assert.Equal(expected, new StreamRevision(expected).ToUInt64());
    }

    class ScenarioFixture : Fixture {
        public ScenarioFixture() => Customize<StreamRevision>(composer => composer.FromFactory<ulong>(value => new(value)));
    }
}