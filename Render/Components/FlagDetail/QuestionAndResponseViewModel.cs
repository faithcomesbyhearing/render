using System.Collections.ObjectModel;
using Render.Components.BarPlayer;
using Render.Kernel;
using Render.Models.Sections.CommunityCheck;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Components.FlagDetail;

public class QuestionAndResponseViewModel : ActionViewModelBase
{
    public IBarPlayerViewModel QuestionPlayerViewModel { get; private set; }
    public ReadOnlyObservableCollection<IBarPlayerViewModel> ResponsePlayerViewModelList { get; private set; }

    public QuestionAndResponseViewModel(Question question, int number, bool required, IViewModelContextProvider viewModelContextProvider)
        : base(ActionState.Required, string.Empty, viewModelContextProvider)
    {
        bool isLibraryQuestion = question.QuestionAudio.CreatedFromAudioId != default;
        var title = string.Format(AppResources.QuestionFormatted, number);
        var glyph = isLibraryQuestion
            ? ResourceExtensions.GetResourceValue<string>(Icon.StarFilled.ToString())
            : null;

        QuestionPlayerViewModel = viewModelContextProvider.GetBarPlayerViewModel(question.QuestionAudio,
            required ? ActionState.Required : ActionState.Optional,
            title: title,
            barPlayerPosition: 0,
            glyph: glyph);

        var responses = new ObservableCollection<IBarPlayerViewModel>();

        for (int i = 0; i < question.Responses.Count; i++)
        {
            var response = question.Responses[i];
            responses.Add(viewModelContextProvider.GetBarPlayerViewModel(response,
                required ? ActionState.Required : ActionState.Optional,
                title: string.Format(AppResources.Response, i + 1),
                barPlayerPosition: 0));
        }

        ResponsePlayerViewModelList = new ReadOnlyObservableCollection<IBarPlayerViewModel>(responses);
    }

	public QuestionAndResponseViewModel(Question question, bool required, IViewModelContextProvider viewModelContextProvider)
	: base(ActionState.Required, string.Empty, viewModelContextProvider)
	{
		var responses = new ObservableCollection<IBarPlayerViewModel>();

		for (int i = 0; i < question.Responses.Count; i++)
		{
			var response = question.Responses[i];
			responses.Add(viewModelContextProvider.GetBarPlayerViewModel(response,
				required ? ActionState.Required : ActionState.Optional,
				title: string.Format(AppResources.Response, i + 1),
				barPlayerPosition: 0));
		}

		ResponsePlayerViewModelList = new ReadOnlyObservableCollection<IBarPlayerViewModel>(responses);
	}

	public void PauseAllAudios(IBarPlayerViewModel exceptPlayer = null)
    {
        if (QuestionPlayerViewModel != exceptPlayer)
        {
            QuestionPlayerViewModel.PauseAudioCommand.Execute().Subscribe();
        }

        foreach (var player in ResponsePlayerViewModelList.Where(player => player != exceptPlayer))
        {
            player.PauseAudioCommand.Execute().Subscribe();
        }
    }

    public override void Dispose()
    {
        QuestionPlayerViewModel?.Dispose();
        QuestionPlayerViewModel = null;

        foreach (var responsePlayer in ResponsePlayerViewModelList)
        {
            responsePlayer?.Dispose();
        }

        ResponsePlayerViewModelList = null;

        base.Dispose();
    }
}