using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;

namespace EventStore.Client.Tests.FluentDocker;

public abstract class TestCompositeService : TestService<ICompositeService, CompositeBuilder> { }