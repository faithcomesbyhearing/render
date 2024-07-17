using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.AudioRecorder;
using Render.Kernel;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Resources.Localization;
using Render.Services.AudioServices;

namespace Render.Components.CommunityTestFlagLibraryModal;

public class CommunityTestFlagLibraryModalViewModel : PopupViewModelBase<bool>
{
    private readonly Guid _stageId;

    private Section _section;
    private CommunityTest _communityTest;
    private CommunityTestFlagMarkerViewModel _flagMarkerViewModel;
    private Action<CommunityTestFlagMarkerViewModel> _onClosed;
    private IObservable<bool> _canPlayQuestionAudio;

    [Reactive] public IMiniAudioRecorderViewModel MiniAudioRecorderViewModel { get; private set; }

    public ReactiveCommand<Unit, Unit> CloseModalCommand { get; }
    public ReactiveCommand<Unit, Unit> ForceCloseModalCommand { get; }
    public ReactiveCommand<Unit, Unit> StartRecordingCommand { get; }
    public ReactiveCommand<Unit, Unit> StopRecordingCommand { get; }

	public ReactiveCommand<QuestionCardViewModel, Unit> AddToQuestionListCommand { get; }
    public ReactiveCommand<QuestionCardViewModel, Unit> AddToLibraryCommand { get; }

    [Reactive] public bool IsLibraryQuestionsExists { get; private set; }
    [Reactive] public bool IsFlagQuestionsExists { get; private set; }
    [Reactive] public bool RecordingIsInProgress { get; private set; }
    [Reactive] public bool QuestionPlayerPlayingIsInProgress { get; private set; }
    [Reactive] public bool LibraryQuestionPlayerPlayingIsInProgress { get; private set; }
    [Reactive] public string PressArrowToAddQuestionLabelText { get; private set; }
    [Reactive] public string PressCircleToRecordOrPressArrowToAddQuestionLabelText { get; private set; }

    private readonly SourceCache<QuestionCardViewModel, Guid> _standardQuestionCardSource = new(x => x.Audio.Id);

    private readonly ReadOnlyObservableCollection<QuestionCardViewModel> _standardQuestionCards;
    public ReadOnlyObservableCollection<QuestionCardViewModel> StandardQuestionCards => _standardQuestionCards;

    private readonly SourceCache<QuestionCardViewModel, Guid> _questionCardSource = new(x => x.Audio.Id);

    private readonly ReadOnlyObservableCollection<QuestionCardViewModel> _questionCards;
    public ReadOnlyObservableCollection<QuestionCardViewModel> QuestionCards => _questionCards;

	public static async Task<CommunityTestFlagLibraryModalViewModel> CreateAsync(
        IViewModelContextProvider viewModelContextProvider,
        CommunityTestFlagMarkerViewModel flagMarkerViewModel,
        Section section,
        Guid stageId,
        CommunityTest communityTest,
        Action<CommunityTestFlagMarkerViewModel> onClosed)
    {
        var standardQuestions = await viewModelContextProvider
            .GetStandardQuestionAudioRepository()
            .GetMultipleByParentIdAsync(section.ProjectId);

        return new CommunityTestFlagLibraryModalViewModel(viewModelContextProvider, flagMarkerViewModel, section, stageId,
            communityTest, onClosed, standardQuestions);
    }

    private CommunityTestFlagLibraryModalViewModel(IViewModelContextProvider viewModelContextProvider,
        CommunityTestFlagMarkerViewModel flagMarkerViewModel, Section section, Guid stageId, CommunityTest communityTest,
        Action<CommunityTestFlagMarkerViewModel> onClosed, List<StandardQuestion> standardQuestions)
        : base("CommunityTestFlagLibraryModalViewModel", viewModelContextProvider)
    {
        _section = section;
        _stageId = stageId;
        _communityTest = communityTest;
        _flagMarkerViewModel = flagMarkerViewModel;
        _onClosed = onClosed;

        AddToQuestionListCommand = ReactiveCommand.Create<QuestionCardViewModel>(AddToQuestionList);
        AddToLibraryCommand = ReactiveCommand.Create<QuestionCardViewModel>(AddToLibrary);

        var canExecuteCloseModal = this.WhenAnyValue(x => x.RecordingIsInProgress)
            .Select(x => !x);

        _canPlayQuestionAudio = this.WhenAnyValue(x => x.RecordingIsInProgress)
            .Select(x => !x);

        var canExecuteRecording = this.WhenAnyValue(x => x.QuestionPlayerPlayingIsInProgress,
                x => x.LibraryQuestionPlayerPlayingIsInProgress)
            .Select(((bool questionPlayerPlayingIsInProgress, bool libraryQuestionPlayerPlayingIsInProgress) x) =>
                !x.questionPlayerPlayingIsInProgress && !x.libraryQuestionPlayerPlayingIsInProgress);

        CloseModalCommand = ReactiveCommand.CreateFromTask(CloseModalAsync, canExecuteCloseModal);
        ForceCloseModalCommand = ReactiveCommand.CreateFromTask(ForceCloseModalAsync);
        StartRecordingCommand = ReactiveCommand.Create(StartRecording, canExecuteRecording);
        StopRecordingCommand = ReactiveCommand.CreateFromTask(StopRecordingAsync);

		MiniAudioRecorderViewModel = viewModelContextProvider.GetMiniAudioRecorderViewModel(new Audio(default, _section.ProjectId, _section.ProjectId));

        _standardQuestionCardSource.AddOrUpdate(standardQuestions.Select(x => new
            QuestionCardViewModel(viewModelContextProvider, x, AddToQuestionListCommand, AddToLibraryCommand, _canPlayQuestionAudio)));

        var standardQuestionCardChangeList = _standardQuestionCardSource.Connect().Publish();

        Disposables.Add(standardQuestionCardChangeList.Bind(out _standardQuestionCards)
            .Subscribe());

        standardQuestionCardChangeList.CountChanged().Subscribe(_ => { IsLibraryQuestionsExists = _standardQuestionCards.Count > 0; });

        Disposables.Add(standardQuestionCardChangeList
            .WhenPropertyChanged(x => x.BarPlayerViewModel.AudioPlayerState)
            .Subscribe(vm =>
            {
                if (vm.Value == AudioPlayerState.Playing)
                {
                    PauseStandardQuestionPlayers(vm.Sender);
                    PauseQuestionPlayers();
                }

                LibraryQuestionPlayerPlayingIsInProgress = _standardQuestionCards.Any(x =>
                    x.BarPlayerViewModel.AudioPlayerState == AudioPlayerState.Playing);
            }));

        Disposables.Add(standardQuestionCardChangeList.Connect());

		var questionsOnFlag = flagMarkerViewModel.Flag.GetQuestionsForStage(stageId).Select(x => x.QuestionAudio.Id).ToList();
		var questionsOnFlagCreatedFrom = flagMarkerViewModel.Flag.GetQuestionsForStage(stageId)
			.Where(q => q.QuestionAudio.CreatedFromAudioId != default)
			.Select(q => q.QuestionAudio.CreatedFromAudioId).ToList();

		foreach (var standardQuestionCardViewModel in _standardQuestionCards)
        {
            if (standardQuestionCardViewModel.Audio.CreatedFromAudioId != default)
            {
                standardQuestionCardViewModel.Include =
                    questionsOnFlag.Contains(standardQuestionCardViewModel.Audio.CreatedFromAudioId) ||
                    questionsOnFlagCreatedFrom.Contains(standardQuestionCardViewModel.Audio.Id);
            }
            else
            {
                standardQuestionCardViewModel.Include =
                    questionsOnFlagCreatedFrom.Contains(standardQuestionCardViewModel.Audio.Id);
            }
        }

		_questionCardSource.AddOrUpdate(flagMarkerViewModel.Flag.GetQuestionsForStage(stageId).Select(x => new
			QuestionCardViewModel(viewModelContextProvider, x.QuestionAudio, AddToQuestionListCommand, AddToLibraryCommand, _canPlayQuestionAudio)));

        var questionCardChangeList = _questionCardSource.Connect().Publish();

        Disposables.Add(questionCardChangeList.Bind(out _questionCards)
            .Subscribe());

        questionCardChangeList.CountChanged().Subscribe(_ => { IsFlagQuestionsExists = _questionCards.Count > 0; });

        Disposables.Add(questionCardChangeList
            .WhenPropertyChanged(x => x.BarPlayerViewModel.AudioPlayerState)
            .Subscribe(vm =>
            {
                if (vm.Value == AudioPlayerState.Playing)
                {
                    PauseStandardQuestionPlayers();
                    PauseQuestionPlayers(vm.Sender);
                }

                QuestionPlayerPlayingIsInProgress = _questionCards.Any(x =>
                    x.BarPlayerViewModel.AudioPlayerState == AudioPlayerState.Playing);
            }));

        Disposables.Add(questionCardChangeList.Connect());

        var questionsInLibrary = _standardQuestionCards.Select(x => x.Audio.Id).ToList();
        var questionsInLibraryCreatedFrom = _standardQuestionCards
            .Where(x => x.Audio.CreatedFromAudioId != default)
            .Select(x => x.Audio.CreatedFromAudioId).ToList();

        foreach (var questionCardViewModel in _questionCards)
        {
            if (questionCardViewModel.Audio.CreatedFromAudioId != default)
            {
                questionCardViewModel.Include =
                    questionsInLibrary.Contains(questionCardViewModel.Audio.CreatedFromAudioId) ||
                    questionsInLibraryCreatedFrom.Contains(questionCardViewModel.Audio.Id);
            }
            else
            {
                questionCardViewModel.Include =
                    questionsInLibraryCreatedFrom.Contains(questionCardViewModel.Audio.Id);
            }
        }

        Disposables.Add(this.WhenAnyValue(x => x.FlowDirection)
            .Subscribe(flowDirection =>
            {
                object leftArrow = '\u2B05';
                object rightArrow = '\u2B95';

                PressArrowToAddQuestionLabelText = string.Format(AppResources.PressArrowToAddQuestion,
                    flowDirection == FlowDirection.LeftToRight ? leftArrow : rightArrow);

                PressCircleToRecordOrPressArrowToAddQuestionLabelText = string.Format(
                    AppResources.PressCircleToRecordOrPressArrowToAddQuestionFromLibrary,
                    flowDirection == FlowDirection.LeftToRight ? rightArrow : leftArrow, Environment.NewLine);
            }));

		Disposables.Add(MiniAudioRecorderViewModel.WhenAnyValue(r => r.AudioRecorderState)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(state => {
					if (state == AudioRecorderState.NoAudio)
					{
						RecordingIsInProgress = false;
					}
				}));
        
        StopRecordingCommand.IsExecuting.Subscribe(isExecuting => IsLoading = isExecuting);
	}

    private void PauseAllQuestionPlayers()
    {
        PauseQuestionPlayers();
        PauseStandardQuestionPlayers();
    }

    private void PauseQuestionPlayers(QuestionCardViewModel needToExcept = null)
    {
        if (QuestionCards.All(s => s.BarPlayerViewModel.AudioPlayerState != AudioPlayerState.Playing))
        {
            return;
        }

        var playingStateCards = QuestionCards
            .Where(s => s.BarPlayerViewModel.AudioPlayerState == AudioPlayerState.Playing)
            .ToList();

        if (needToExcept != null)
        {
            playingStateCards = playingStateCards.Except(new List<QuestionCardViewModel> { needToExcept }).ToList();
        }

        foreach (var card in playingStateCards)
        {
            card.BarPlayerViewModel.Pause();
        }
    }

    private void PauseStandardQuestionPlayers(QuestionCardViewModel needToExcept = null)
    {
        if (StandardQuestionCards.All(s => s.BarPlayerViewModel.AudioPlayerState != AudioPlayerState.Playing))
        {
            return;
        }

        var playingStateCards = StandardQuestionCards
            .Where(s => s.BarPlayerViewModel.AudioPlayerState == AudioPlayerState.Playing)
            .ToList();

        if (needToExcept != null)
        {
            playingStateCards = playingStateCards.Except(new List<QuestionCardViewModel> { needToExcept }).ToList();
        }

        foreach (var card in playingStateCards)
        {
            card.BarPlayerViewModel.Pause();
        }
    }

    private void AddToLibrary(QuestionCardViewModel questionCardViewModel)
    {
        var audio = new StandardQuestion(_section.ScopeId, _section.ProjectId, _section.ProjectId);
        audio.SetAudio(questionCardViewModel.Audio.Data);
        questionCardViewModel.Audio.CreatedFromAudioId = audio.Id;

        _standardQuestionCardSource.AddOrUpdate(new QuestionCardViewModel(ViewModelContextProvider, audio,
            AddToQuestionListCommand, AddToLibraryCommand, _canPlayQuestionAudio)
        {
            Include = true
        });

        questionCardViewModel.Include = true;
    }

    private void AddToQuestionList(QuestionCardViewModel questionCardViewModel)
    {
        var audio = new NotableAudio(_section.ScopeId, _section.ProjectId, _flagMarkerViewModel.Flag.Id);
        audio.SetAudio(questionCardViewModel.Audio.Data);
        audio.CreatedFromAudioId = questionCardViewModel.Audio.Id;

        _questionCardSource.AddOrUpdate(new QuestionCardViewModel(ViewModelContextProvider, audio,
            AddToQuestionListCommand, AddToLibraryCommand, _canPlayQuestionAudio)
        {
            Include = true
        });

        questionCardViewModel.Include = true;
    }

    private void StartRecording()
    {
        PauseAllQuestionPlayers();
        RecordingIsInProgress = true;
        MiniAudioRecorderViewModel.StartRecordingCommand.Execute().Subscribe();
    }

    private async Task StopRecordingAsync()
    {
        await MiniAudioRecorderViewModel.StopRecordingCommand.Execute();
        await AddQuestionAsync();

        RecordingIsInProgress = false;
    }

    private async Task AddQuestionAsync()
    {
        var audio = await MiniAudioRecorderViewModel.GetAudio();
        
		if (!audio.HasAudio)
		{
			return;
		}
        
		var questionAudio = new NotableAudio(_section.ScopeId, _section.ProjectId, _flagMarkerViewModel.Flag.Id);
		questionAudio.SetAudio(audio.Data);

		_questionCardSource.AddOrUpdate(new QuestionCardViewModel(ViewModelContextProvider, questionAudio,
			AddToQuestionListCommand, AddToLibraryCommand, _canPlayQuestionAudio));

		MiniAudioRecorderViewModel.Dispose();
		MiniAudioRecorderViewModel = null;

		MiniAudioRecorderViewModel = ViewModelContextProvider.GetMiniAudioRecorderViewModel(
			new Audio(default, _section.ProjectId, _section.ProjectId));
	}

    /// <summary>
    /// Closes library popup if recording is not in progress only.
    /// </summary>
	private async Task CloseModalAsync()
    {
        PauseAllQuestionPlayers();

        await SaveAsync();
        
        ClosePopup(_flagMarkerViewModel.Flag.Questions.Any());

        Dispose();
    }

    /// <summary>
    /// Closes library popup in any state.
    /// </summary>
    private async Task ForceCloseModalAsync()
    {
        await MiniAudioRecorderViewModel.StopRecordingCommand.Execute();
        await CloseModalAsync();
    }

    private async Task SaveAsync()
    {
        await SaveQuestionsAsync();
        await SaveStandardQuestionsAsync();

        _onClosed.Invoke(_flagMarkerViewModel);

        await ViewModelContextProvider.GetCommunityTestRepository().SaveCommunityTestAsync(_communityTest);
    }

    private async Task SaveStandardQuestionsAsync()
    {
        var standardQuestionRepo = ViewModelContextProvider.GetStandardQuestionAudioRepository();
        var deletedStandardQuestions = new List<Guid>();

        foreach (var question in StandardQuestionCards)
        {
            if (question.IsDeleted)
            {
                await standardQuestionRepo.DeleteAudioByIdAsync(question.Audio.Id);
                deletedStandardQuestions.Add(question.Audio.Id);
                continue;
            }

            await standardQuestionRepo.SaveAsync((StandardQuestion)question.Audio);
        }

        await standardQuestionRepo.ResetCreatedFromAudioIds(_communityTest.ProjectId, deletedStandardQuestions);
    }

    private async Task SaveQuestionsAsync()
    {
        var notableAudioRepository = ViewModelContextProvider.GetNotableAudioRepository();

        foreach (var question in QuestionCards)
        {
            var flagQuestion =
			   _flagMarkerViewModel.Flag.GetQuestionsForStage(_stageId).FirstOrDefault(q => q.QuestionAudioId == question.Audio.Id);

			if (question.IsDeleted)
            {
                if (flagQuestion != null)
                {
					if (_flagMarkerViewModel.Flag.RemoveQuestion(flagQuestion, _stageId))
					{
						await notableAudioRepository.DeleteAudioByIdAsync(flagQuestion.QuestionAudio.Id);
					}
				}
            }
            else
            {
                if (flagQuestion == null)
                {
                    flagQuestion = _flagMarkerViewModel.Flag.AddQuestion(_stageId, _section.ScopeId, _section.ProjectId);
                    flagQuestion.UpdateAudio(question.Audio);
                }

                await notableAudioRepository.SaveAsync(flagQuestion.QuestionAudio);
            }
        }
    }

    public override void Dispose()
    {
        _section = null;
        _communityTest = null;
        _onClosed = null;
        _canPlayQuestionAudio = null;

        CloseModalCommand.Dispose();
        ForceCloseModalCommand.Dispose();
        StartRecordingCommand.Dispose();
        StopRecordingCommand.Dispose();
	    AddToQuestionListCommand.Dispose();
        AddToLibraryCommand.Dispose();

        MiniAudioRecorderViewModel.Dispose();
        MiniAudioRecorderViewModel = null;
        
        _flagMarkerViewModel = null;
        
        foreach (var question in _standardQuestionCardSource.Items)
        {
            question?.Dispose();
        }
        _standardQuestionCardSource.Clear();
        
        foreach (var questionCard in  _questionCardSource.Items)
        {
            questionCard?.Dispose();
        }
        _questionCardSource.Clear();
        
        base.Dispose();
    }
}