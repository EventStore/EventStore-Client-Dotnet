using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Projections;

#nullable enable
namespace EventStore.Client {
	public partial class EventStoreProjectionManagementClient {
		public async Task UpdateAsync(string name, string query, bool? emitEnabled = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			var options = new UpdateReq.Types.Options {
				Name = name,
				Query = query
			};
			if (emitEnabled.HasValue) {
				options.EmitEnabled = emitEnabled.Value;
			} else {
				options.NoEmitOptions = new Empty();
			}

			using var call = _client.UpdateAsync(new UpdateReq {
				Options = options
			}, EventStoreCallOptions.Create(Settings, Settings.OperationOptions, userCredentials, cancellationToken));

			await call.ResponseAsync.ConfigureAwait(false);
		}
	}
}
