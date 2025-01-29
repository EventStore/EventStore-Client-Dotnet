using System.Text.Json.Serialization;

namespace EventStore.Client.Serialization;

public record SerializationMetadata(
	[property: JsonPropertyName(SerializationMetadata.Constants.MessageTypeAssemblyQualifiedName)]
	string? MessageTypeAssemblyQualifiedName,
	[property: JsonPropertyName(SerializationMetadata.Constants.MessageTypeClrTypeName)]
	string? MessageTypeClrTypeName
) {
	public static readonly SerializationMetadata None = new SerializationMetadata(null, null);

	public bool IsValid =>
		MessageTypeAssemblyQualifiedName != null && MessageTypeClrTypeName != null;

	public static SerializationMetadata From(Type clrType) =>
		new SerializationMetadata(clrType.AssemblyQualifiedName, clrType.Name);

	public static class Constants {
		public const string MessageTypeAssemblyQualifiedName = "$clrTypeAssemblyQualifiedName";
		public const string MessageTypeClrTypeName           = "$clrTypeName";
	}
}
