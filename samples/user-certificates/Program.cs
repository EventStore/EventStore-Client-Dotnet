await ClientWithUserCertificates();

return;

static async Task ClientWithUserCertificates() {
	try {
		# region client-with-user-certificates

		const string userCertFile = "/path/to/user.crt";
		const string userKeyFile  = "/path/to/user.key";

		var settings = KurrentClientSettings.Create(
			$"esdb://localhost:2113/?tls=true&tlsVerifyCert=true&userCertFile={userCertFile}&userKeyFile={userKeyFile}"
		);

		await using var client = new KurrentClient(settings);

		# endregion client-with-user-certificates
	} catch (InvalidClientCertificateException) {
		// ignore for sample purposes
	}
}
