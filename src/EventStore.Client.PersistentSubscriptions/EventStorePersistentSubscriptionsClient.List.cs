using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.PersistentSubscriptions;
using Grpc.Core;

#nullable enable
namespace EventStore.Client {
	partial class EventStorePersistentSubscriptionsClient {
		/// <summary>
		/// Lists persistent subscriptions to $all.
		/// </summary>
		public async Task<IEnumerable<PersistentSubscriptionInfo>> ListToAllAsync(TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			
			var channelInfo = await GetChannelInfo(userCredentials, cancellationToken).ConfigureAwait(false);
			if (channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsList) {
				var req = new ListReq() {
					Options = new ListReq.Types.Options{
						ListForStream = new ListReq.Types.StreamOption() {
							All = new Empty()
						}
					}
				};

				return await ListGrpcAsync(req, deadline, userCredentials, channelInfo.CallInvoker, cancellationToken)
					.ConfigureAwait(false);
			}
			
			throw new NotSupportedException("The server does not support listing the persistent subscriptions.");
		}

		/// <summary>
		/// Lists persistent subscriptions to the specified stream.
		/// </summary>
		public async Task<IEnumerable<PersistentSubscriptionInfo>> ListToStreamAsync(string streamName, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {

			var channelInfo = await GetChannelInfo(userCredentials, cancellationToken).ConfigureAwait(false);
			if (channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsList) {
				var req = new ListReq() {
					Options = new ListReq.Types.Options {
						ListForStream = new ListReq.Types.StreamOption() {
							Stream = streamName
						}
					}
				};
				
				return await ListGrpcAsync(req, deadline, userCredentials, channelInfo.CallInvoker, cancellationToken)
					.ConfigureAwait(false);
			}
			
			return await ListHttpAsync(streamName, channelInfo, deadline, userCredentials, cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Lists all persistent subscriptions.
		/// </summary>
		public async Task<IEnumerable<PersistentSubscriptionInfo>> ListAllAsync(TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {

			var channelInfo = await GetChannelInfo(userCredentials, cancellationToken).ConfigureAwait(false);
			if (channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsList) {
				var req = new ListReq() {
					Options = new ListReq.Types.Options {
						ListAllSubscriptions = new Empty()
					}
				};
				
				return await ListGrpcAsync(req, deadline, userCredentials, channelInfo.CallInvoker, cancellationToken)
					.ConfigureAwait(false);
			}

			try {
				var result = await HttpGet<IList<PersistentSubscriptionDto>>("/subscriptions",
						onNotFound: () => throw new PersistentSubscriptionNotFoundException(string.Empty, string.Empty),
						channelInfo, deadline, userCredentials, cancellationToken)
					.ConfigureAwait(false);

				return result.Select(PersistentSubscriptionInfo.From);
			} catch (AccessDeniedException ex) when (userCredentials != null) { // Required to get same gRPC behavior.
				throw new NotAuthenticatedException(ex.Message, ex);
			}
		}
		
		private async Task<IEnumerable<PersistentSubscriptionInfo>> ListGrpcAsync(ListReq req, TimeSpan? deadline,
			UserCredentials? userCredentials, CallInvoker callInvoker, CancellationToken cancellationToken) {
			
			using var call = new PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient(callInvoker)
				.ListAsync(req, EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));

			ListResp? response = await call.ResponseAsync.ConfigureAwait(false);

			return response.Subscriptions.Select(PersistentSubscriptionInfo.From);
		}
		
		private async Task<IEnumerable<PersistentSubscriptionInfo>> ListHttpAsync(string streamName,
			ChannelInfo channelInfo, TimeSpan? deadline, UserCredentials? userCredentials, CancellationToken cancellationToken) {

			var path = $"/subscriptions/{UrlEncode(streamName)}";
			var result = await HttpGet<IList<PersistentSubscriptionDto>>(path,
					onNotFound: () => throw new PersistentSubscriptionNotFoundException(streamName, string.Empty),
					channelInfo, deadline, userCredentials, cancellationToken)
				.ConfigureAwait(false);
			return result.Select(PersistentSubscriptionInfo.From);
		}
	}
}
