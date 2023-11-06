using Xunit;

namespace EventStore.Client {
	public class EventStoreClientOperationOptionsTests {
		[Fact]
		public void setting_options_on_clone_should_not_modify_original() {
			EventStoreClientOperationOptions options = EventStoreClientOperationOptions.Default;
			
			var clonedOptions = options.Clone();
			clonedOptions.BatchAppendSize = int.MaxValue;
			
			Assert.Equal(options.BatchAppendSize, EventStoreClientOperationOptions.Default.BatchAppendSize);
			Assert.Equal(int.MaxValue, clonedOptions.BatchAppendSize);
		}
	}
}
