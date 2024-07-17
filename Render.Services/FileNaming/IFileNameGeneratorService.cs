using Render.Models.Sections;

namespace Render.Services.FileNaming
{
    public interface IFileNameGeneratorService
    {
        string GetFileNameForSnapshot(Section section, string projectName, string stageName, string fileExtension);
    }
}