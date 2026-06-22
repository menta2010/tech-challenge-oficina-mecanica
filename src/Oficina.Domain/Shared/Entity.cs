namespace Oficina.Domain.Shared;

/// <summary>
/// Base de identidade para entidades e raizes de agregado.
/// Igualdade por Id (identidade), nao por valor.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public override bool Equals(object? obj)
        => obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}
