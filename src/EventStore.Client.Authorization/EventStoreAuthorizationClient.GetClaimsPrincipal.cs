using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Authorization;

using RpcClaimsPrincipal = EventStore.Client.Authorization.ClaimsPrincipal;
using ClaimsPrincipal = System.Security.Claims.ClaimsPrincipal;
using ClaimsIdentity = System.Security.Claims.ClaimsIdentity;
using Claim = System.Security.Claims.Claim;

namespace EventStore.Client {
	public sealed partial class EventStoreAuthorizationClient {
		/// <summary>
		/// Get the claims for the current or specified user
		/// </summary>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<ClaimsPrincipal> GetClaimsPrincipalAsync(
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Authorization.Authorization.AuthorizationClient(
				channelInfo.CallInvoker).GetClaimsAsync(new ClaimsReq (),
				EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			var response = await call.ResponseAsync.ConfigureAwait(false);
			return GetSystemClaimsPrincipal(response.ClaimsPrincipal);
		}
		
		private static ClaimsPrincipal GetSystemClaimsPrincipal(RpcClaimsPrincipal rpcClaimsPrincipal) {
			return new ClaimsPrincipal(
				identities: rpcClaimsPrincipal.Identities
					.Select(identity => identity.Claims
						.Select(claim => new Claim(
							type: claim.Type,
							value: claim.Value,
							valueType: claim.ValueType,
							issuer: claim.Issuer,
							originalIssuer: claim.OriginalIssuer)))
					.Select(claims => new ClaimsIdentity(claims, "Client")));
		}
	}
}
