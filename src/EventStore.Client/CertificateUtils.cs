using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace EventStore.Client;

internal class CertificateUtils {
	private static RSA LoadKey(string privateKeyPath) {
		string[] allLines = File.ReadAllLines(privateKeyPath);
		var header = allLines[0].Replace("-", "");
		var privateKey = Convert.FromBase64String(string.Join(string.Empty, allLines.Skip(1).SkipLast(1)));

		var rsa = RSA.Create();
		switch (header) {
			case "BEGIN PRIVATE KEY":
				rsa.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(privateKey), out _);
				break;
			case "BEGIN RSA PRIVATE KEY":
				rsa.ImportRSAPrivateKey(new ReadOnlySpan<byte>(privateKey), out _);
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
		RSA? rsa = null;

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
			var certificate = new X509Certificate2(publicWithPrivate.Export(X509ContentType.Pfx));

			return certificate;
		} finally {
			publicCertificate?.Dispose();
			rsa?.Dispose();
		}
	}
}
