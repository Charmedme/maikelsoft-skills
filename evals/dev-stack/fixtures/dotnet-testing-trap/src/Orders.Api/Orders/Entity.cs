namespace Orders.Api.Orders;

/// <summary>
/// Base class for entities. Two entities are equal when they have the same type and the same id.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && Id != Guid.Empty && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}
