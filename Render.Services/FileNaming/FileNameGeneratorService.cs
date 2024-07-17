using Render.Models.Sections;

namespace Render.Services.FileNaming
{
    public class FileNameGeneratorService : IFileNameGeneratorService
    {
        public string GetFileNameForSnapshot(Section section, string projectName, string stageName, string fileExtension)
        {
            var invalidChars = Path.GetInvalidFileNameChars();

            //replace invalid characters in project name
            projectName = new string(projectName
                .Select(symbol => invalidChars
                .Contains(symbol) ? '_' : symbol)
                .ToArray());
            
            return
                $"{projectName}_{section.Number}_{section.ScriptureReference.ToString().Replace(":", "_")}_{stageName}.{fileExtension}";
        }
    }
}