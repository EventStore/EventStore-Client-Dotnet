using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Xunit.Sdk;

namespace EventStore.Client.Tests;

[DataDiscoverer("AutoFixture.Xunit2.NoPreDiscoveryDataDiscoverer", "AutoFixture.Xunit2")]
public class AutoScenarioDataAttribute : DataAttribute {
	readonly Type _fixtureType;

	public AutoScenarioDataAttribute(Type fixtureType, int iterations = 3) {
		_fixtureType = fixtureType;
		Iterations   = iterations;
	}

	public int Iterations { get; }

	public override IEnumerable<object[]> GetData(MethodInfo testMethod) {
		var customAutoData = new CustomAutoData(_fixtureType);

		return Enumerable.Range(0, Iterations).SelectMany(_ => customAutoData.GetData(testMethod));
	}

	class CustomAutoData : AutoDataAttribute {
		public CustomAutoData(Type fixtureType) : base(() => (IFixture)Activator.CreateInstance(fixtureType)!) { }
	}
}
