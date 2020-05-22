using Grpc.Core;

#nullable enable
namespace EventStore.Client {
	internal static class RequestMetadata {
		public static Metadata Create(UserCredentials? userCredentials) =>
			userCredentials == null
				? new Metadata()
				: new Metadata {
					new Metadata.Entry(Constants.Headers.Authorization, userCredentials.ToString())
				};
	}
}
