using Render.Models.Audio;
using Render.Models.Sections;

namespace Render.Services.FileNaming
{
    public class FileNameGeneratorService : IFileNameGeneratorService
    {
        public string GetFileNameForAudioGroup(
           Section section,
           string stageName,
           string autonim,
           bool hasConflict,
           int index,
           AudioGroup audioGroup)
        {
            var bookName = section.Passages
                .Where(p => p.ScriptureReferences.Count > 0)
                .FirstOrDefault().ScriptureReferences
                    .FirstOrDefault().Book;

            return audioGroup.AudioStepType switch
            {
                AudioStepTypes.None => "None",
                AudioStepTypes.Draft => 
                    GetNameForDraft(
                        hasConflict, 
                        index, 
                        autonim, 
                        section.Number,
                        section.ScriptureReference.ToString().Replace(":", "_"),
                        stageName),
                AudioStepTypes.BackTranslate or
                AudioStepTypes.SegmentBackTranslate or
                AudioStepTypes.BackTranslate2 or
                AudioStepTypes.SegmentBackTranslate2 => 
                    GetNameForRetells(
                        hasConflict, 
                        index, 
                        autonim, 
                        section.Number,
                        bookName, 
                        GetAudioStepTypeString(audioGroup.AudioStepType)),
                _ => "Unknown"
            };
        }

        public string GetFileNameForScopeAudiosZip(Section section, string projectName, string scopeName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();

            //replace invalid characters in project name
            projectName = new string(projectName
                .Select(symbol => invalidChars
                .Contains(symbol) ? '_' : symbol)
                .ToArray());

            return $"{projectName}_{scopeName}_{section.Number}_{section.ScriptureReference.ToString().Replace(":", "_")}.zip";
        }

        private static string GetNameForDraft(
            bool hasConflict,
            int index,
            string autonim,
            int sectionNumber,
            string bookAndVerses,
            string stageName)
        {
            if (hasConflict)
            {
                var conflictName = $"[Conflict_{index + 1}]";
                return $"{autonim}_{sectionNumber}_{bookAndVerses} - {stageName} - {conflictName}.wav";
            }

            return $"{autonim}_{sectionNumber}_{bookAndVerses} - {stageName}.wav";
        }

        private static string GetNameForRetells(
            bool hasConflict,
            int index,
            string autonim,
            int sectionNumber,
            string book,
            string audioType)
        {
            if (hasConflict)
            {
                var conflictName = $"[Conflict_{index + 1}]";
                return $"{autonim}_{sectionNumber}_{book} - {audioType} - {conflictName}.wav";
            }

            return $"{autonim}_{sectionNumber}_{book} - {audioType}.wav";
        }

        private static string GetAudioStepTypeString(AudioStepTypes stepType)
        {
            return stepType switch
            {
                AudioStepTypes.None => "None",
                AudioStepTypes.Draft => "Draft",
                AudioStepTypes.BackTranslate => "PBT_1",
                AudioStepTypes.SegmentBackTranslate => "SBT_1",
                AudioStepTypes.BackTranslate2 => "PBT_2",
                AudioStepTypes.SegmentBackTranslate2 => "SBT_2",
                _ => "Unknown"
            };
        }
    }
}