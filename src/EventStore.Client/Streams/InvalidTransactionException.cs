using System;
using System.Runtime.Serialization;

namespace EventStore.Client; 

/// <summary>
/// Exception thrown if there is an attempt to operate inside a
/// transaction which does not exist.
/// </summary>
public class InvalidTransactionException : Exception {
    /// <summary>
    /// Constructs a new <see cref="InvalidTransactionException"/>.
    /// </summary>
    public InvalidTransactionException() { }

    /// <summary>
    /// Constructs a new <see cref="InvalidTransactionException"/>.
    /// </summary>
    public InvalidTransactionException(string message) : base(message) { }

    /// <summary>
    /// Constructs a new <see cref="InvalidTransactionException"/>.
    /// </summary>
    public InvalidTransactionException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Constructs a new <see cref="InvalidTransactionException"/>.
    /// </summary>
    [Obsolete("Obsolete")]
    protected InvalidTransactionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}