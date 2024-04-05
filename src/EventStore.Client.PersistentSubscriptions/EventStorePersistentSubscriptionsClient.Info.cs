#nullable enable
using EventStore.Client.PersistentSubscriptions;
using Grpc.Core;

namespace EventStore.Client;

partial class EventStorePersistentSubscriptionsClient {
	/// <summary>
	/// Gets the status of a persistent subscription to $all
	/// </summary>
	public async Task<PersistentSubscriptionInfo> GetInfoToAllAsync(
		string groupName, TimeSpan? deadline = null,
		UserCredentials? userCredentials = null, CancellationToken cancellationToken = default
	) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		if (channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsGetInfo) {
			var req = new GetInfoReq() {
				Options = new GetInfoReq.Types.Options {
					GroupName = groupName,
					All       = new Empty()
				}
			};

			return await GetInfoGrpcAsync(req, deadline, userCredentials, channelInfo.CallInvoker, cancellationToken)
				.ConfigureAwait(false);
		}

		throw new NotSupportedException("The server does not support getting persistent subscription details for $all");
	}

	/// <summary>
	/// Gets the status of a persistent subscription to a stream
	/// </summary>
	public async Task<PersistentSubscriptionInfo> GetInfoToStreamAsync(
		string streamName, string groupName,
		TimeSpan? deadline = null, UserCredentials? userCredentials = null, CancellationToken cancellationToken = default
	) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		if (channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsGetInfo) {
			var req = new GetInfoReq() {
				Options = new GetInfoReq.Types.Options {
					GroupName        = groupName,
					StreamIdentifier = streamName
				}
			};

			return await GetInfoGrpcAsync(req, deadline, userCredentials, channelInfo.CallInvoker, cancellationToken)
				.ConfigureAwait(false);
		}

		return await GetInfoHttpAsync(streamName, groupName, channelInfo, deadline, userCredentials, cancellationToken)
			.ConfigureAwait(false);
	}

	async Task<PersistentSubscriptionInfo> GetInfoGrpcAsync(
		GetInfoReq req, TimeSpan? deadline,
		UserCredentials? userCredentials, CallInvoker callInvoker, CancellationToken cancellationToken
	) {
		var result = await new PersistentSubscriptions.PersistentSubscriptions.PersistentSubscriptionsClient(callInvoker)
			.GetInfoAsync(req, EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken))
			.ConfigureAwait(false);

		return PersistentSubscriptionInfo.From(result.SubscriptionInfo);
	}

	async Task<PersistentSubscriptionInfo> GetInfoHttpAsync(
		string streamName, string groupName,
		ChannelInfo channelInfo, TimeSpan? deadline, UserCredentials? userCredentials, CancellationToken cancellationToken
	) {
		var path = $"/subscriptions/{UrlEncode(streamName)}/{UrlEncode(groupName)}/info";
		var result = await HttpGet<PersistentSubscriptionDto>(
				path,
				() => throw new PersistentSubscriptionNotFoundException(streamName, groupName),
				channelInfo, deadline, userCredentials, cancellationToken
			)
			.ConfigureAwait(false);

		return PersistentSubscriptionInfo.From(result);
	}
}