using System.Net;
using System.Net.Sockets;

namespace EventStore.Client.Tests;

public class NetworkPortProvider(int port = 2113) {
	static readonly SemaphoreSlim Semaphore = new(1, 1);
	public async Task<int> GetNextAvailablePort(TimeSpan delay = default) {
		await Semaphore.WaitAsync();

		try {
			using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			while (true) {
				var nexPort = Interlocked.Increment(ref port);

				try {
					await socket.ConnectAsync(IPAddress.Any, nexPort);
				}
				catch (SocketException ex) {
					if (ex.SocketErrorCode is SocketError.ConnectionRefused or not SocketError.IsConnected) {
						return nexPort;
					}

					await Task.Delay(delay);
				}
				finally {
					if (socket.Connected) {
						await socket.DisconnectAsync(true);
					}
				}
			}
		}
		finally {
			Semaphore.Release();
		}
	}

	public int NextAvailablePort => GetNextAvailablePort(TimeSpan.FromMilliseconds(100)).GetAwaiter().GetResult();
}