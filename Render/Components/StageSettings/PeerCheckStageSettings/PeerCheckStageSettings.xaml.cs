using System;
using System.Reactive.Linq;
using ReactiveUI;
using Render.Components.StageSettings.RadioButtons;
using Render.Extensions;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Components.StageSettings.PeerCheckStageSettings
{
    public partial class PeerCheckStageSettings
    {
        public PeerCheckStageSettings()
        {
            InitializeComponent();

            RadioButtonStack.Children.ForEach(view =>
            {
                if (view is RadioButton radioButton)
                {
                    radioButton.ControlTemplate = new ControlTemplate(typeof(RadioButtonsView));
                }
            });

            this.WhenActivated(d =>
            {
                d(this.Bind(ViewModel, vm => vm.NoSelfCheck,
                    v => v.NoSelfCheckToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.StageName,
                    v => v.StageName.Text));

                d(this.WhenAnyValue(x => x.ViewModel.SelectedState).Subscribe(SetRadioButtonState));
                
                d(this.Bind(ViewModel, vm => vm.CheckRequirePassageListen,
                    v => v.RequirePassageCheckToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.CheckRequireSectionListen,
                    v => v.RequireSectionCheckToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateRequireNoteListen,
                    v => v.RequireReviseNoteListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.CheckRequireNoteListen,
                    v => v.RequirePeerCheckNoteListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateDoPassageReview,
                    v => v.DoPassageReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateRequirePassageReview,
                    v => v.RequirePassageReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateAllowEditing,
                    v => v.AllowEditingToggle.IsToggled));

                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.RequireSectionCheckToggle.IsEnabled, state => 
                        state == SelectedState.SectionListenOnly || state == SelectedState.Both ));
                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.RequireSectionCheckLabel.IsEnabled, state => 
                        state == SelectedState.SectionListenOnly || state == SelectedState.Both ));
                
                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.RequirePassageCheckToggle.IsEnabled, state => 
                        state == SelectedState.PassageListenOnly || state == SelectedState.Both));
                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.RequirePassageCheckLabel.IsEnabled, state => 
                        state == SelectedState.PassageListenOnly || state == SelectedState.Both));
                
                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.SelectedOptionLabel.Text, state =>
                    {
                        string value;

                        switch (state)
                        {
                            case SelectedState.Both:
                                value = AppResources.Both;
                                break;
                            case SelectedState.SectionListenOnly:
                                value = AppResources.SectionListenOnly;
                                break;
                            case SelectedState.PassageListenOnly:
                                value = AppResources.PassageListenOnly;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(state), state, null);
                        }

                        return value;
                    }));
                
                d(this.OneWayBind(ViewModel, vm => vm.TranslateDoPassageReview,
                    v => v.RequirePassageReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.TranslateDoPassageReview,
                    v => v.RequirePassageReviewLabel.IsEnabled));
                
                
                d(this.WhenAnyValue(x => x.ViewModel.FlowDirection)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(flowDirection =>
                    {
                        TopLevelElement.SetValue(FlowDirectionProperty, flowDirection);
                    }));
            });
        }

        #region Toggle event methods

        private void NoSelfCheckToggleTapped(object sender, EventArgs e)
        {
            NoSelfCheckToggle.IsToggled = !NoSelfCheckToggle.IsToggled;
        }

        private void RequireSectionCheckToggleTapped(object sender, EventArgs e)
        {
            if (RequireSectionCheckToggle.IsEnabled)
            {
                RequireSectionCheckToggle.IsToggled = !RequireSectionCheckToggle.IsToggled;
            }
        }

        private void RequirePassageCheckToggleTapped(object sender, EventArgs e)
        {
            if (RequirePassageCheckToggle.IsEnabled)
            {
                RequirePassageCheckToggle.IsToggled = !RequirePassageCheckToggle.IsToggled;
            }
        }

        private void DoPassageReviewToggleTapped(object sender, EventArgs e)
        {
            DoPassageReviewToggle.IsToggled = !DoPassageReviewToggle.IsToggled;
        }

        private void RequirePassageReviewToggleTapped(object sender, EventArgs e)
        {
            if (RequirePassageReviewToggle.IsEnabled)
            {
                RequirePassageReviewToggle.IsToggled = !RequirePassageReviewToggle.IsToggled;
            }
        }

        private void RequireReviseNoteListenToggleTapped(object sender, EventArgs e)
        {
            if (RequireReviseNoteListenToggle.IsEnabled)
            {
                RequireReviseNoteListenToggle.IsToggled = !RequireReviseNoteListenToggle.IsToggled;
            }
        }

        private void RequirePeerCheckNoteListenToggleTapped(object sender, EventArgs e)
        {
            if (RequirePeerCheckNoteListenToggle.IsEnabled)
            {
                RequirePeerCheckNoteListenToggle.IsToggled = !RequirePeerCheckNoteListenToggle.IsToggled;
            }
        }

        private void AllowEditingToggleTapped(object sender, EventArgs e)
        {
            AllowEditingToggle.IsToggled = !AllowEditingToggle.IsToggled;
        }

        #endregion

        private void SectionAndPassageReviewStackTapped(object sender, EventArgs e)
        {
            SectionAndPassageReviewStack.IsVisible = !SectionAndPassageReviewStack.IsVisible;

            SectionReviewAndPassageExpandIcon.IsVisible = !SectionAndPassageReviewStack.IsVisible;
            SectionReviewAndPassageCollapseIcon.IsVisible = SectionAndPassageReviewStack.IsVisible;
        }

        private void ReviseNoteListenStackTapped(object sender, EventArgs e)
        {
            ReviseNoteListenStack.IsVisible = !ReviseNoteListenStack.IsVisible;

            ReviseNoteListenExpandStackIcon.IsVisible = !ReviseNoteListenStack.IsVisible;
            ReviseNoteListenCollapseStackIcon.IsVisible = ReviseNoteListenStack.IsVisible;
        }
        
        private void PeerCheckNoteListenStackTapped(object sender, EventArgs e)
        {
            PeerCheckNoteListenStack.IsVisible = !PeerCheckNoteListenStack.IsVisible;

            PeerCheckNoteListenExpandStackIcon.IsVisible = !PeerCheckNoteListenStack.IsVisible;
            PeerCheckNoteListenCollapseStackIcon.IsVisible = PeerCheckNoteListenStack.IsVisible;
        }

        private void RevisePassageReviewStackTapped(object sender, EventArgs e)
        {
            RevisePassageReviewStack.IsVisible = !RevisePassageReviewStack.IsVisible;
            
            RevisePassageReviewExpandStackIcon.IsVisible = !RevisePassageReviewStack.IsVisible;
            RevisePassageReviewCollapseStackIcon.IsVisible = RevisePassageReviewStack.IsVisible;
        }

        private void HandleCheck(object sender, CheckedChangedEventArgs e)
        {
            var radioButton = (RadioButton)sender;
            if (ViewModel != null && e.Value)
            {
                ViewModel.SelectedState = (SelectedState)radioButton.Value;
            }
        }

        private void SetRadioButtonState(SelectedState state)
        {
            switch (state)
            {
                case SelectedState.Both:
                    Both.IsChecked = true;
                    break;
                case SelectedState.SectionListenOnly:
                    SectionListenOnly.IsChecked = true;
                    break;
                case SelectedState.PassageListenOnly:
                    PassageListenOnly.IsChecked = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}
