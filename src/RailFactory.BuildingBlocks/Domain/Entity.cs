namespace RailFactory.BuildingBlocks.Domain;

public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id)
    {
        Id = id;
    }

    // Required for EF Core
    protected Entity()
    {
        Id = default!;
    }

    public TId Id { get; private set; }
}
