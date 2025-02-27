// ReSharper disable InconsistentNaming

namespace Kurrent.Client.Tests;

public static class Certificates {
	static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
	const           string CertsFolder   = "certs";

	public static class TlsCa {
		public static string Absolute => GetAbsolutePath(CertsFolder, "ca", "ca.crt");
		public static string Relative => GetRelativePath(CertsFolder, "ca", "ca.crt");
	}

	public static class Admin {
		public static string CertAbsolute => GetAbsolutePath(CertsFolder, "user-admin", "user-admin.crt");
		public static string CertRelative => GetRelativePath(CertsFolder, "user-admin", "user-admin.crt");

		public static string KeyAbsolute => GetAbsolutePath(CertsFolder, "user-admin", "user-admin.key");
		public static string KeyRelative => GetRelativePath(CertsFolder, "user-admin", "user-admin.key");
	}

	public static class Invalid {
		public static string CertAbsolute => GetAbsolutePath(CertsFolder, "user-invalid", "user-invalid.crt");
		public static string CertRelative => GetRelativePath(CertsFolder, "user-invalid", "user-invalid.crt");

		public static string KeyAbsolute => GetAbsolutePath(CertsFolder, "user-invalid", "user-invalid.key");
		public static string KeyRelative => GetRelativePath(CertsFolder, "user-invalid", "user-invalid.key");
	}

	static string GetAbsolutePath(params string[] paths) => Path.Combine(BaseDirectory, Path.Combine(paths));
	static string GetRelativePath(params string[] paths) => Path.Combine(paths);
}
