using Ductus.FluentDocker.Builders;
using EventStore.Client.Tests.FluentDocker;

namespace EventStore.Client.Tests;

public class EventStoreTestVoid : TestContainerService {
    protected override ContainerBuilder Configure() => new Builder().UseContainer();
}