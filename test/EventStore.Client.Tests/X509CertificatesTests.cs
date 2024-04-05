using System.Security.Cryptography.X509Certificates;

namespace EventStore.Client.Tests;

public class X509CertificatesTests {
	[Fact]
	public void create_from_pem_file() {
		const string certPemFilePath = "certs/ca/ca.crt";
		const string keyPemFilePath  = "certs/ca/ca.key";

		var rsa = X509Certificates.CreateFromPemFile(certPemFilePath, keyPemFilePath);

		var cert = new X509Certificate2(certPemFilePath);

		rsa.Issuer.ShouldBe(cert.Issuer);
		rsa.SerialNumber.ShouldBe(cert.SerialNumber);
	}

	[Fact]
	public void create_from_pem_file_() {
		const string certPemFilePath = "certs/user-admin/user-admin.crt";
		const string keyPemFilePath  = "certs/user-admin/user-admin.key";

		var rsa = X509Certificates.CreateFromPemFile(certPemFilePath, keyPemFilePath);

		var cert = new X509Certificate2(certPemFilePath);

		rsa.Issuer.ShouldBe(cert.Issuer);
		rsa.Subject.ShouldBe(cert.Subject);
		rsa.SerialNumber.ShouldBe(cert.SerialNumber);
	}
}