using System.Globalization;
using Render.Components.StageSettings.CommunityTestStageSettings;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Services.AudioServices;

namespace Render.Utilities
{
    public static class Utilities
    {
        private const double HourSeconds = 3600;

        public static (DateTimeOffset dateTimeOffset, string name) GetDropDownAttributes(string formattedString)
        {
            var name = (formattedString).Split(':').FirstOrDefault();
            var replaceString = name + ": ";
            var dateTimeString = formattedString.Replace(replaceString,"");
                
            var isValid =  DateTimeOffset.TryParse(dateTimeString, out var dateTime);
            return isValid ? (dateTime, name) : (default, default);
        }
        
        public static string GetTimeDisplay(double positionSeconds, double durationSeconds, AudioPlayerState? state=null)
        {
            var durationText = GetFormattedTime(durationSeconds);
            var positionText = GetFormattedTime(positionSeconds);

            return state != AudioPlayerState.Initial ? $"{positionText} | {durationText}" : $"{durationText}";
        }

        public static (string PositionText, string DurationText) GetTimerLabelDisplay(double positionSeconds, double durationSeconds, AudioPlayerState? state=null)
        {
            var durationText = GetFormattedTime(durationSeconds);
            var positionText = GetFormattedTime(positionSeconds);
            return state != AudioPlayerState.Initial ? (positionText, durationText) : (null, durationText);
            
        }

        public static int ToMilliseconds(this double seconds)
        {
            return (int)(seconds * 1000);
        }

        public static string GetFormattedTime(double seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            return seconds >= HourSeconds ? $"{timeSpan:hh\\:mm\\:ss}" : $"{timeSpan:mm\\:ss}";
        }
        
        public static List<CultureInfo> GetAvailableCultures()
        {
            var result = new List<CultureInfo>();
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            var culture = cultures.FirstOrDefault(x => x.Name.StartsWith("ar"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("bn"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("dz"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("en"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("es"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("fr"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("ha"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("hi"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("id"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("lo"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("ne"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("pt"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("ru"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("sw"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("ta"));
            if (culture != null)
            {
                result.Add(culture);
            }
            culture = cultures.FirstOrDefault(x => x.Name.StartsWith("vi"));
            if (culture != null)
            {
                result.Add(culture);
            }
            return result;
        }
        
        public static string GetStageName(Stage stage)
        {
            return stage.Name == GetDefaultStageName(stage.StageType) ? StageTypes.ToString(stage.StageType) : stage.Name;
        }

        public static string GetDefaultStageName(Models.Workflow.StageTypes stageType)
        {
            switch (stageType)
            {
                case Models.Workflow.StageTypes.Drafting:
                    return Stage.DraftingDefaultStageName;
                case Models.Workflow.StageTypes.PeerCheck:
                    return Stage.PeerCheckDefaultStageName;
                case Models.Workflow.StageTypes.CommunityTest:
                    return Stage.CommunityTestDefaultStageName;
                case Models.Workflow.StageTypes.ConsultantCheck:
                    return Stage.ConsultantCheckDefaultStageName;
                case Models.Workflow.StageTypes.ConsultantApproval:
                    return Stage.ConsultantApprovalDefaultStageName;
                case Models.Workflow.StageTypes.Generic:
                default:
                    throw new ArgumentOutOfRangeException(nameof(stageType), stageType, null);
            }
        }

        public static RetellQuestionResponseSettings GetRetellQuestionResponseSettingFrom(Step step)
        {
            var doCommunityRetell = step.StepSettings.GetSetting(SettingType.DoCommunityRetell);
            var doCommunityResponse = step.StepSettings.GetSetting(SettingType.DoCommunityResponse);

            return doCommunityRetell && doCommunityResponse ? 
                RetellQuestionResponseSettings.Both :
                doCommunityRetell ? 
                    RetellQuestionResponseSettings.Retell :
                    RetellQuestionResponseSettings.QuestionAndResponse;
        }

        /// <summary>
        /// Need to find index of the next untranslated segment based on previously translated segment.
        /// If this is a segment index at the end of the segments list, 
        /// then find the next untranslated segment index from the beginning.
        /// If there are not previously translated segments, just select first untranslated segment.
        /// </summary
        public static int GetSegmentIndexToSelect(
            IList<SegmentBackTranslation> backTranslations, 
            SegmentBackTranslation previousBackTranslation,
            Func<SegmentBackTranslation, bool> nextSegmentCondition)
        {
            if (previousBackTranslation is null)
            {
                var segmentToSelect = backTranslations.FirstOrDefault(nextSegmentCondition);
                return segmentToSelect is null ? -1 : backTranslations.IndexOf(segmentToSelect);
            }

            if (backTranslations.Contains(previousBackTranslation) is false)
            {
                return -1;
            }

            var previousSelectedTranslationIndex = backTranslations.IndexOf(previousBackTranslation);
            var itemToSelect = 
                
                backTranslations
                    .Skip(previousSelectedTranslationIndex + 1)
                    .Take(backTranslations.Count - previousSelectedTranslationIndex + 1)
                    .FirstOrDefault(nextSegmentCondition) ??
                
                backTranslations
                    .Take(previousSelectedTranslationIndex)
                    .FirstOrDefault(nextSegmentCondition);

            return itemToSelect is null ? -1 : backTranslations.IndexOf(itemToSelect);
        }
    }
}