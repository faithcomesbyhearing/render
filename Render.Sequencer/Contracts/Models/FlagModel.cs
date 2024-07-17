namespace Render.Sequencer.Contracts.Models;

public abstract record FlagModel
{
    public Guid Key { get; }

    public double Position { get; }

    public bool IsRequired { get; }

    public bool IsRead { get; }

    public FlagModel(double position, bool required, bool read)
    {
        Key = default;
        Position = position;
        IsRequired = required;
        IsRead = read;
    }

    public FlagModel(Guid key, double position, bool required, bool read) 
        : this(position, required, read)
    {
        Key = key;
    }
}