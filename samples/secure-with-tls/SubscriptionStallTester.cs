using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;
using EventTypeFilter = EventStore.Client.EventTypeFilter;

namespace secure_with_tls {

	//			if (msg.Expires < DateTime.UtcNow || DateTime.UtcNow.Ticks % 3 != 0) {
	//				if (msg.ReplyOnExpired) {


	class SubscriptionStallTester {

		public async Task Run(EventStoreClient client) {
			//Console.WriteLine(DateTime.Now + " filtered subscribing to $all");

			//using var sub = client.SubscribeToAllAsync(
			//	FromAll.Start,
			//	(s, evt, ct) => {
			//		Console.WriteLine(DateTime.Now + " received event in subscription {0}", evt.OriginalPosition.Value);
			//		return Task.CompletedTask;
			//	},
			//	filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()));

			//var subscriptionTcs = new TaskCompletionSource<Position>();
			//await subscriptionTcs.Task;




			Console.WriteLine(DateTime.Now + " subscribing to $all");

			using var sub = client.SubscribeToAllAsync(
				start: FromAll.Start,
				eventAppeared: (s, evt, ct) => {
					Console.WriteLine(DateTime.Now + " received event in subscription {0}", evt.OriginalPosition.Value);
					return Task.CompletedTask;
				},
				subscriptionDropped: (s, reason, ex) => {
					Console.WriteLine($"dropped {reason} {ex}");
				});

			var subscriptionTcs = new TaskCompletionSource<Position>();
			await subscriptionTcs.Task;

			Console.WriteLine(DateTime.Now + " done");


			//Console.WriteLine(DateTime.Now + " subscribing to stream");

			//using var sub = client.SubscribeToStreamAsync(
			//	"f",
			//	FromStream.Start,
			//	(s, evt, ct) => {
			//		Console.WriteLine(DateTime.Now + " received event in subscription {0}", evt.OriginalEventNumber);
			//		return Task.CompletedTask;
			//	},
			//	false,
			//	(s, r, ex) => Console.WriteLine("dropped {0} {1}", r, ex));

			//var subscriptionTcs = new TaskCompletionSource<Position>();
			//await subscriptionTcs.Task;


			// 1. replicate the stall [tick]
			// 2. verify that the fix fixes it [tick]
			// 3. verify that the fix gives us the same events.
			// - stream [tick]
			// - all
			// - all filtered [tick]
			// 4. verify that the transition to live works too
			// - stream
			// - all
			// - all filtered
		}
	}
}
