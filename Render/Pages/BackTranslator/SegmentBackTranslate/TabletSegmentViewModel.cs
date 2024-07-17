using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Sections;

namespace Render.Pages.BackTranslator.SegmentBackTranslate
{
    public class TabletSegmentViewModel : ViewModelBase
    {
        public SegmentBackTranslation SegmentBackTranslation { get; set; }
        
        [Reactive]
        public bool Selected { get; set; }
        
        [Reactive]
        public bool CanCombine { get; set; }
        
        public readonly double PassageStartTime;

        public readonly ReactiveCommand<Unit, Unit> SelectOnClickCommand;

        [Reactive]
        public string Title { get; set; }
     
        [Reactive]
        public double Duration { get; set; }
        
        [Reactive]
        public double CurrentTime { get; set; }

        //Duration of the whole section
        public double TotalDuration;
        
        public string Timer { get; private set; }
        [Reactive]
        public string TimeDisplay { get; set; }
        
        [Reactive] public string CurrentPositionTime { get; set; }
        [Reactive] public string DurationTime { get; set; }
        
        public Passage Passage;
        private double _secondsOnScreen;

        [Reactive]
        public bool Recorded { get; set; }
       
        [Reactive]
        public bool HasTranscription { get; set; }
        
        public bool InCombineMode { get; }
        public bool TranscribeMode { get; }
        public int StartTime { get; }
        public int EndTime { get; }
        public byte [] DraftAudioData { get; }

        public TabletSegmentViewModel(
            byte[] draftAudioData,
            SegmentBackTranslation segmentBackTranslation,
            Passage passage,
            double passageStartTime,
            string title,
            Action<TabletSegmentViewModel> onSelectedCallback,
            IViewModelContextProvider viewModelContextProvider,
            double secondsOnScreen,
            int startTime,
            int endTime,
            bool inCombineMode = false,
            bool transcribeMode = false) :base("TabletSegment", viewModelContextProvider)
        {
            StartTime = startTime;
            EndTime = endTime;
            _secondsOnScreen = secondsOnScreen;
            DraftAudioData = draftAudioData;
            SegmentBackTranslation = segmentBackTranslation;
            Passage = passage;
            PassageStartTime = passageStartTime;
            Title = title;
            InCombineMode = inCombineMode;
            TranscribeMode = transcribeMode;
            HasTranscription = segmentBackTranslation.Transcription != null;
            Duration = (double)(endTime - startTime) / 1000;
            Disposables.Add(this.WhenAnyValue(x => x.SegmentBackTranslation)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(d =>
                {
                    if (!transcribeMode)
                    {
                        Recorded = d != null && d.HasAudio;
                    }

                    else
                    {
                        HasTranscription = d?.Transcription != null;
                    }
                }));

            SelectOnClickCommand = ReactiveCommand.Create(() =>
            {
                if (!inCombineMode)
                {
                    onSelectedCallback(this);
                }
            });

            var totalTime = TimeSpan.FromSeconds(Duration);
            Timer = totalTime.TotalSeconds < 1 ? "<1" : $"{totalTime:mm\\:ss}";
            
            Disposables.Add(this.WhenAnyValue(x => x.CurrentTime, x=> x.Duration)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(d =>
                {
                    TimeDisplay = Utilities.Utilities.GetTimeDisplay(d.Item1, Duration);
                    CurrentPositionTime = $"{Utilities.Utilities.GetFormattedTime(d.Item1)}";
                    DurationTime = $" | {Utilities.Utilities.GetFormattedTime(d.Item2)}";
                }));
        }
        
        public double GetWidthPerSecond()
        {
            if (Application.Current == null)
            {
                return 1;
            }

            var width = Application.Current.MainPage.Width;
            if (TotalDuration == 0)
            {
                return width / _secondsOnScreen;
            }
            return TotalDuration < _secondsOnScreen ? width / TotalDuration : width / _secondsOnScreen;
        }
    }
}