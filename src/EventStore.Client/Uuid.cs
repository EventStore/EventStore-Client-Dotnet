using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EventStore.Client {
	/// <summary>
	/// An RFC-4122 compliant v4 UUID.
	/// </summary>
	public readonly struct Uuid : IEquatable<Uuid> {
		/// <summary>
		/// Represents the empty (00000000-0000-0000-0000-000000000000) <see cref="Uuid"/>.
		/// </summary>
		/// <remarks>
		/// This reorders the bits in System.Guid to improve interop with other languages. See: https://stackoverflow.com/a/16722909
		/// </remarks>
		public static readonly Uuid Empty = new Uuid(Guid.Empty);

		private readonly long _lsb;
		private readonly long _msb;

		/// <summary>
		/// Creates a new, randomized <see cref="Uuid"/>.
		/// </summary>
		/// <returns><see cref="Uuid"/></returns>
		public static Uuid NewUuid() => new Uuid(Guid.NewGuid());

		/// <summary>
		/// Converts a <see cref="Guid"/> to a <see cref="Uuid"/>.
		/// </summary>
		/// <param name="value"></param>
		/// <returns><see cref="Uuid"/></returns>
		public static Uuid FromGuid(Guid value) => new Uuid(value);

		/// <summary>
		/// Parses a <see cref="string"/> into a <see cref="Uuid"/>.
		/// </summary>
		/// <param name="value"></param>
		/// <returns><see cref="Uuid"/></returns>
		public static Uuid Parse(string value) => new Uuid(value);

		/// <summary>
		/// Creates a <see cref="Uuid"/> from a pair of <see cref="long"/>.
		/// </summary>
		/// <param name="msb">The <see cref="long"/> representing the most significant bits.</param>
		/// <param name="lsb">The <see cref="long"/> representing the least significant bits.</param>
		/// <returns></returns>
		public static Uuid FromInt64(long msb, long lsb) => new Uuid(msb, lsb);

		/// <summary>
		/// Creates a <see cref="Uuid"/> from the gRPC wire format.
		/// </summary>
		/// <param name="dto"></param>
		/// <returns><see cref="Uuid"/></returns>
		public static Uuid FromDto(UUID dto) =>
			dto == null
				? throw new ArgumentNullException(nameof(dto))
				: dto.ValueCase switch {
					UUID.ValueOneofCase.String => new Uuid(dto.String),
					UUID.ValueOneofCase.Structured => new Uuid(dto.Structured.MostSignificantBits,
						dto.Structured.LeastSignificantBits),
					_ => throw new ArgumentException($"Invalid argument: {dto.ValueCase}", nameof(dto))
				};

		private Uuid(Guid value) {
			if (!BitConverter.IsLittleEndian) {
				throw new NotSupportedException();
			}

			Span<byte> data = stackalloc byte[16];

			if (!TryWriteGuidBytes(value, data)) {
				throw new InvalidOperationException();
			}

			data[..8].Reverse();
			data[..2].Reverse();
			data.Slice(2, 2).Reverse();
			data.Slice(4, 4).Reverse();
			data[8..].Reverse();

			_msb = BitConverterToInt64(data);
			_lsb = BitConverterToInt64(data[8..]);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long BitConverterToInt64(ReadOnlySpan<byte> value)
		{
#if NET
			return BitConverter.ToInt64(value);
#else
			return Unsafe.ReadUnaligned<long>(ref MemoryMarshal.GetReference(value));
#endif
		}

		private Uuid(string value) : this(value == null
			? throw new ArgumentNullException(nameof(value))
			: Guid.Parse(value)) {
		}

		private Uuid(long msb, long lsb) {
			_msb = msb;
			_lsb = lsb;
		}

		/// <summary>
		/// Converts the <see cref="Uuid"/> to the gRPC wire format.
		/// </summary>
		/// <returns><see cref="UUID"/></returns>
		public UUID ToDto() =>
			new UUID {
				Structured = new UUID.Types.Structured {
					LeastSignificantBits = _lsb,
					MostSignificantBits  = _msb
				}
			};


		/// <inheritdoc />
		public bool Equals(Uuid other) => _lsb == other._lsb && _msb == other._msb;

		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is Uuid other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => HashCode.Hash.Combine(_lsb).Combine(_msb);

		/// <summary>
		/// Compares left and right for equality.
		/// </summary>
		/// <param name="left">A <see cref="Uuid"/></param>
		/// <param name="right">A <see cref="Uuid"/></param>
		/// <returns>True if left is equal to right.</returns>
		public static bool operator ==(Uuid left, Uuid right) => left.Equals(right);

		/// <summary>
		/// Compares left and right for inequality.
		/// </summary>
		/// <param name="left">A <see cref="Uuid"/></param>
		/// <param name="right">A <see cref="Uuid"/></param>
		/// <returns>True if left is not equal to right.</returns>
		public static bool operator !=(Uuid left, Uuid right) => !left.Equals(right);

		/// <inheritdoc />
		public override string ToString() => ToGuid().ToString();

		/// <summary>
		/// Converts the <see cref="Uuid"/> to a <see cref="string"/> based on the supplied format.
		/// </summary>
		/// <param name="format"></param>
		/// <returns><see cref="string"/></returns>
		public string ToString(string format) => ToGuid().ToString(format);

		/// <summary>
		/// Converts the <see cref="Uuid"/> to a <see cref="Guid"/>.
		/// </summary>
		/// <returns><see cref="Guid"/></returns>
		public Guid ToGuid() {
			if (!BitConverter.IsLittleEndian) {
				throw new NotSupportedException();
			}

			Span<byte> data = stackalloc byte[16];
			if (!TryWriteBytes(data, _msb) ||
			    !TryWriteBytes(data[8..], _lsb)) {
				throw new InvalidOperationException();
			}

			data[..8].Reverse();
			data[..4].Reverse();
			data.Slice(4, 2).Reverse();
			data.Slice(6, 2).Reverse();
			data[8..].Reverse();

#if NET
			return new Guid(data);
#else
			return new Guid(data.ToArray());
#endif
		}
		private static bool TryWriteBytes(Span<byte> destination, long value)
		{
			if (destination.Length < sizeof(long))
				return false;

			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			return true;
		}

		private bool TryWriteGuidBytes(Guid value, Span<byte> destination)
		{
#if NET
			return value.TryWriteBytes(destination);
#else
			if (destination.Length < 16)
				return false;

			var bytes = value.ToByteArray();
			bytes.CopyTo(destination);
			return true;
#endif
		}
	}
}
