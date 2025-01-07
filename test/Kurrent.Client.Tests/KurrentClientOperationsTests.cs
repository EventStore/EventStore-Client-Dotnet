using EventStore.Client;

namespace Kurrent.Client.Tests;

public class KurrentClientOperationOptionsTests {
	[RetryFact]
	public void setting_options_on_clone_should_not_modify_original() {
		var options = KurrentClientOperationOptions.Default;

		var clonedOptions = options.Clone();
		clonedOptions.BatchAppendSize = int.MaxValue;

		Assert.Equal(options.BatchAppendSize, KurrentClientOperationOptions.Default.BatchAppendSize);
		Assert.Equal(int.MaxValue, clonedOptions.BatchAppendSize);
	}
}
