using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace EventStore.Client {
	public static class EventStoreClientExtensions {
		private static readonly JsonSerializerOptions SystemSettingsJsonSerializerOptions = new JsonSerializerOptions {
			Converters = {
				SystemSettingsJsonConverter.Instance
			},
		};

		public static Task SetSystemSettingsAsync(
			this EventStoreClient client,
			SystemSettings settings,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			if (client == null) throw new ArgumentNullException(nameof(client));
			return client.AppendToStreamAsync(SystemStreams.SettingsStream, StreamState.Any,
				new[] {
					new EventData(Uuid.NewUuid(), SystemEventTypes.Settings,
						JsonSerializer.SerializeToUtf8Bytes(settings, SystemSettingsJsonSerializerOptions))
				}, userCredentials: userCredentials, cancellationToken: cancellationToken);
		}

		public static async Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(
			this EventStoreClient client,
			string streamName,
			StreamRevision expectedRevision,
			IEnumerable<EventData> eventData,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (client == null) throw new ArgumentNullException(nameof(client));
			try {
				var result = await client.AppendToStreamAsync(streamName, expectedRevision, eventData,
					userCredentials: userCredentials, cancellationToken: cancellationToken).ConfigureAwait(false);
				return ConditionalWriteResult.FromWriteResult(result);
			} catch (StreamDeletedException) {
				return ConditionalWriteResult.StreamDeleted;
			} catch (WrongExpectedVersionException ex) {
				return ConditionalWriteResult.FromWrongExpectedVersion(ex);
			}
		}

		public static async Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(
			this EventStoreClient client,
			string streamName,
			StreamState expectedState,
			IEnumerable<EventData> eventData,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (client == null) throw new ArgumentNullException(nameof(client));
			try {
				var result = await client.AppendToStreamAsync(streamName, expectedState, eventData,
					userCredentials: userCredentials, cancellationToken: cancellationToken).ConfigureAwait(false);
				return ConditionalWriteResult.FromWriteResult(result);
			} catch (StreamDeletedException) {
				return ConditionalWriteResult.StreamDeleted;
			} catch (WrongExpectedVersionException ex) {
				return ConditionalWriteResult.FromWrongExpectedVersion(ex);
			}
		}
	}
}
