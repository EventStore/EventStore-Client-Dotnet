namespace EventStore.Client.Tests;

public class X509CertificatesTests {
	[Fact]
	public void create_from_pem_file() {
		var expectedIssuer       = "CN=EventStoreDB CA 6da0684ec4f88a784c51a58d45752dec, O=Event Store Ltd, C=UK";
		var expectedSerialNumber = "6DA0684EC4F88A784C51A58D45752DEC";
		
		var certPemFilePath = "certs/ca/ca.crt";
		var keyPemFilePath  = "certs/ca/ca.key";

		var rsa = X509Certificates.CreateFromPemFile(certPemFilePath, keyPemFilePath);

		rsa.Issuer.ShouldBe(expectedIssuer);
		rsa.SerialNumber.ShouldBe(expectedSerialNumber);
	}
	
	[Fact]
	public void create_from_pem_file_() {
		var expectedIssuer       = "CN=EventStoreDB CA 6da0684ec4f88a784c51a58d45752dec, O=Event Store Ltd, C=UK";
		var expectedSubject      = "CN=admin";
		var expectedSerialNumber = "00CA52802A7EE35DE8080CACCE49724DAE";
		
		var certPemFilePath      = "certs/user-admin/user-admin.crt";
		var keyPemFilePath       = "certs/user-admin/user-admin.key";

		var rsa = X509Certificates.CreateFromPemFile(certPemFilePath, keyPemFilePath);
		
		rsa.Issuer.ShouldBe(expectedIssuer);
		rsa.Subject.ShouldBe(expectedSubject);
		rsa.SerialNumber.ShouldBe(expectedSerialNumber);
	}
}