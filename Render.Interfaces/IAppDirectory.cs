namespace Render.Interfaces
{
    public interface IAppDirectory
    {
        string AppData { get; }
        string Temporary { get; }
        string TempAudio { get; }
    }
}