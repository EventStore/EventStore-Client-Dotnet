using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Authorization;
using ClaimsPrincipal = System.Security.Claims.ClaimsPrincipal;

using RpcOperation = EventStore.Client.Authorization.Operation;
using RpcOperationParameter = EventStore.Client.Authorization.OperationParameter;
using RpcClaimsPrincipal = EventStore.Client.Authorization.ClaimsPrincipal;
using RpcClaimsIdentity = EventStore.Client.Authorization.ClaimsIdentity;
using RpcClaim = EventStore.Client.Authorization.Claim;

namespace EventStore.Client {
	public sealed partial class EventStoreAuthorizationClient {
		/// <summary>
		/// Checks if an operation is authorized on the server with the specified claims.
		/// </summary>
		/// <param name="operation"></param>
		/// <param name="claimsPrincipal"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<bool> CheckAccessAsync(
			Operation operation,
			ClaimsPrincipal claimsPrincipal,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Authorization.Authorization.AuthorizationClient(
				channelInfo.CallInvoker).CheckAccessAsync(new CheckAccessReq {
					Operation = GetRpcOperation(operation),
					ClaimsPrincipal = GetRpcClaimsPrincipal(claimsPrincipal)
				},
				EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			var response = await call.ResponseAsync.ConfigureAwait(false);
			return response.IsAllowed;
		}

		private static RpcClaimsPrincipal GetRpcClaimsPrincipal(ClaimsPrincipal cp) {
			return new RpcClaimsPrincipal {
				Identities = {
					cp.Identities
						.Select(identity =>
							identity.Claims.Select(claim => new RpcClaim {
								Value = claim.Value,
								Type = claim.Type,
								ValueType = claim.ValueType,
								Issuer = claim.Issuer,
								OriginalIssuer = claim.OriginalIssuer
							}))
						.Select(rpcClaims => new RpcClaimsIdentity {
							Claims = { rpcClaims }
						})
				}
			};
		}

		private static RpcOperation GetRpcOperation(Operation op) {
			return new RpcOperation {
				Action = op.Action,
				Resource = op.Resource,
				Parameters = {
					op.Parameters.ToArray()
						.Select(p => new RpcOperationParameter {
							Name = p.Name,
							Value = p.Value
						})
				}
			};
		}
	}
}
