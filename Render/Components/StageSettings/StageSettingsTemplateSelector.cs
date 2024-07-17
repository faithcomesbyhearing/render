using Render.Components.StageSettings.CommunityTestStageSettings;
using Render.Components.StageSettings.ConsultantCheckStageSettings;
using Render.Components.StageSettings.DraftingStageSettings;
using Render.Components.StageSettings.PeerCheckStageSettings;

namespace Render.Components.StageSettings
{
    public class StageSettingsTemplateSelector : DataTemplateSelector
    {
        private readonly DataTemplate _community;
        private readonly DataTemplate _consultant;
        private readonly DataTemplate _drafting;
        private readonly DataTemplate _peer;

        public StageSettingsTemplateSelector()
        {
            _community = new DataTemplate(typeof(CommunityTestStageSettings.CommunityTestStageSettings));
            _consultant = new DataTemplate(typeof(ConsultantCheckStageSettings.ConsultantCheckStageSettings));
            _drafting = new DataTemplate(typeof(DraftingStageSettings.DraftingStageSettings));
            _peer = new DataTemplate(typeof(PeerCheckStageSettings.PeerCheckStageSettings));
        }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            switch (item)
            {
                case CommunityTestStageSettingsViewModel _:
                    return _community;
                case ConsultantCheckStageSettingsViewModel _:
                    return _consultant;
                case DraftingStageSettingsViewModel _:
                    return _drafting;
                case PeerCheckStageSettingsViewModel _:
                    return _peer;
                default:
                    return null;
            }
        }
    }
}