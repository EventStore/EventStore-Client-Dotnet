using EventStore.Client;

namespace Kurrent.Client.Tests;

public record KurrentFixtureOptions(
	KurrentClientSettings ClientSettings,
	IDictionary<string, string?> Environment
) {
	public KurrentFixtureOptions RunInMemory(bool runInMemory = true) =>
		this with { Environment = Environment.With(x => x["EVENTSTORE_MEM_DB"] = runInMemory.ToString()) };

	public KurrentFixtureOptions WithoutDefaultCredentials() => this with { ClientSettings = ClientSettings.With(x => x.DefaultCredentials = null) };

	public KurrentFixtureOptions RunProjections(bool runProjections = true) =>
		this with {
			Environment = Environment.With(
				x => {
					x["EVENTSTORE_START_STANDARD_PROJECTIONS"] = runProjections.ToString();
					x["EVENTSTORE_RUN_PROJECTIONS"]            = runProjections ? "All" : "None";
				}
			)
		};
}

public delegate KurrentFixtureOptions ConfigureFixture(KurrentFixtureOptions options);
