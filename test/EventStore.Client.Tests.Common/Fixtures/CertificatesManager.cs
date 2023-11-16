using Ductus.FluentDocker.Builders;

namespace EventStore.Client.Tests;

static class CertificatesManager {
	static readonly DirectoryInfo CertificateDirectory;

	static CertificatesManager() => CertificateDirectory = new(Path.Combine(Environment.CurrentDirectory, GlobalEnvironment.UseCluster ? "certs-cluster" : "certs"));

	public static void VerifyCertificatesExist(string certificatePath) {
		var certificateFiles = new[] {
			Path.Combine("ca", "ca.crt"),
			Path.Combine("ca", "ca.key"),
			Path.Combine("node", "node.crt"),
			Path.Combine("node", "node.key")
		}.Select(path => Path.Combine(certificatePath, path));

		foreach (var file in certificateFiles)
			if (!File.Exists(file))
				throw new InvalidOperationException(
					$"Could not locate the certificates file {file} needed to run EventStoreDB. Please run the 'gencert' tool at the root of the repository."
				);
	}
	
	/// <summary>
	/// SS: not ready yet.
	/// </summary>
	static async Task<DirectoryInfo> EnsureCertificatesExist(DirectoryInfo? certificateDirectory = null) {
		certificateDirectory ??= CertificateDirectory;

		if (!certificateDirectory.Exists)
			certificateDirectory.Create();

		var caCertificatePath = Path.Combine(certificateDirectory.FullName, "ca");
		if (!Directory.Exists(caCertificatePath))
			await GenerateCertificates(
				certificateDirectory.FullName,
				"A CA certificate & key file have been generated in the '/tmp/ca/' directory",
				"create-ca", "-out", "/tmp/ca"
			);

		var nodeCertificatePath = Path.Combine(certificateDirectory.FullName, "node");
		if (!Directory.Exists(nodeCertificatePath))
			await GenerateCertificates(
				certificateDirectory.FullName,
				"A node certificate & key file have been generated in the '/tmp/node' directory.",
				"create-node",
				"-ca-certificate", "/tmp/ca/ca.crt",
				"-ca-key", "/tmp/ca/ca.key",
				"-out", "/tmp/node",
				"-ip-addresses", "127.0.0.1",
				"-dns-names", "localhost"
			);

		static Task GenerateCertificates(string sourceFolder, string expectedLogMessage, string command, params string[] commandArgs) {
			using var container = new Builder()
				.UseContainer()
				.UseImage("ghcr.io/eventstore/es-gencert-cli:1.3")
				.MountVolume(sourceFolder, "/tmp", Ductus.FluentDocker.Model.Builders.MountType.ReadWrite)
				// .MountVolume(Options.CertificateDirectory.FullName, "/etc/eventstore/certs", MountType.ReadOnly)
				.Command(command, commandArgs)
				.WaitForMessageInLog(expectedLogMessage, TimeSpan.FromSeconds(5))
				.Build();

			container.Start();

			return Task.CompletedTask;
		}

		VerifyCertificatesExist(certificateDirectory.FullName);

		return certificateDirectory;
	}

}
