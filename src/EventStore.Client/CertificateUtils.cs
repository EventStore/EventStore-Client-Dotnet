using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#if NET48
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
#endif

namespace EventStore.Client;

/// <summary>
/// Utility class for loading certificates and private keys from files.
/// </summary>
static class CertificateUtils {
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
			{
				var pemReader        = new PemReader(new StringReader(string.Join(Environment.NewLine, allLines)));
				var keyPair          = (AsymmetricCipherKeyPair)pemReader.ReadObject();
				var privateKeyParams = (RsaPrivateCrtKeyParameters)keyPair.Private;
				rsa.ImportParameters(DotNetUtilities.ToRSAParameters(privateKeyParams));
			}
#endif
				break;

			case "BEGIN RSA PRIVATE KEY":
#if NET
				rsa.ImportRSAPrivateKey(new ReadOnlySpan<byte>(privateKey), out _);
#else
			{
				var pemReader = new PemReader(new StringReader(string.Join(Environment.NewLine, allLines)));
				object pemObject = pemReader.ReadObject();
				RsaPrivateCrtKeyParameters privateKeyParams;
				if (pemObject is RsaPrivateCrtKeyParameters) {
					privateKeyParams = (RsaPrivateCrtKeyParameters)pemObject;
				} else if (pemObject is AsymmetricCipherKeyPair keyPair) {
					privateKeyParams = (RsaPrivateCrtKeyParameters)keyPair.Private;
				} else {
					throw new NotSupportedException($"Unsupported PEM object type: {pemObject.GetType()}");
				}

				rsa.ImportParameters(DotNetUtilities.ToRSAParameters(privateKeyParams));
			}
#endif
				break;

			default:
				rsa.Dispose();
				throw new NotSupportedException($"Unsupported private key file format: {header}");
		}

		return rsa;
	}

	internal static X509Certificate2 LoadCertificate(string certificatePath) {
		return new X509Certificate2(certificatePath);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="certificatePath"></param>
	/// <param name="privateKeyPath"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public static X509Certificate2 LoadFromFile(string certificatePath, string privateKeyPath) {
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
}
