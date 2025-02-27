using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;

namespace Kurrent.Client.Tests.FluentDocker;

public abstract class TestCompositeService : TestService<ICompositeService, CompositeBuilder>;
