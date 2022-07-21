using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace EventStore.Client {
	public static class ReadOnlyMemoryExtensions {
		public static Position ParsePosition(this ReadOnlyMemory<byte> json) {
			var checkPoint = json.ParseJson<string>();
			var parts = checkPoint.Split('/');
			var commitPosition = ulong.Parse(parts[0].Split(':')[1]);
			var preparePosition = ulong.Parse(parts[1].Split(':')[1]);
			return new Position(commitPosition, preparePosition);
		}

		public static StreamPosition ParseStreamPosition(this ReadOnlyMemory<byte> json) {
			var checkPoint = json.ParseJson<string>();
			return StreamPosition.FromInt64(int.Parse(checkPoint));
		}

		private static T ParseJson<T>(this ReadOnlyMemory<byte> json) =>
			JsonConvert.DeserializeObject<T>(UTF8NoBom.GetString(json.ToArray()), JsonSettings)!;

		private static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

		private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings {
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			NullValueHandling = NullValueHandling.Ignore,
			DefaultValueHandling = DefaultValueHandling.Ignore,
			MissingMemberHandling = MissingMemberHandling.Ignore,
			TypeNameHandling = TypeNameHandling.None,
			Converters = new JsonConverter[] {new StringEnumConverter()}
		};
	}
}
