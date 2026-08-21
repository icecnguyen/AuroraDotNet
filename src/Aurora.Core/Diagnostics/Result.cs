using System;
using System.Collections.Generic;

namespace Aurora.Core.Diagnostics;

/// <summary>
/// Represents a success or failure result without throwing exceptions for normal control flow.
/// </summary>
public readonly struct Result<T> : IEquatable<Result<T>>
{
    public T? Value { get; }
    public Exception? Error { get; }
    public bool IsSuccess => Error == null;

    internal Result(T value)
    {
        Value = value;
        Error = null;
    }

    internal Result(Exception error)
    {
        Value = default;
        Error = error;
    }

#pragma warning disable CA1000
    public static Result<T> FromT(T value) => new(value);
#pragma warning restore CA1000
    
    public static implicit operator Result<T>(T value) => new(value);

    public bool Equals(Result<T> other)
    {
        return EqualityComparer<T?>.Default.Equals(Value, other.Value) && EqualityComparer<Exception?>.Default.Equals(Error, other.Error);
    }

    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Value, Error);

    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    public static bool operator !=(Result<T> left, Result<T> right) => !(left == right);
}

public static class Result
{
    public static Result<T> Success<T>(T value) => new(value);
    public static Result<T> Failure<T>(Exception error) => new(error);
}
