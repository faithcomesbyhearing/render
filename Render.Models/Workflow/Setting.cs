using Newtonsoft.Json;
using ReactiveUI.Fody.Helpers;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Workflow
{
    public class Setting : ValueObject
    {
        //Quickly create our settings with default values
        [JsonIgnore]
        public static Setting DoSectionListen => new Setting(SettingType.DoSectionListen);

        [JsonIgnore]
        public static Setting DoPassageListen => new Setting(SettingType.DoPassageListen);

        [JsonIgnore]
        public static Setting RequireSectionListen => new Setting(SettingType.RequireSectionListen);

        [JsonIgnore]
        public static Setting RequirePassageListen => new Setting(SettingType.RequirePassageListen);

        [JsonIgnore]
        public static Setting IsActive => new Setting(SettingType.IsActive);

        [JsonIgnore]
        public static Setting AssignToTranslator => new Setting(SettingType.AssignToTranslator);

        [JsonIgnore]
        public static Setting NoSelfCheck => new Setting(SettingType.NoSelfCheck);

        [JsonIgnore]
        public static Setting AssignToConsultant => new Setting(SettingType.AssignToConsultant);

        [JsonIgnore]
        public static Setting DoSectionReview => new Setting(SettingType.DoSectionReview);

        [JsonIgnore]
        public static Setting DoPassageReview => new Setting(SettingType.DoPassageReview);

        [JsonIgnore]
        public static Setting RequireSectionReview => new Setting(SettingType.RequireSectionReview);

        [JsonIgnore]
        public static Setting RequirePassageReview => new Setting(SettingType.RequirePassageReview);

        [JsonIgnore]
        public static Setting RequireNoteListen => new Setting(SettingType.RequireNoteListen);

        [JsonIgnore]
        public static Setting LoopByDefault => new Setting(SettingType.LoopByDefault);

        [JsonIgnore]
        public static Setting TranslatorCanSkipCheck => new Setting(SettingType.TranslatorCanSkipCheck);

        [JsonIgnore]
        public static Setting DoCommunityRetell => new Setting(SettingType.DoCommunityRetell);

        [JsonIgnore]
        public static Setting DoCommunityResponse => new Setting(SettingType.DoCommunityResponse);

        [JsonIgnore]
        public static Setting DoRetellBackTranslate => new Setting(SettingType.DoRetellBackTranslate);

        [JsonIgnore]
        public static Setting DoSegmentBackTranslate => new Setting(SettingType.DoSegmentBackTranslate);

        [JsonIgnore]
        public static Setting DoSegmentTranscribe => new Setting(SettingType.DoSegmentTranscribe);

        [JsonIgnore]
        public static Setting DoPassageTranscribe => new Setting(SettingType.DoPassageTranscribe);

        [JsonIgnore]
        public static Setting RequireRetellBTPassageListen => new Setting(SettingType.RequireRetellBTPassageListen);

        [JsonIgnore]
        public static Setting RequireSegmentBTPassageListen => new Setting(SettingType.RequireSegmentBTPassageListen);

        [JsonIgnore]
        public static Setting RequireRetellBTSectionListen => new Setting(SettingType.RequireRetellBTSectionListen);

        [JsonIgnore]
        public static Setting RequireSegmentBTSectionListen => new Setting(SettingType.RequireSegmentBTSectionListen);

        [JsonIgnore]
        public static Setting DoRetellBTPassageReview => new Setting(SettingType.DoRetellBTPassageReview);

        [JsonIgnore]
        public static Setting DoSegmentBTPassageReview => new Setting(SettingType.DoSegmentBTPassageReview);

        [JsonIgnore]
        public static Setting RequireSegmentBTPassageReview => new Setting(SettingType.RequireSegmentBTPassageReview);

        [JsonIgnore]
        public static Setting RequireRetellBTPassageReview => new Setting(SettingType.RequireRetellBTPassageReview);

        [JsonIgnore]
        public static Setting ConsultantLanguage => new Setting(SettingType.ConsultantLanguage);

        [JsonIgnore]
        public static Setting Consultant2StepLanguage => new Setting(SettingType.Consultant2StepLanguage);

        [JsonIgnore]
        public static Setting SegmentConsultantLanguage => new Setting(SettingType.SegmentConsultantLanguage);

        [JsonIgnore]
        public static Setting SegmentConsultant2StepLanguage => new Setting(SettingType.SegmentConsultant2StepLanguage);
        
        [JsonIgnore]
        public static Setting AllowEditing => new Setting(SettingType.AllowEditing);
        
        [Reactive]
        [JsonProperty("Value")]
        public bool Value { get; set; }
        
        [Reactive]
        [JsonProperty("StringValue")]
        public string StringValue { get; set; }
        
        [JsonProperty("SettingType")]
        public SettingType SettingType { get; }

        public Setting(SettingType settingType, bool initialValue = true, string stringValue = "")
        {
            SettingType = settingType;
            Value = initialValue;
            StringValue = stringValue;
        }
    }
}