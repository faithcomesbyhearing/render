namespace Render.Sequencer.Contracts.Models;

public record MarkerFlagModel : FlagModel
{
    public string? Symbol { get; set; }

    public MarkerFlagModel(double position, bool required, string? symbol, bool read) 
        : this(Guid.Empty, position, required, symbol, read) { }

    public MarkerFlagModel(Guid key, double position, bool required, string? symbol, bool read) 
        : base(key, position, required, read) 
    { 
        Symbol = symbol;
    }
}