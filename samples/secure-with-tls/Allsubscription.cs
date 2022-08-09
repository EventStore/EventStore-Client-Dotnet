using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace secure_with_tls {
	// when running this we expect to see
	// 1. no out of order events (such an event will stop the process with an error)
	// 2. no stalled subscriptions - currently this is not detected, they just stop outputting
	class AllSubscription {

		public async Task Run(EventStoreClient leaderClient, EventStoreClient followerClient, EventStoreClient readOnlyReplicaClient) {
			Console.WriteLine(DateTime.Now + " subscribing!");

			var appendClient = leaderClient;
			var subscribeClient = readOnlyReplicaClient;

			// broken sub doesn't process anything it receives
			await MultiSubscribeAsync("broken", subscribeClient, () => async (name, evt) => {
				await new TaskCompletionSource<bool>().Task;
			});

			// slow sub processes one event each second
			await MultiSubscribeAsync("slow", subscribeClient, () => async (name, evt) => {
				Console.WriteLine(DateTime.Now + " {0} {1} {2}", name, evt.Event.EventNumber, evt.OriginalPosition);
				await Task.Delay(500);
			});

			// fast sub drops everything on the floor
			await MultiSubscribeAsync("fast", subscribeClient, () => {
				var count = 0L;
				return (name, evt) => {
					if (count++ % 500 == 0)
						Console.WriteLine(DateTime.Now + "                     {0} {1} {2}", name, evt.Event.EventNumber, evt.OriginalPosition);
					return Task.CompletedTask;
				};
			});

			// bursty sub goes fast and slow. alternating
			await MultiSubscribeAsync("bursty", subscribeClient, () => {
				var sw = Stopwatch.StartNew();
				var count = 0L;
				return async (name, evt) => {
					if (sw.ElapsedMilliseconds > 1000) {
						await Task.Delay(1000);
						sw.Restart();
					}
					if (count++ % 500 == 0)
						Console.WriteLine(DateTime.Now + "                                                {0} {1} {2}", name, evt.Event.EventNumber, evt.OriginalPosition);
				};
			});

			Console.WriteLine(DateTime.Now + " subscribed");

			var r = new Random();
			while (true) {
				var evts = Enumerable
					.Range(0, r.Next(100))
					.Select(_ => new EventData(Uuid.NewUuid(), "testtype", Encoding.UTF8.GetBytes(@$"{{""data"": ""{new string('#', 1)}""}}")))
					.ToArray();
				var result = await subscribeClient.AppendToStreamAsync(streamName: "test", StreamState.Any, evts);
				await Task.Delay(1);
			}
		}

		private async Task MultiSubscribeAsync(
			string name,
			EventStoreClient client,
			Func<Func<string, ResolvedEvent, Task>> genEventAppeared) {

			await SubscribeToStreamAsync($"{name}-stream", client, genEventAppeared());
			await SubscribeToAllAsync($"{name}-all", client, genEventAppeared());
			await SubscribeToAllFilteredAsync($"{name}-allfiltered", client, genEventAppeared());
		}

		private async Task SubscribeToStreamAsync(
			string name,
			EventStoreClient client,
			Func<string, ResolvedEvent, Task> eventAppeared,
			EventOrderTracker tracker = null,
			StreamPosition? checkpoint = null) {

			tracker ??= new(name);
			var start = checkpoint == null ? FromStream.Start : FromStream.After(checkpoint.Value);

			while (true) {
				try {
					await client.SubscribeToStreamAsync(
						"test",
						start: start,
						eventAppeared: async (s, evt, ct) => {
							checkpoint = evt.Event.EventNumber;
							tracker.OnEvent(evt);
							await eventAppeared(name, evt);
						},
						subscriptionDropped: (s, reason, ex) => {
							Console.WriteLine(DateTime.Now + $" {name} was dropped {reason} {ex.Message}. Resubscribing...");
							_ = SubscribeToStreamAsync(name, client, eventAppeared, tracker, checkpoint);
						});
					return;
				} catch (Exception ex) {
					Console.WriteLine(DateTime.Now + $" {name} failed to subscribe. Retrying. ex: {ex}");
				}
			}
		}

		private async Task SubscribeToAllAsync(
			string name,
			EventStoreClient client,
			Func<string, ResolvedEvent, Task> eventAppeared,
			EventOrderTracker tracker = null,
			Position? checkpoint = null) {

			tracker ??= new(name);
			var start = checkpoint == null ? FromAll.Start : FromAll.After(checkpoint.Value);

			while (true) {
				try {
					await client.SubscribeToAllAsync(
						start: start,
						eventAppeared: async (s, evt, ct) => {
							checkpoint = evt.OriginalPosition;
							tracker.OnEvent(evt);
							await eventAppeared(name, evt);
						},
						subscriptionDropped: (s, reason, ex) => {
							Console.WriteLine(DateTime.Now + $" {name} was dropped {reason} {ex.Message}. Resubscribing...");
							_ = SubscribeToAllAsync(name, client, eventAppeared, tracker, checkpoint);
						});
					return;
				} catch (Exception ex) {
					Console.WriteLine(DateTime.Now + $" {name} failed to subscribe. Retrying. ex: {ex}");
				}
			}
		}

		private async Task SubscribeToAllFilteredAsync(
			string name,
			EventStoreClient client,
			Func<string, ResolvedEvent, Task> eventAppeared,
			EventOrderTracker tracker = null,
			Position? checkpoint = null) {

			tracker ??= new(name);
			var start = checkpoint == null ? FromAll.Start : FromAll.After(checkpoint.Value);

			while (true) {
				try {
					await client.SubscribeToAllAsync(
						start: start,
						eventAppeared: async (s, evt, ct) => {
							if (checkpoint == null || checkpoint < evt.OriginalPosition)
								checkpoint = evt.OriginalPosition;
							tracker.OnEvent(evt);
							await eventAppeared(name, evt);
						},
						subscriptionDropped: (s, reason, ex) => {
							Console.WriteLine(DateTime.Now + $" {name} was dropped {reason} {ex.Message}. Resubscribing...");
							_ = SubscribeToAllFilteredAsync(name, client, eventAppeared, tracker, checkpoint);
						},
						filterOptions: new SubscriptionFilterOptions(
							EventStore.Client.EventTypeFilter.ExcludeSystemEvents(),
							checkpointReached: (s, p, ct) => {
								if (checkpoint == null || checkpoint < p)
									checkpoint = p;
								return Task.CompletedTask;
							}));
					return;
				} catch (Exception ex) {
					Console.WriteLine(DateTime.Now + $" {name} failed to subscribe. Retrying. ex: {ex}");
				}
			}
		}
	}

	// makes sure the events are in order and not missing any. uses events in the test stream for this
	class EventOrderTracker {
		private readonly string _name;

		public EventOrderTracker(string name) {
			_name = name;
		}

		private StreamPosition _expectedEventNumber = StreamPosition.Start;

		public void OnEvent(ResolvedEvent evt) {
			if (evt.Event.EventStreamId == "test") {
				if (evt.Event.EventNumber != _expectedEventNumber) {
					Console.WriteLine(DateTime.Now + $" {_name} INVALID ORDER expected {_expectedEventNumber} actual {evt.Event.EventNumber}");
					Environment.Exit(1);
				}
				_expectedEventNumber = _expectedEventNumber.Next();
			}
		}
	}
}
