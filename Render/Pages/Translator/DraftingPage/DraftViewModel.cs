using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Audio;
using Render.Resources.Localization;
using Render.Services.AudioServices;

namespace Render.Pages.Translator.DraftingPage
{
    public enum DraftState
    {
        New,
        HasAudio,
        Selected
    }

    public class DraftViewModel : ViewModelBase
    {
        private ITempAudioService _tempAudioService;
        private IDisposable _setAudioDisposable;

        public Audio Audio { get; private set; }

        [Reactive] public DraftState DraftState { get; set; }

        public int Number { get; }

        public string Title { get; }

        [Reactive] public bool Selected { get; private set; }

        public ReactiveCommand<Unit, Unit> SelectCommand { get; private set; }

        [Reactive]
        public bool IsPreviousDraft { get; set; }

        public DraftViewModel(int draftNumber, IViewModelContextProvider viewModelContextProvider,
            IObservable<bool> canSelect = default, bool isPreviousDraft = false) :
            base("Draft", viewModelContextProvider)
        {
            Number = draftNumber;
            Title = string.Format(AppResources.DraftTitle, draftNumber);
            Selected = false;
            IsPreviousDraft = isPreviousDraft;
            if (canSelect == default)
            {
                canSelect = this.WhenAnyValue(x => x.Audio).Select(x => x != null);
            }
            SelectCommand = ReactiveCommand.Create(Select, canSelect);
        }

        public void Select()
        {
            Selected = true;
            DraftState = DraftState == DraftState.HasAudio ? DraftState.HasAudio : DraftState.Selected;
        }

        public void Deselect()
        {
            Selected = false;
            if (Audio.TemporaryDeleted)
            {
                //draft with temporary deleted audio should have a plus sign (same as new draft) in UI
                DraftState = DraftState.New;
                return;
            }
            DraftState = DraftState == DraftState.HasAudio ? DraftState.HasAudio : DraftState.New;
        }

        public bool HasAudio => Audio != null && Audio.HasAudio;
        public bool TemporaryDeleted => Audio != null && Audio.TemporaryDeleted;

        public void SetAudio(Audio audio, string audioPath = null)
        {
            Audio = audio;

            _setAudioDisposable?.Dispose();
            _setAudioDisposable = Audio
                .WhenAnyValue(x => x.Data)
                .Subscribe(data =>
                {
                    SetDraftState();
                    _tempAudioService?.Dispose();
                    if (Audio.Data.Length > 0)
                    {
                        _tempAudioService = ViewModelContextProvider.GetTempAudioService(Audio, audioPath, mutable: true);
                    }
                });
        }

        public string GetAudioPath()
        {
            return _tempAudioService?.SaveTempAudio();
        }

        /// <summary>
        /// Raise audio data update and recalculate draft state
        /// </summary>
        public void TriggerUpdate()
        {
            SetDraftState();
        }

        private void SetDraftState()
        {
            if (Audio.HasAudio)
            {
                DraftState = DraftState.HasAudio;
            }
            else if (Selected)
            {
                DraftState = DraftState.Selected;
            }
            else
            {
                DraftState = DraftState.New;
            }
        }

        public override void Dispose()
        {
            Audio = null;

            _tempAudioService?.Dispose();
            _tempAudioService = null;

            SelectCommand?.Dispose();
            SelectCommand = null;

            base.Dispose();
        }
    }
}