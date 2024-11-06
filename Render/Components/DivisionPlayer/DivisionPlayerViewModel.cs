using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Components.Scroller;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Services.AudioServices;

namespace Render.Components.DivisionPlayer
{
    public class DivisionPlayerViewModel : BarPlayerViewModel, IDivisionPlayerViewModel
    {
        private const double MillisecondsPerSeconds = 1000d;
        
        private PassageNumber _passageNumber;
        private TimeMarkers _fullPassageTimeMarker;
        private IBreathPauseAnalyzer _breathPauseAnalyzer;
        
        public SectionReferenceAudio ReferenceAudio { get; private set; }
        public double SecondsPerScreen { get; private set; }
        [Reactive]
        public string ReferenceName { get; private set; }
        [Reactive]
        public bool IsLocked { get; private set; }
        [Reactive]
        public int DivisionsCount { get; private set; }

        public ReactiveCommand<Unit, Unit> ChangeLockStateCommand { get; }
        
        public DynamicDataWrapper<BreathPauseViewModel> BreathPauses { get; private set; } = new();

        public IObservableCollection<DivisionViewModel> Divisions { get; private set; } =
            new ObservableCollectionExtended<DivisionViewModel>();
        
        public ScrollerViewModel ScrollerViewModel { get; private set; }

        public bool IsAudioAvailable
        {
            get => ReferenceAudio?.HasAudio is true && 
                  _breathPauseAnalyzer?.IsAudioLoaded is true;
        }

        public DivisionPlayerViewModel(
            IViewModelContextProvider viewModelContextProvider,
            SectionReferenceAudio referenceAudio,
            PassageNumber passageNumber,
            TimeMarkers passageTimeMarkers,
            double secondsPerScreen = 15) :
            base(
                referenceAudio,
                viewModelContextProvider,
                ActionState.Optional,
                referenceAudio.Reference.Name,
                playerPositionInList: 0,
                passageTimeMarkers)
        {
            SecondsPerScreen = secondsPerScreen;
                
            ReferenceName = referenceAudio.Reference.Name;
            ReferenceAudio = referenceAudio;
            _passageNumber = passageNumber;
            _fullPassageTimeMarker = passageTimeMarkers;

            IsLocked = referenceAudio.LockedReferenceByPassageNumbersList.Contains(passageNumber.Number);
            
            if (!IsLocked)
            {
                ChangeLockStateCommand = ReactiveCommand.Create(ChangeLockedState);
            }

            LoadBreathPauses();
            
            IsLocked = IsLocked || !IsAudioAvailable;

            ScrollerViewModel = new ScrollerViewModel(viewModelContextProvider);

            Disposables.Add(BreathPauses.Observable
                .WhenPropertyChanged(breathPause => breathPause.IsDivisionMarker)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(_ => { UpdateDivisions(); }));
            
            Disposables.Add(this
                .WhenAnyValue(p => p.AudioPlayerService.CurrentPosition)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(UpdateScroller));
            
            Disposables.Add(this
                .WhenAnyValue(p => p.ScrollerViewModel.ScrollerTranslationX)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateDivisionLabels()));

            var canPlayAudio = this.WhenAnyValue(x => x.IsLocked,
                x => x.IsAudioAvailable)
            .Select(((bool isLocked, bool isAudioAvailable) x) =>
                !x.isLocked && x.isAudioAvailable);

            PlayAudioCommand = ReactiveCommand.Create(AudioPlayerService.Play, canPlayAudio);
        }

        public override void Pause()
        {
            if (AudioPlayerService != null && AudioPlayerService.AudioPlayerState == AudioPlayerState.Playing)
            {
                AudioPlayerService.Pause();
            }
        }

        protected override void AudioPlayerServiceOnOnPlayerEnd()
        {
            AudioPlayerService.Seek(0);

            LogInfo("Division Player Ended", new Dictionary<string, string>
            {
                { "Division Player Title", AudioTitle },
                { "Division Player Position", PlayerPositionInList.ToString() }
            });
        }

        private void UpdateScroller(double currentPosition)
        {
            var screenPosition = currentPosition / SecondsPerScreen * ScrollerViewModel.VisibleScreenWidth -
                ScrollerViewModel.VisibleScreenWidth / 2;

            if (screenPosition > ScrollerViewModel.InputScrollX)
            {
                ScrollerViewModel.ScrollTo(screenPosition);
            }
        }
        
        private void UpdateDivisionLabels()
        {
            var scrollerPosition = ScrollerViewModel.InputScrollX;
            
            var absolutePosition = 0d;
            
            var screenStart = scrollerPosition - ScrollerViewModel.VisibleScreenWidth / 2;
            var screenFinish = scrollerPosition + ScrollerViewModel.VisibleScreenWidth / 2;
            
            foreach (var division in Divisions)
            {
                var chunkLength = division.ChunkDuration * division.Scale / MillisecondsPerSeconds;
                
                var chunkStart = absolutePosition;
                var chunkFinish = absolutePosition + chunkLength;
                
                absolutePosition += chunkLength;
                
                var leftMargin = Math.Max(screenStart - chunkStart, 0);
                var rightMargin = Math.Max(chunkFinish - screenFinish, 0);
                
                division.UpdateLabel(leftMargin, rightMargin);
            }
        }
        
        public void ApplyChanges()
        {
            if (IsLocked && !ReferenceAudio.LockedReferenceByPassageNumbersList.Contains(_passageNumber.Number))
            {
                ReferenceAudio.LockedReferenceByPassageNumbersList.Add(_passageNumber.Number);
            }
            else
            {
                var newPassageReferences = GetPassageReferences();
                if (!newPassageReferences.Any())
                {
                    return;
                }
                
                ReferenceAudio.PassageReferences.RemoveAll(p => p.PassageNumber.Number == _passageNumber.Number);
                ReferenceAudio.PassageReferences.AddRange(newPassageReferences);
                ReferenceAudio.SetPassageReferences(ReferenceAudio.PassageReferences);
            }
        }

        private List<PassageReference> GetPassageReferences()
        {
            var startMarker = _fullPassageTimeMarker.StartMarkerTime;
            var divisionIndex = 0;

            var passageReferences = new List<PassageReference>();

            
            foreach (var d in Divisions)
            {
                passageReferences.Add(
                    new PassageReference(
                        new PassageNumber(_passageNumber.Number, ++divisionIndex),
                        new TimeMarkers(
                            startMarker,
                            startMarker + d.ChunkDuration
                        )));
                startMarker += d.ChunkDuration;
            }

            return passageReferences;
        }

        private void ChangeLockedState()
        {
            IsLocked = !IsLocked;

            if (IsLocked)
            {
                Divisions.Clear();
                foreach (var breathPauseViewModel in BreathPauses.Items)
                {
                    breathPauseViewModel.IsDivisionMarker = false;
                }
            }

            AudioPlayerService.Stop();
            ScrollerViewModel.ScrollTo(0);
        }

        private IEnumerable<int> GetAudioBreathAndPauses()
        {
            _breathPauseAnalyzer = ViewModelContextProvider.GetBreathPauseAnalyzer();
            _breathPauseAnalyzer.LoadAudioAndFindBreathPauses(ViewModelContextProvider.GetTempAudioService(ReferenceAudio));
			_breathPauseAnalyzer.RemoveLastExtraDivision();

			var divisions = new List<int>();
            foreach (var division in _breathPauseAnalyzer.Division)
            {
                if (_fullPassageTimeMarker.TimeWithinRange(division))
                {
                    if (division - _fullPassageTimeMarker.StartMarkerTime > MillisecondsPerSeconds)
                    {
                        divisions.Add(division - _fullPassageTimeMarker.StartMarkerTime);
                    }
                }
            }
            
            return divisions;
        }

        private void LoadBreathPauses()
        {
            var breathPausePositions = GetAudioBreathAndPauses().OrderBy(p => p).ToList();

            var absolutePosition = 0;
            foreach (var breathPause in breathPausePositions)
            {
                BreathPauses.Add(new BreathPauseViewModel(
                    ViewModelContextProvider,
                    position: breathPause,
                    chunkDuration: breathPause - absolutePosition));
                absolutePosition = breathPause;
            }
        }

        private void UpdateDivisions()
		{
			Divisions.Clear();

			var absolutePosition = 0;
			var divisionIndex = 0;
			var divisions = BreathPauses.Items
				.Where(bp => bp.IsDivisionMarker)
				.OrderBy(bp => bp.Position)
				.ToList();

			foreach (var division in divisions)
			{
				Divisions.Add(new DivisionViewModel(
					chunkDuration: division.Position - absolutePosition,
					text: $"{_passageNumber.Number}.{++divisionIndex}",
					scale: division.Scale));
				absolutePosition = division.Position;
			}

			if (divisions.Any())
			{
				var scale = divisions.First().Scale;

				Divisions.Add(new DivisionViewModel(
					chunkDuration: _fullPassageTimeMarker.EndMarkerTime - _fullPassageTimeMarker.StartMarkerTime -
								   absolutePosition,
					text: $"{_passageNumber.Number}.{++divisionIndex}",
					scale: scale));
			}

			DivisionsCount = Divisions.Count;

			UpdateDivisionLabels();
		}

		public void UpdateScale(double scale)
        {
            foreach (var breathPause in BreathPauses.Items)
            {
                breathPause.Scale = scale;
            }
            
            foreach (var division in Divisions)
            {
                division.Scale = scale;
            }
        }
        
        public override void Dispose()
        {
            _breathPauseAnalyzer = null;
            _fullPassageTimeMarker = null;
            _passageNumber = null;
            ReferenceAudio = null;
            
            BreathPauses.Items.DisposeCollection();
            BreathPauses = null;
            
            ScrollerViewModel.Dispose();
            ScrollerViewModel = null;
            
            Divisions.Clear();
            Divisions = null;

            base.Dispose();
        }
    }
}