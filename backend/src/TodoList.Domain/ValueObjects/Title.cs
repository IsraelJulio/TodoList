using TodoList.Domain.Exceptions;

namespace TodoList.Domain.ValueObjects;

public sealed class Title
{
    public const int MaxLength = 200;

    public string Value { get; }

    private Title(string value) => Value = value;

    public static Title Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException("O título não pode ser vazio.");

        if (value.Length > MaxLength)
            throw new DomainValidationException($"O título não pode ultrapassar {MaxLength} caracteres.");

        return new Title(value.Trim());
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj) =>
        obj is Title other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}
