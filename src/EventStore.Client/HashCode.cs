using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

#nullable enable
namespace EventStore.Client {
	#pragma warning disable 1591
	public readonly struct HashCode {
		private readonly int _value;

		private HashCode(int value) {
			_value = value;
		}

		public static readonly HashCode Hash = default;

		public HashCode Combine<T>(T? value) where T : struct => Combine(value ?? default);
		
		public HashCode Combine<T>(T value) where T: struct {
			unchecked {
				return new HashCode((_value * 397) ^ value.GetHashCode());
			}
		}
		
		public HashCode Combine(string? value){
			unchecked {
				return new HashCode((_value * 397) ^ (value?.GetHashCode() ?? 0));
			}
		}

		public HashCode Combine<T>(IEnumerable<T>? values) where T: struct =>
			(values ?? Enumerable.Empty<T>()).Aggregate(Hash, (previous, value) => previous.Combine(value));

		public HashCode Combine(IEnumerable<string>? values) =>
			(values ?? Enumerable.Empty<string>()).Aggregate(Hash, (previous, value) => previous.Combine(value));

		public static implicit operator int(HashCode value) => value._value;
	}
}
