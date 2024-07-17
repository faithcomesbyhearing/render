using Render.Components.BarPlayer;
using Render.Kernel;
using Render.Models.Sections.CommunityCheck;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Components.FlagDetail;

public class RetellViewModel : ActionViewModelBase
{
    public IBarPlayerViewModel RetellPlayerViewModel { get; private set; }

    public RetellViewModel(CommunityRetell retell, int index, bool required, IViewModelContextProvider viewModelContextProvider)
        : base(ActionState.Optional, string.Empty, viewModelContextProvider)
    {
        var title = $"{index} - {AppResources.Retell}";
        var glyph = ResourceExtensions.GetResourceValue<string>(Icon.PassageNew.ToString());

        RetellPlayerViewModel = viewModelContextProvider.GetBarPlayerViewModel(retell,
            required ? ActionState.Required : ActionState.Optional,
            title: title,
            barPlayerPosition: 0,
            glyph: glyph);
    }
    
    public void PauseAllAudios(IBarPlayerViewModel exceptPlayer = null)
    {
        if (RetellPlayerViewModel != exceptPlayer)
        {
            RetellPlayerViewModel.PauseAudioCommand.Execute().Subscribe();
        }
    }

    public override void Dispose()
    {
        RetellPlayerViewModel?.Dispose();
        RetellPlayerViewModel = null;

        base.Dispose();
    }
}