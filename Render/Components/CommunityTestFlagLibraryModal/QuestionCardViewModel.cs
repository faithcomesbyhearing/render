using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Kernel;
using Render.Models.Audio;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services.AudioServices;

namespace Render.Components.CommunityTestFlagLibraryModal;

public class QuestionCardViewModel : ViewModelBase
{
    private readonly ReactiveCommand<QuestionCardViewModel, Unit> _addToQuestionListCommand;
    private readonly ReactiveCommand<QuestionCardViewModel, Unit> _addToLibraryCommand;
    public NotableAudio Audio { get; private set; }
    [Reactive] public bool IsLibraryQuestion { get; private set; }
    [Reactive] public bool Include { get; set; }
    [Reactive] public bool IsDeleted { get; set; }
    [Reactive] public string AddToLibraryButtonGlyph { get; set; }
    [Reactive] public string AddToQuestionListButtonGlyph { get; set; }
    public ReactiveCommand<Unit, Unit> AddToLibraryCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteFromQuestionListCommand { get; }
    public ReactiveCommand<Unit, Unit> UndoDeleteFromQuestionCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteFromLibraryCommand { get; }
    public ReactiveCommand<Unit, Unit> UndoDeleteFromLibraryCommand { get; }
    public ReactiveCommand<Unit, Unit> AddToQuestionListCommand { get; }
    public IBarPlayerViewModel BarPlayerViewModel { get; }

    public QuestionCardViewModel(IViewModelContextProvider viewModelContextProvider, NotableAudio audio,
        ReactiveCommand<QuestionCardViewModel, Unit> addToQuestionListCommand,
        ReactiveCommand<QuestionCardViewModel, Unit> addToLibraryCommand,
        IObservable<bool> canPlayQuestionAudio)
        : base("QuestionCard", viewModelContextProvider)
    {
        _addToLibraryCommand = addToLibraryCommand;
        _addToQuestionListCommand = addToQuestionListCommand;

        Audio = audio;
        IsLibraryQuestion = audio is StandardQuestion;

        BarPlayerViewModel = viewModelContextProvider.GetBarPlayerViewModel(audio, ActionState.Optional, AppResources.Question,
            0, canPlayAudio: canPlayQuestionAudio);

        var canExecuteDelete = this.WhenAnyValue(x => x.BarPlayerViewModel.AudioPlayerState)
            .Select(x => x != AudioPlayerState.Playing);

        AddToLibraryCommand = ReactiveCommand.CreateFromTask(AddToLibrary);
        DeleteFromQuestionListCommand = ReactiveCommand.Create(Delete, canExecuteDelete);
        UndoDeleteFromQuestionCommand = ReactiveCommand.Create(UndoDelete);
        DeleteFromLibraryCommand = ReactiveCommand.Create(Delete, canExecuteDelete);
        UndoDeleteFromLibraryCommand = ReactiveCommand.Create(UndoDelete);
        AddToQuestionListCommand = ReactiveCommand.CreateFromTask(AddToQuestionList);

        Disposables.Add(this.WhenAnyValue(x => x.Include, x => x.IsLibraryQuestion)
            .Subscribe(x =>
            {
                if (IsLibraryQuestion || Include)
                {
                    BarPlayerViewModel.Glyph =
                        ResourceExtensions.GetResourceValue<string>(Icon.StarFilled.ToString());
                    BarPlayerViewModel.SetGlyphColor(ResourceExtensions.GetColor("SecondaryText"));
                }
                else
                {
                    BarPlayerViewModel.Glyph = null;
                }
            }));

        Disposables.Add(this.WhenAnyValue(x => x.FlowDirection)
            .Subscribe(flowDirection =>
            {
                AddToLibraryButtonGlyph = IconExtensions
                    .BuildFontImageSource(flowDirection == FlowDirection.LeftToRight
                        ? Icon.TickerArrowLeft
                        : Icon.TickerArrowRight).Glyph;

                AddToQuestionListButtonGlyph = IconExtensions.BuildFontImageSource(
                    flowDirection == FlowDirection.LeftToRight
                        ? Icon.TickerArrowRight
                        : Icon.TickerArrowLeft).Glyph;
            }));
    }

    private async Task AddToLibrary()
    {
        if (Include || IsDeleted)
        {
            return;
        }

        await _addToLibraryCommand.Execute(this);
    }

    private async Task AddToQuestionList()
    {
		if (Include || IsDeleted)
		{
			return;
		}

		await _addToQuestionListCommand.Execute(this);
    }

    private void Delete()
    {
        IsDeleted = true;
    }

    private void UndoDelete()
    {
        IsDeleted = false;
    }

    public override void Dispose()
    {
        Audio = null;
        BarPlayerViewModel?.Dispose();

        base.Dispose();
    }
}