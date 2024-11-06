using Render.Models.Audio;
using Render.Models.Sections;

namespace Render.Services.FileNaming
{
    public interface IFileNameGeneratorService
    {
        public string GetFileNameForAudioGroup(
            Section section,
            string stageName,
            string autonim,
            bool hasConflict,
            int index,
            AudioGroup audioGroup);

        public string GetFileNameForScopeAudiosZip(
            Section section,
            string projectName,
            string scopeName);
    }
}