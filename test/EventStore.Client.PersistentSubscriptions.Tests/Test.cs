using System;
using Xunit;

namespace EventStore.Client {
	public class Test {

		[Fact]
		public void test() {
			Utils.DuplicateOutput("/tmp/test.txt");
			Console.Out.WriteLine("test_stdout");
			Console.Error.WriteLine("test_stderr");
		}

	}
}
