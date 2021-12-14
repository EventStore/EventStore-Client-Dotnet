using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

#nullable enable
namespace EventStore.Client {
	internal interface IChannelSelector : IAsyncDisposable {
		ValueTask<ChannelInfo> SelectChannel(CancellationToken cancellationToken);
		void Rediscover(DnsEndPoint? leader);
	}
}
