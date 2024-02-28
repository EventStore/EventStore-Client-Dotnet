using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace EventStore.Client;

internal class CertificateUtils {
	private static RSA LoadKey(string privateKeyPath) {
		string[] allLines        = File.ReadAllLines(privateKeyPath);
		var      header          = allLines[0].Replace("-", "");
		var      privateKeyLines = allLines.Skip(1).Take(allLines.Length - 2);
		var      privateKey      = Convert.FromBase64String(string.Join(string.Empty, privateKeyLines));

		var rsa = RSA.Create();
		switch (header) {
			case "BEGIN PRIVATE KEY":
#if NET
				rsa.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(privateKey), out _);
#else
				LightweightPkcs8Decoder.DecodeRSAPkcs8(privateKey);
#endif
				break;

			case "BEGIN RSA PRIVATE KEY":
#if NET
				rsa.ImportRSAPrivateKey(new ReadOnlySpan<byte>(privateKey), out _);
#else

				GetRsaParameters(new ReadOnlySpan<byte>(privateKey), true);
#endif
				break;

			default:
				rsa.Dispose();
				throw new NotSupportedException($"Unsupported private key file format: {header}");
		}

		return rsa;
	}

	private static X509Certificate2 LoadCertificate(string certificatePath) {
		return new X509Certificate2(certificatePath);
	}

	internal static X509Certificate2 LoadFromFile(string certificatePath, string privateKeyPath) {
		X509Certificate2? publicCertificate = null;
		RSA?              rsa               = null;

		try {
			try {
				publicCertificate = LoadCertificate(certificatePath);
			} catch (Exception ex) {
				throw new Exception($"Failed to load certificate: {ex.Message}");
			}

			try {
				rsa = LoadKey(privateKeyPath);
			} catch (Exception ex) {
				throw new Exception($"Failed to load private key: {ex.Message}");
			}

			using var publicWithPrivate = publicCertificate.CopyWithPrivateKey(rsa);
			var       certificate       = new X509Certificate2(publicWithPrivate.Export(X509ContentType.Pfx));

			return certificate;
		} finally {
			publicCertificate?.Dispose();
			rsa?.Dispose();
		}
	}

	// Derived from https://github.com/mysql-net/MySqlConnector/blob/bbdbd782e7434b765154805b1cb61d8daac68112/src/MySqlConnector/Utilities/Utility.cs#L150

	// Reads a length encoded according to ASN.1 BER rules.
	private static bool TryReadAsnLength(ReadOnlySpan<byte> data, out int length, out int bytesConsumed) {
		var leadByte = data[0];
		if (leadByte < 0x80) {
			// Short form. One octet. Bit 8 has value "0" and bits 7-1 give the length.
			length        = leadByte;
			bytesConsumed = 1;
			return true;
		}

		// Long form. Two to 127 octets. Bit 8 of first octet has value "1" and bits 7-1 give the number of additional length octets. Second and following octets give the length, base 256, most significant digit first.
		if (leadByte == 0x81) {
			length        = data[1];
			bytesConsumed = 2;
			return true;
		}

		if (leadByte == 0x82) {
			length        = data[1] * 256 + data[2];
			bytesConsumed = 3;
			return true;
		}

		// lengths over 2^16 are not currently handled
		length        = 0;
		bytesConsumed = 0;
		return false;
	}

	private static bool TryReadAsnInteger(
		ReadOnlySpan<byte> data, out ReadOnlySpan<byte> number, out int bytesConsumed
	) {
		// integer tag is 2
		if (data is not [0x02, ..]) {
			number        = default;
			bytesConsumed = 0;
			return false;
		}

		data = data[1..];

		// tag is followed by the length of the integer
		if (!TryReadAsnLength(data, out var length, out var lengthBytesConsumed)) {
			number        = default;
			bytesConsumed = 0;
			return false;
		}

		// length is followed by the integer bytes, MSB first
		number        = data.Slice(lengthBytesConsumed, length);
		bytesConsumed = lengthBytesConsumed + length + 1;

		// trim leading zero bytes
		while (number is [0, _, ..])
			number = number[1..];

		return true;
	}

	private static RSAParameters GetRsaParameters(ReadOnlySpan<byte> data, bool isPrivate) {
		// read header (30 81 xx, or 30 82 xx xx)
		if (data[0] != 0x30)
			throw new FormatException($"Expected 0x30 but read 0x{data[0]:X2}");

		data = data.Slice(1);

		if (!TryReadAsnLength(data, out var length, out var bytesConsumed))
			throw new FormatException("Couldn't read key length");

		data = data.Slice(bytesConsumed);

		if (!isPrivate) {
			// encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
			ReadOnlySpan<byte> rsaOid = [
				0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00
			];

			if (!data.Slice(0, rsaOid.Length).SequenceEqual(rsaOid))
				throw new FormatException(
					$"Expected RSA OID but read {BitConverter.ToString(data.Slice(0, 15).ToArray())}"
				);

			data = data.Slice(rsaOid.Length);

			// BIT STRING (0x03) followed by length
			if (data[0] != 0x03)
				throw new FormatException($"Expected 0x03 but read 0x{data[0]:X2}");

			data = data.Slice(1);

			if (!TryReadAsnLength(data, out length, out bytesConsumed))
				throw new FormatException("Couldn't read length");

			data = data.Slice(bytesConsumed);

			// skip NULL byte
			if (data[0] != 0x00)
				throw new FormatException($"Expected 0x00 but read 0x{data[0]:X2}");

			data = data.Slice(1);

			// skip next header (30 81 xx, or 30 82 xx xx)
			if (data[0] != 0x30)
				throw new FormatException($"Expected 0x30 but read 0x{data[0]:X2}");

			data = data.Slice(1);

			if (!TryReadAsnLength(data, out length, out bytesConsumed))
				throw new FormatException("Couldn't read length");

			data = data.Slice(bytesConsumed);
		} else {
			if (!TryReadAsnInteger(data, out var zero, out bytesConsumed) || zero.Length != 1 || zero[0] != 0)
				throw new FormatException("Couldn't read zero.");

			data = data.Slice(bytesConsumed);
		}

		if (!TryReadAsnInteger(data, out var modulus, out bytesConsumed))
			throw new FormatException("Couldn't read modulus");

		data = data.Slice(bytesConsumed);

		if (!TryReadAsnInteger(data, out var exponent, out bytesConsumed))
			throw new FormatException("Couldn't read exponent");

		data = data.Slice(bytesConsumed);

		if (!isPrivate) {
			return new RSAParameters {
				Modulus  = modulus.ToArray(),
				Exponent = exponent.ToArray(),
			};
		}

		if (!TryReadAsnInteger(data, out var d, out bytesConsumed))
			throw new FormatException("Couldn't read D");

		data = data.Slice(bytesConsumed);

		if (!TryReadAsnInteger(data, out var p, out bytesConsumed))
			throw new FormatException("Couldn't read P");

		data = data.Slice(bytesConsumed);

		if (!TryReadAsnInteger(data, out var q, out bytesConsumed))
			throw new FormatException("Couldn't read Q");

		data = data.Slice(bytesConsumed);

		if (!TryReadAsnInteger(data, out var dp, out bytesConsumed))
			throw new FormatException("Couldn't read DP");

		data = data.Slice(bytesConsumed);

		if (!TryReadAsnInteger(data, out var dq, out bytesConsumed))
			throw new FormatException("Couldn't read DQ");

		data = data.Slice(bytesConsumed);

		if (!TryReadAsnInteger(data, out var iq, out bytesConsumed))
			throw new FormatException("Couldn't read IQ");

		data = data.Slice(bytesConsumed);

		return new RSAParameters {
			Modulus  = modulus.ToArray(),
			Exponent = exponent.ToArray(),
			D        = d.ToArray(),
			P        = p.ToArray(),
			Q        = q.ToArray(),
			DP       = dp.ToArray(),
			DQ       = dq.ToArray(),
			InverseQ = iq.ToArray(),
		};
	}
}
