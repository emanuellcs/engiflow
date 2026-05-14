using EngiFlow.Domain.Exceptions;

namespace EngiFlow.Domain.ValueObjects;

public readonly record struct CompanyId
{
    public CompanyId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("Company id cannot be empty.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static CompanyId New() => new(Guid.NewGuid());

    public static CompanyId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

public readonly record struct UserId
{
    public UserId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("User id cannot be empty.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static UserId New() => new(Guid.NewGuid());

    public static UserId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

public readonly record struct EngineeringChangeOrderId
{
    public EngineeringChangeOrderId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("Engineering change order id cannot be empty.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static EngineeringChangeOrderId New() => new(Guid.NewGuid());

    public static EngineeringChangeOrderId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

public readonly record struct EcoEventId
{
    public EcoEventId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("ECO event id cannot be empty.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static EcoEventId New() => new(Guid.NewGuid());

    public static EcoEventId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
