using System;
using System.Collections.Generic;
using Xunit;

namespace EventStore.Client {
	public class StreamStateTests {
		[Fact]
		public void Equality() {
			var sut = StreamState.NoStream;
			Assert.Equal(StreamState.NoStream, sut);
		}

		[Fact]
		public void Inequality() {
			var sut = StreamState.NoStream;
			Assert.NotEqual(StreamState.Any, sut);
		}

		[Fact]
		public void EqualityOperator() {
			var sut = StreamState.NoStream;
			Assert.True(StreamState.NoStream == sut);
		}

		[Fact]
		public void InequalityOperator() {
			var sut = StreamState.NoStream;
			Assert.True(StreamState.Any != sut);
		}

		public static IEnumerable<object[]> ArgumentOutOfRangeTestCases() {
			yield return new object[] {0};
			yield return new object[] {int.MaxValue};
			yield return new object[] {-3};
		}

		[Theory, MemberData(nameof(ArgumentOutOfRangeTestCases))]
		public void ArgumentOutOfRange(int value) {
			var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new StreamState(value));
			Assert.Equal(nameof(value), ex.ParamName);
		}

		[Fact]
		public void ExplicitConversionExpectedResult() {
			const int expected = 1;
			var actual = (int)new StreamState(expected);
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ImplicitConversionExpectedResult() {
			const int expected = 1;
			Assert.Equal(expected, new StreamState(expected));
		}

		public static IEnumerable<object[]> ToStringTestCases() {
			yield return new object[] {StreamState.Any, nameof(StreamState.Any)};
			yield return new object[] {StreamState.NoStream, nameof(StreamState.NoStream)};
			yield return new object[] {StreamState.StreamExists, nameof(StreamState.StreamExists)};
		}

		[Theory, MemberData(nameof(ToStringTestCases))]
		public void ToStringExpectedResult(StreamState sut, string expected) {
			Assert.Equal(expected, sut.ToString());
		}
	}
}
