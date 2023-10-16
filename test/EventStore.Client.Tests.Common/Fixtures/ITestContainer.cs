using System;

namespace EventStore.Tests.Fixtures;

public interface ITestContainer : IDisposable {
	void Start();
	void Stop();
}