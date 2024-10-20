using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Xunit.Sdk;

namespace EventStore.Client.Tests;

[DataDiscoverer("AutoFixture.Xunit2.NoPreDiscoveryDataDiscoverer", "AutoFixture.Xunit2")]
public class AutoScenarioDataAttribute(Type fixtureType, int iterations = 3) : DataAttribute {
	public int Iterations { get; } = iterations;

	public override IEnumerable<object[]> GetData(MethodInfo testMethod) {
		var customAutoData = new CustomAutoData(fixtureType);

		return Enumerable.Range(0, Iterations).SelectMany(_ => customAutoData.GetData(testMethod));
	}

	class CustomAutoData(Type fixtureType) : AutoDataAttribute(() => (IFixture)Activator.CreateInstance(fixtureType)!);
}
