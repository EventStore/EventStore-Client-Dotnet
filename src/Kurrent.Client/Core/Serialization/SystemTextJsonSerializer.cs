using System.Text;
using System.Text.Json;
using EventStore.Client.Serialization;

namespace Kurrent.Client.Core.Serialization;

public class SystemTextJsonSerializer: ISerializer {
	public ReadOnlyMemory<byte> Serialize(object value) {
		return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
	}
	public object? Deserialize(ReadOnlyMemory<byte> data, Type type) {
		return JsonSerializer.Deserialize(data.Span, type);
	}
}
