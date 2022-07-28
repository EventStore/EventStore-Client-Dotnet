using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace secure_with_tls {
	// this is designed to test that the server is able to detect connection loss during
	//   - persistent subscription
	//   - subscription
	//   - read
	//   - batchappend
	// use clumsy or similar to silently break the connection by dropping all the packets
	// use the following server settings to make it likely that its the heartbeat that kills the connection
	// KeepAliveInterval: 1000
	// KeepAliveTimeout: 1000


	class HeartbeatTester {

		public async Task Run(
			EventStoreClient client,
			EventStorePersistentSubscriptionsClient persistentSubscriptions) {

			await RunSubscription(client);
			await RunRead(client);
			await RunPersistentSubscription(persistentSubscriptions);
			await RunBatchAppend(client);
		}

		public async Task RunBatchAppend(EventStoreClient client) {
			Console.WriteLine($"Disable packet loss and press a key to start APPEND test");
			Console.ReadKey();

			var tcs = new TaskCompletionSource<bool>();

			Console.WriteLine($"Started! Drop the packets now, expect to see server notice and tidy up.");

			try {
				var events = Enumerable.Range(1, 5000).Select(x => CreateEventData());
				while (true) {
					var result = await client.AppendToStreamAsync(
						streamName: "test",
						StreamState.Any,
						events,
						deadline: TimeSpan.FromHours(100));

					Console.WriteLine($"Wrote {result.NextExpectedStreamRevision}");
					await Task.Delay(100);
				}
			} catch (Exception ex) {
				Console.WriteLine($"Disconnected with {ex.GetType().Name} {ex.Message}.)");
			}
		}

		public async Task RunRead(EventStoreClient client) {
			Console.WriteLine($"Disable packet loss and press a key to start READ test");
			Console.ReadKey();

			var results = client.ReadAllAsync(
				Direction.Forwards,
				Position.Start,
				maxCount: long.MaxValue,
				deadline: TimeSpan.FromHours(100));

			Console.WriteLine($"Started! Drop the packets now, expect to see server notice and tidy up.");

			try {
				await foreach (var x in results) {
					Console.WriteLine("Received event");
					await Task.Delay(100);
				}
				Console.WriteLine($"Completed");
			} catch (Exception ex) {
				Console.WriteLine($"Disconnected with {ex.GetType().Name} {ex.Message}.)");
			}
		}

		public async Task RunSubscription(EventStoreClient client) {
			Console.WriteLine($"Disable packet loss and press a key to start SUBSCRIPTION test");
			Console.ReadKey();

			var tcs = new TaskCompletionSource<bool>();
			var sub = await client.SubscribeToStreamAsync(
				streamName: $"test-{Guid.NewGuid()}", // straight to live, otherwise we are in the same case as Read
				start: FromStream.Start,
				eventAppeared: (s, e, ct) => {
					Console.WriteLine("Received event");
					return Task.CompletedTask;
				},
				resolveLinkTos: false,
				subscriptionDropped: (s, reason, ex) => {
					Console.WriteLine($"Dropped with {ex.GetType().Name} {ex.Message}");
					tcs.SetResult(true);
				});

			Console.WriteLine($"Started! Drop the packets now, expect to see server notice and tidy up.");

			await tcs.Task;
		}

		public async Task RunPersistentSubscription(EventStorePersistentSubscriptionsClient client) {
			Console.WriteLine($"Disable packet loss and press a key to start PERSISTENT SUBSCRIPTION test");
			Console.ReadKey();

			var tcs = new TaskCompletionSource<bool>();

			var groupName = $"{Guid.NewGuid()}";
			await client.CreateAsync(
				streamName: "test",
				groupName: groupName,
				settings: new PersistentSubscriptionSettings());

			Console.WriteLine($"Created group {groupName}. Subscribing. ");

			var sub = await client.SubscribeToStreamAsync(
				streamName: "test",
				groupName: groupName,
				eventAppeared: (s, e, x, ct) => {
					Console.WriteLine("Received event");
					return Task.CompletedTask;
				},
				subscriptionDropped: (s, reason, ex) => {
					Console.WriteLine($"Dropped with {ex?.GetType().Name} {ex?.Message}");
					tcs.SetResult(true);
				});

			Console.WriteLine($"Started! Drop the packets now, expect to see server notice and tidy up.");

			await tcs.Task;
		}

		EventData CreateEventData(int length = 0) {
			var data = new string('#', length);
			return new EventData(
				Uuid.NewUuid(),
				"testtype",
				Encoding.UTF8.GetBytes(@$"{{""data"": ""{data}""}}"));
		}

	}
}
