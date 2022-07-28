using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace secure_with_tls {
	// adding 				Thread.Sleep(1);  in leader replication service trysendlogbulk is enough to force this failure

	class DirtyReadTester {

		public async Task Run(EventStoreClient client, EventStoreOperationsClient operations) {
			Console.WriteLine(DateTime.Now + " subscribing to $all");

			var subscriptionTcs = new TaskCompletionSource<Position>();
			using var sub = client.SubscribeToAllAsync(FromAll.End, (s, evt, ct) => {
				Console.WriteLine(DateTime.Now + " received event in subscription {0}", evt.OriginalPosition.Value);
				subscriptionTcs.SetResult(evt.OriginalPosition.Value);
				return Task.CompletedTask;
			});

			Console.WriteLine(DateTime.Now + " writing...");
			var sw = Stopwatch.StartNew();
			var evt = new EventData(Uuid.NewUuid(), "testtype", Encoding.UTF8.GetBytes(@$"{{""data"": ""{new string('#', 1_000_000)}""}}"));
			var result = await client.AppendToStreamAsync(streamName: "test", StreamState.Any, new[] { evt });
			Console.WriteLine(DateTime.Now + " written {0} :: {1} in {2}", result.LogPosition, result.NextExpectedStreamRevision, sw.Elapsed);

			await subscriptionTcs.Task;

			Console.WriteLine(DateTime.Now + " shuttingdown...");
			await operations.ShutdownAsync();
			Console.WriteLine(DateTime.Now + " shut down.");

			if (result.LogPosition != subscriptionTcs.Task.Result)
				throw new Exception("oops, received different event in subscription than write");

			// leader is now shut down, we try to read the write, which will involve reconnecting to a follower
			var emptyReadCount = 0;
			while (emptyReadCount < 10) {
				try {
					Console.WriteLine(DateTime.Now + " reading...");
					var allRead = client.ReadAllAsync(Direction.Forwards, result.LogPosition, maxCount: 1);
					await foreach (var x in allRead) {
						Console.WriteLine(DateTime.Now + " successfully read event {0}@{1} at {2}. Dataloss NOT detected.",
							x.OriginalEvent.EventNumber, x.OriginalEvent.EventStreamId, result.LogPosition);
						return;
					}

					// this is a successful replication of the problem. we get an empty read rather than an error because
					// the writer checkpoint is still pointing at this point on the new leader so its a legal read i think.
					// if we wrote to the cluster in the mean time then that would cause the invalid position errors
					Console.WriteLine(DateTime.Now + " empty read at {0}.",
						result.LogPosition);
					emptyReadCount++;
					await Task.Delay(1000);

				} catch (Exception ex) {
					Console.WriteLine(DateTime.Now + " failed to read event at {0}. {1}: {2}",
						result.LogPosition, ex.GetType(), string.Concat(ex.Message.TakeWhile(x => x != '-')));
					await Task.Delay(500);
				}
			}

			Console.WriteLine(DateTime.Now + " really couldn't read the event. DATALOSS DETECTED");
		}
	}
}
