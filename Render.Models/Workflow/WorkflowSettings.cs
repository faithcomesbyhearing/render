using Newtonsoft.Json;

namespace Render.Models.Workflow
{
    public class WorkflowSettings
    {
        [JsonIgnore]
        public static WorkflowSettings StandardCheckSettings => new WorkflowSettings(
            Setting.IsActive,
            Setting.DoPassageReview,
            Setting.RequirePassageReview,
            Setting.DoSectionReview,
            Setting.RequireSectionReview);

        [JsonIgnore]
        public static WorkflowSettings StandardReviseSettings => new WorkflowSettings(
            Setting.IsActive,
            Setting.DoSectionListen,
            Setting.RequireSectionListen,
            Setting.DoPassageListen,
            Setting.RequirePassageListen,
            Setting.DoSectionReview,
            Setting.RequireSectionReview,
            Setting.DoPassageReview,
            Setting.RequirePassageReview,
            Setting.AllowEditing);
        
        [JsonIgnore]
        public static WorkflowSettings StandardBackTranslateSettings => new WorkflowSettings(
            Setting.IsActive,
            Setting.RequireRetellBTSectionListen,
            Setting.RequireRetellBTPassageListen,
            Setting.RequireSegmentBTSectionListen,
            Setting.RequireSegmentBTPassageListen,                
            Setting.DoRetellBTPassageReview,
            Setting.DoSegmentBTPassageReview,
            Setting.RequireRetellBTPassageReview,
            Setting.RequireSegmentBTPassageReview,
            Setting.DoRetellBackTranslate,
            Setting.DoSegmentBackTranslate,
            Setting.DoPassageTranscribe,
            Setting.DoSegmentTranscribe,
            Setting.ConsultantLanguage,
            Setting.Consultant2StepLanguage,
            Setting.SegmentConsultantLanguage,
            Setting.SegmentConsultant2StepLanguage);
            

        [JsonProperty("Settings")] public List<Setting> Settings { get; } = new List<Setting>();

        public WorkflowSettings(params Setting[] initialSettings)
            : this()
        {
            Settings = initialSettings.ToList();
        }

        public WorkflowSettings() { }

        public bool GetSetting(SettingType settingType)
        {
            var setting = Settings.FirstOrDefault(x => x.SettingType == settingType);
            return setting != null && setting.Value;
        }

        public string GetString(SettingType settingType)
        {
            var setting = Settings.FirstOrDefault(x => x.SettingType == settingType);

            return setting != null ? setting.StringValue : "";
        }

        public void SetSetting(SettingType settingType, bool value, string stringValue = "")
        {
            var setting = Settings.FirstOrDefault(x => x.SettingType == settingType);
            if (setting == null)
            {
                setting = new Setting(settingType, value);
                Settings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.StringValue = stringValue;
            }
        }
    }
}