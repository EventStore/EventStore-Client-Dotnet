using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client.Projections;
using Google.Protobuf.WellKnownTypes;
using Type = System.Type;

namespace EventStore.Client {
	public partial class EventStoreProjectionManagementClient {
        static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions();
        
		/// <summary>
		/// Gets the result of a projection as an untyped document.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="partition"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<JsonDocument> GetResultAsync(string name, string? partition = null,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var value = await GetResultInternalAsync(name, partition, deadline, userCredentials, cancellationToken)
				.ConfigureAwait(false);

			await using var stream = new MemoryStream();
			await using var writer = new Utf8JsonWriter(stream);
			var serializer = new ValueSerializer();
			serializer.Write(writer, value, DefaultJsonSerializerOptions);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
			stream.Position = 0;

			return JsonDocument.Parse(stream);
		}

		/// <summary>
		/// Gets the result of a projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="partition"></param>
		/// <param name="serializerOptions"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public async Task<T> GetResultAsync<T>(string name, string? partition = null,
			JsonSerializerOptions? serializerOptions = null, 
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var value = await GetResultInternalAsync(name, partition, deadline, userCredentials, cancellationToken)
				.ConfigureAwait(false);
			await using var stream = new MemoryStream();
			await using var writer = new Utf8JsonWriter(stream);
			var serializer = new ValueSerializer();
			serializer.Write(writer, value, DefaultJsonSerializerOptions);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
			stream.Position = 0;

			return JsonSerializer.Deserialize<T>(stream.ToArray(), serializerOptions)!;
		}

		private async ValueTask<Value> GetResultInternalAsync(string name, string? partition,
			TimeSpan? deadline, UserCredentials? userCredentials, CancellationToken cancellationToken) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Projections.Projections.ProjectionsClient(
				channelInfo.CallInvoker).ResultAsync(new ResultReq {
				Options = new ResultReq.Types.Options {
					Name = name,
					Partition = partition ?? string.Empty
				}
			}, EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));

			var response = await call.ResponseAsync.ConfigureAwait(false);
			return response.Result;
		}

		/// <summary>
		/// Gets the state of a projection as an untyped document.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="partition"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<JsonDocument> GetStateAsync(string name, string? partition = null,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var value = await GetStateInternalAsync(name, partition, deadline, userCredentials, cancellationToken)
				.ConfigureAwait(false);

			await using var stream = new MemoryStream();
			await using var writer = new Utf8JsonWriter(stream);
			var serializer = new ValueSerializer();
			serializer.Write(writer, value, DefaultJsonSerializerOptions);
			stream.Position = 0;
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

			return JsonDocument.Parse(stream);
		}

		/// <summary>
		/// Gets the state of a projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="partition"></param>
		/// <param name="serializerOptions"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public async Task<T> GetStateAsync<T>(string name, string? partition = null,
			JsonSerializerOptions? serializerOptions = null, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) {
			var value = await GetStateInternalAsync(name, partition, deadline, userCredentials, cancellationToken)
				.ConfigureAwait(false);

			await using var stream = new MemoryStream();
			await using var writer = new Utf8JsonWriter(stream);
			var serializer = new ValueSerializer();
			serializer.Write(writer, value, DefaultJsonSerializerOptions);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
			stream.Position = 0;

			return JsonSerializer.Deserialize<T>(stream.ToArray(), serializerOptions)!;
		}

		private async ValueTask<Value> GetStateInternalAsync(string name, string? partition, TimeSpan? deadline,
			UserCredentials? userCredentials, CancellationToken cancellationToken) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Projections.Projections.ProjectionsClient(
				channelInfo.CallInvoker).StateAsync(new StateReq {
				Options = new StateReq.Types.Options {
					Name = name,
					Partition = partition ?? string.Empty
				}
			}, EventStoreCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));

			var response = await call.ResponseAsync.ConfigureAwait(false);
			return response.State;
		}

		private class ValueSerializer : System.Text.Json.Serialization.JsonConverter<Value> {
			public override Value Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
				throw new NotSupportedException();

			public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options) {
				switch (value.KindCase) {
					case Value.KindOneofCase.None:
						break;
					case Value.KindOneofCase.BoolValue:
						writer.WriteBooleanValue(value.BoolValue);
						break;
					case Value.KindOneofCase.NullValue:
						writer.WriteNullValue();
						break;
					case Value.KindOneofCase.NumberValue:
						writer.WriteNumberValue(value.NumberValue);
						break;
					case Value.KindOneofCase.StringValue:
						writer.WriteStringValue(value.StringValue);
						break;
					case Value.KindOneofCase.ListValue:
						writer.WriteStartArray();
						foreach (var item in value.ListValue.Values) {
							Write(writer, item, options);
						}

						writer.WriteEndArray();
						break;
					case Value.KindOneofCase.StructValue:
						writer.WriteStartObject();
						foreach (var (name, item) in value.StructValue.Fields) {
							writer.WritePropertyName(name);
							Write(writer, item, options);
						}

						writer.WriteEndObject();
						break;
				}
			}
		}
	}
}
