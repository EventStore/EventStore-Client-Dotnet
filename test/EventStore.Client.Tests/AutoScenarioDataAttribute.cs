using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Xunit.Sdk;

namespace EventStore.Client {
	[DataDiscoverer("AutoFixture.Xunit2.NoPreDiscoveryDataDiscoverer", "AutoFixture.Xunit2")]
	public class AutoScenarioDataAttribute : DataAttribute {
		private readonly Type _fixtureType;
		public int Iterations { get; }

		public AutoScenarioDataAttribute(Type fixtureType, int iterations = 3) {
			_fixtureType = fixtureType;
			Iterations = iterations;
		}

		public override IEnumerable<object[]> GetData(MethodInfo testMethod) {
			var customAutoData = new CustomAutoData(_fixtureType);

			return Enumerable.Range(0, Iterations).SelectMany(_ => customAutoData.GetData(testMethod));
		}

		private class CustomAutoData : AutoDataAttribute {
			public CustomAutoData(Type fixtureType) : base(() => (IFixture)Activator.CreateInstance(fixtureType)!) {
			}
		}
	}
}
