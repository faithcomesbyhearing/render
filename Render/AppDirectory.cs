using Render.Interfaces;

namespace Render
{
    internal class AppDirectory : IAppDirectory
    {
        public string AppData { get; }

        public string Temporary { get; }

        public string TempAudio { get; }

        public string DbBackup { get; }

        public AppDirectory(string appData, string temporary)
        {
            AppData = appData;
            Temporary = temporary;
            TempAudio = Path.Combine(Temporary, "TempAudio");
            DbBackup = Path.Combine(Temporary, "DbBackup");
        }
    }
}