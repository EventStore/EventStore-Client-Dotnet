using AutoFixture;
using EventStore.Client;

namespace Kurrent.Client.Tests;

[Trait("Category", "Target:Misc")]
public class UuidTests : ValueObjectTests<Uuid> {
	public UuidTests() : base(new ScenarioFixture()) { }

	[RetryFact]
	public void ToGuidReturnsExpectedResult() {
		var guid = Guid.NewGuid();
		var sut  = Uuid.FromGuid(guid);

		Assert.Equal(sut.ToGuid(), guid);
	}

	[RetryFact]
	public void ToStringProducesExpectedResult() {
		var sut = Uuid.NewUuid();

		Assert.Equal(sut.ToGuid().ToString(), sut.ToString());
	}

	[RetryFact]
	public void ToFormattedStringProducesExpectedResult() {
		var sut = Uuid.NewUuid();

		Assert.Equal(sut.ToGuid().ToString("n"), sut.ToString("n"));
	}

	[RetryFact]
	public void ToDtoReturnsExpectedResult() {
		var msb = GetRandomInt64();
		var lsb = GetRandomInt64();

		var sut = Uuid.FromInt64(msb, lsb);

		var result = sut.ToDto();

		Assert.NotNull(result.Structured);
		Assert.Equal(lsb, result.Structured.LeastSignificantBits);
		Assert.Equal(msb, result.Structured.MostSignificantBits);
	}

	[RetryFact]
	public void ParseReturnsExpectedResult() {
		var guid = Guid.NewGuid();

		var sut = Uuid.Parse(guid.ToString());

		Assert.Equal(Uuid.FromGuid(guid), sut);
	}

	[RetryFact]
	public void FromInt64ReturnsExpectedResult() {
		var guid     = Guid.Parse("65678f9b-d139-4786-8305-b9166922b378");
		var sut      = Uuid.FromInt64(7306966819824813958L, -9005588373953137800L);
		var expected = Uuid.FromGuid(guid);

		Assert.Equal(expected, sut);
	}

	static long GetRandomInt64() {
		var buffer = new byte[sizeof(long)];

		new Random().NextBytes(buffer);

		return BitConverter.ToInt64(buffer, 0);
	}

	class ScenarioFixture : Fixture {
		public ScenarioFixture() => Customize<Uuid>(composer => composer.FromFactory<Guid>(Uuid.FromGuid));
	}
}
