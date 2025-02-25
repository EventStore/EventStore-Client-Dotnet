namespace Kurrent.Client.Core.Serialization;

/// <summary>
/// Defines the core serialization capabilities required by the KurrentDB client.
/// Implementations of this interface handle the conversion between .NET objects and their
/// binary representation for storage in and retrieval from the event store.
/// <br />
/// The client ships default System.Text.Json implementation, but custom implementations can be provided or other formats.
/// </summary>
public interface ISerializer {
    /// <summary>
    /// Converts a .NET object to its binary representation for storage in the event store.
    /// </summary>
    /// <param name="value">The object to serialize. This could be an event, command, or metadata object.</param>
    /// <returns>
    /// A binary representation of the object that can be stored in KurrentDB.
    /// </returns>
    public ReadOnlyMemory<byte> Serialize(object value);

    /// <summary>
    /// Reconstructs a .NET object from its binary representation retrieved from the event store.
    /// </summary>
    /// <param name="data">The binary data to deserialize, typically retrieved from a KurrentDB event.</param>
    /// <param name="type">The target .NET type to deserialize the data into, determined from message type mappings.</param>
    /// <returns>
    /// The deserialized object cast to the specified type, or null if the data cannot be deserialized.
    /// The returned object will be an instance of the specified type or a compatible subtype.
    /// </returns>
    public object? Deserialize(ReadOnlyMemory<byte> data, Type type);
}
