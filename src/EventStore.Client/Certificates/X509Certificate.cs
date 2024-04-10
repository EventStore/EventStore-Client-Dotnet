#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#if NET48
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
#endif

namespace EventStore.Client;

static class X509Certificates {
#if NET48
	public static X509Certificate2 CreateFromPemFile(string certPemFilePath, string keyPemFilePath) {
		try {
			using var publicCert  = new X509Certificate2(certPemFilePath);
			using var privateKey  = RSA.Create().ImportPrivateKeyFromFile(keyPemFilePath);
			using var certificate = publicCert.CopyWithPrivateKey(privateKey);

			return new(certificate.Export(X509ContentType.Pfx));
		}
		catch (Exception ex) {
			throw new CryptographicException($"Failed to load private key: {ex.Message}");
		}
	}
#else
	public static X509Certificate2 CreateFromPemFile(string certPemFilePath, string keyPemFilePath) {
		// TODO:
		// using X509Certificate2.CreateFromPemFile(certPemFilePath, keyPemFilePath) would be the ideal choice here,
		// but it's currently causing a Win32Exception specifically on Windows. Alternative implementation is used until the issue is resolved.
		// Error: The SSL connection could not be established, see inner exception. AuthenticationException: Authentication failed because the platform
		// does not support ephemeral keys. Win32Exception: No credentials are available in the security package
		try {
			using var publicCert  = new X509Certificate2(certPemFilePath);
			using var privateKey  = RSA.Create().ImportPrivateKeyFromFile(keyPemFilePath);
			using var certificate = publicCert.CopyWithPrivateKey(privateKey);

			return new(certificate.Export(X509ContentType.Pfx));
		}
		catch (Exception ex) {
			throw new CryptographicException($"Failed to load private key: {ex.Message}");
		}
	}
#endif
}

public static class RsaExtensions {
#if NET48
	public static RSA ImportPrivateKeyFromFile(this RSA rsa, string privateKeyPath) {
		var (content, label) = LoadPemKeyFile(privateKeyPath);

		using var reader = new PemReader(new StringReader(string.Join(Environment.NewLine, content)));

		var keyParameters = reader.ReadObject() switch {
			RsaPrivateCrtKeyParameters parameters => parameters,
			AsymmetricCipherKeyPair keyPair       => keyPair.Private as RsaPrivateCrtKeyParameters,
			_                                     => throw new NotSupportedException($"Invalid private key format: {label}")
		};

		rsa.ImportParameters(DotNetUtilities.ToRSAParameters(keyParameters));

		return rsa;
	}
#else
	public static RSA ImportPrivateKeyFromFile(this RSA rsa, string privateKeyPath) {
		var (content, label) = LoadPemKeyFile(privateKeyPath);

		var privateKey      = string.Join(string.Empty, content[1..^1]);
		var privateKeyBytes = Convert.FromBase64String(privateKey);

		if (label == RsaPemLabels.Pkcs8PrivateKey)
			rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
		else if (label == RsaPemLabels.RSAPrivateKey)
			rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

		return rsa;
	}
#endif

	static (string[] Content, string Label) LoadPemKeyFile(string privateKeyPath) {
		var content = File.ReadAllLines(privateKeyPath);
		var label   = RsaPemLabels.ParseKeyLabel(content[0]);

		if (RsaPemLabels.IsEncryptedPrivateKey(label))
			throw new NotSupportedException("Encrypted private keys are not supported");

		return (content, label);
	}
}

static class RsaPemLabels {
	public const string RSAPrivateKey            = "RSA PRIVATE KEY";
	public const string Pkcs8PrivateKey          = "PRIVATE KEY";
	public const string EncryptedPkcs8PrivateKey = "ENCRYPTED PRIVATE KEY";

	public static readonly string[] PrivateKeyLabels = [RSAPrivateKey, Pkcs8PrivateKey, EncryptedPkcs8PrivateKey];

	public static bool IsPrivateKey(string label) => Array.IndexOf(PrivateKeyLabels, label) != -1;

	public static bool IsEncryptedPrivateKey(string label) => label == EncryptedPkcs8PrivateKey;

	const string LabelPrefix = "-----BEGIN ";
	const string LabelSuffix = "-----";

	public static string ParseKeyLabel(string pemFileHeader) {
		var label = pemFileHeader.Replace(LabelPrefix, string.Empty).Replace(LabelSuffix, string.Empty);

		if (!IsPrivateKey(label))
			throw new CryptographicException($"Unknown private key label: {label}");

		return label;
	}
}