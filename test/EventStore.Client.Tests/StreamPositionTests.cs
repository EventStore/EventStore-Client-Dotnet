using System;
using System.Collections.Generic;
using Xunit;

namespace EventStore.Client {
	public class StreamPositionTests {
		[Fact]
		public void Equality() {
			var sut = new StreamPosition(1);
			Assert.Equal(new StreamPosition(1), sut);
		}

		[Fact]
		public void Inequality() {
			var sut = new StreamPosition(1);
			Assert.NotEqual(new StreamPosition(2), sut);
		}

		[Fact]
		public void EqualityOperator() {
			var sut = new StreamPosition(1);
			Assert.True(new StreamPosition(1) == sut);
		}

		[Fact]
		public void InequalityOperator() {
			var sut = new StreamPosition(1);
			Assert.True(new StreamPosition(2) != sut);
		}

		[Fact]
		public void AdditionOperator() {
			var sut = StreamPosition.Start;
			Assert.Equal(new StreamPosition(1), sut + 1);
			Assert.Equal(new StreamPosition(1), 1 + sut);
		}

		public static IEnumerable<object[]> AdditionOutOfBoundsCases() {
			yield return new object[] {StreamPosition.End, 1};
			yield return new object[] {new StreamPosition(long.MaxValue), long.MaxValue + 2UL};
		}

		[Theory, MemberData(nameof(AdditionOutOfBoundsCases))]
		public void AdditionOutOfBoundsThrows(StreamPosition StreamPosition, ulong operand) {
			Assert.Throws<OverflowException>(() => StreamPosition + operand);
			Assert.Throws<OverflowException>(() => operand + StreamPosition);
		}

		[Fact]
		public void SubtractionOperator() {
			var sut = new StreamPosition(1);
			Assert.Equal(new StreamPosition(0), sut - 1);
			Assert.Equal(new StreamPosition(0), 1 - sut);
		}

		public static IEnumerable<object[]> SubtractionOutOfBoundsCases() {
			yield return new object[] {new StreamPosition(1), 2};
			yield return new object[] {StreamPosition.Start, 1};
		}

		[Theory, MemberData(nameof(SubtractionOutOfBoundsCases))]
		public void SubtractionOutOfBoundsThrows(StreamPosition StreamPosition, ulong operand) {
			Assert.Throws<OverflowException>(() => StreamPosition - operand);
			Assert.Throws<OverflowException>(() => (ulong)StreamPosition - new StreamPosition(operand));
		}

		public static IEnumerable<object[]> ArgumentOutOfRangeTestCases() {
			yield return new object[] {long.MaxValue + 1UL};
			yield return new object[] {ulong.MaxValue - 1UL};
		}

		[Theory, MemberData(nameof(ArgumentOutOfRangeTestCases))]
		public void ArgumentOutOfRange(ulong value) {
			var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new StreamPosition(value));
			Assert.Equal(nameof(value), ex.ParamName);
		}

		public static IEnumerable<object[]> ComparableTestCases() {
			yield return new object[] {StreamPosition.Start, StreamPosition.Start, 0};
			yield return new object[] {StreamPosition.Start, StreamPosition.End, -1};
			yield return new object[] {StreamPosition.End, StreamPosition.Start, 1};
		}

		[Theory, MemberData(nameof(ComparableTestCases))]
		public void Comparability(StreamPosition left, StreamPosition right, int expected)
			=> Assert.Equal(expected, left.CompareTo(right));

		[Fact]
		public void ExplicitConversionFromStreamPositionReturnsExpectedResult() {
			const ulong value = 0UL;
			var actual = (ulong)new StreamPosition(value);
			Assert.Equal(value, actual);
		}

		[Fact]
		public void ImplicitConversionFromStreamPositionReturnsExpectedResult() {
			const ulong value = 0UL;
			ulong actual = new StreamPosition(value);
			Assert.Equal(value, actual);
		}

		[Fact]
		public void ExplicitConversionToStreamPositionReturnsExpectedResult() {
			const ulong value = 0UL;
			var expected = new StreamPosition(value);
			var actual = (StreamPosition)value;
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ImplicitConversionToStreamPositionReturnsExpectedResult() {
			const ulong value = 0UL;
			var expected = new StreamPosition(value);
			StreamPosition actual = value;
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ToStringExpectedResult() {
			var expected = 0UL.ToString();

			Assert.Equal(expected, new StreamPosition(0UL).ToString());
		}

		[Fact]
		public void ToUInt64ExpectedResult() {
			var expected = 0UL;

			Assert.Equal(expected, new StreamPosition(expected).ToUInt64());
		}
	}
}
