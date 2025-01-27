using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventStore.Client.Serialization;

namespace Kurrent.Client.Core.Serialization;

public class SystemTextJsonSerializationSettings : JsonSerializationSettings {
	public static readonly JsonSerializerOptions DefaultJsonSerializerOptions =
		new JsonSerializerOptions(JsonSerializerOptions.Default) {
			PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
			DictionaryKeyPolicy         = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = false,
			DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
			UnknownTypeHandling         = JsonUnknownTypeHandling.JsonNode,
			UnmappedMemberHandling      = JsonUnmappedMemberHandling.Skip,
			NumberHandling              = JsonNumberHandling.AllowReadingFromString,
			Converters = {
				new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
			}
		};

	public override SchemaDefinitionType SchemaType { get => SchemaDefinitionType.Json; }

	public JsonSerializerOptions Options { get; set; } = DefaultJsonSerializerOptions;
}

public class SystemTextJsonSerializer(SystemTextJsonSerializationSettings? options = null) : ISerializer {
	readonly JsonSerializerOptions _options = options?.Options ?? SystemTextJsonSerializationSettings.DefaultJsonSerializerOptions;

	public ReadOnlyMemory<byte> Serialize(object value) {
		return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, _options));
	}

	public object? Deserialize(ReadOnlyMemory<byte> data, Type type) {
		return JsonSerializer.Deserialize(data.Span, type, _options);
	}
}
