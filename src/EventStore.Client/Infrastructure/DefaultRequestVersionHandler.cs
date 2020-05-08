using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client
{
    internal class DefaultRequestVersionHandler : DelegatingHandler
    {
        public DefaultRequestVersionHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
            InnerHandler = innerHandler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Version = new Version(2, 0);
            return base.SendAsync(request, cancellationToken);
        }
    }
}