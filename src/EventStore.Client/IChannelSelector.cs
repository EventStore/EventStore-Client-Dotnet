using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace EventStore.Client {
	internal interface IChannelSelector : IAsyncDisposable {
		Task<ChannelInfo> SelectChannel(CancellationToken cancellationToken);
		void SetEndPoint(EndPoint leader);
		void Rediscover();
	}
}
