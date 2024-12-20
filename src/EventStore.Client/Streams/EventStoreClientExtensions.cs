using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Client {
	/// <summary>
	///  A set of extension methods for an <see cref="EventStoreClient"/>.
	/// </summary>
	public static class EventStoreClientExtensions {
		private static readonly JsonSerializerOptions SystemSettingsJsonSerializerOptions = new JsonSerializerOptions {
			Converters = {
				SystemSettingsJsonConverter.Instance
			},
		};

		/// <summary>
		/// Writes <see cref="SystemSettings"/> to the $settings stream.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="settings"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static Task SetSystemSettingsAsync(
			this EventStoreClient client,
			SystemSettings settings,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (client == null) throw new ArgumentNullException(nameof(client));
			return client.AppendToStreamAsync(SystemStreams.SettingsStream, StreamState.Any,
				new[] {
					new EventData(Uuid.NewUuid(), SystemEventTypes.Settings,
						JsonSerializer.SerializeToUtf8Bytes(settings, SystemSettingsJsonSerializerOptions))
				}, deadline: deadline, userCredentials: userCredentials, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Appends to a stream conditionally.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="streamName"></param>
		/// <param name="expectedRevision"></param>
		/// <param name="eventData"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static async Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(
			this EventStoreClient client,
			string streamName,
			StreamRevision expectedRevision,
			IEnumerable<EventData> eventData,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (client == null) {
				throw new ArgumentNullException(nameof(client));
			}
			try {
				var result = await client.AppendToStreamAsync(streamName, expectedRevision, eventData,
						options => options.ThrowOnAppendFailure = false, deadline, userCredentials, cancellationToken)
					.ConfigureAwait(false);
				return ConditionalWriteResult.FromWriteResult(result);
			} catch (StreamDeletedException) {
				return ConditionalWriteResult.StreamDeleted;
			}
		}

		/// <summary>
		/// Appends to a stream conditionally.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="streamName"></param>
		/// <param name="expectedState"></param>
		/// <param name="eventData"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static async Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(
			this EventStoreClient client,
			string streamName,
			StreamState expectedState,
			IEnumerable<EventData> eventData,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (client == null) {
				throw new ArgumentNullException(nameof(client));
			}
			try {
				var result = await client.AppendToStreamAsync(streamName, expectedState, eventData,
						options => options.ThrowOnAppendFailure = false, deadline, userCredentials, cancellationToken)
					.ConfigureAwait(false);
				return ConditionalWriteResult.FromWriteResult(result);
			} catch (StreamDeletedException) {
				return ConditionalWriteResult.StreamDeleted;
			} catch (WrongExpectedVersionException ex) {
				return ConditionalWriteResult.FromWrongExpectedVersion(ex);
			}
		}
	}
}
