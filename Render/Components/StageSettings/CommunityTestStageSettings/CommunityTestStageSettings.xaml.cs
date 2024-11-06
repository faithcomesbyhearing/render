using System.Reactive.Linq;
using ReactiveUI;
using Render.Components.StageSettings.RadioButtons;
using Render.Extensions;
using Render.Resources.Localization;

namespace Render.Components.StageSettings.CommunityTestStageSettings
{
    public partial class CommunityTestStageSettings
    {
        public CommunityTestStageSettings()
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
                d(this.WhenAnyValue(x => x.ViewModel.SelectedState).Subscribe(SetRadioButtonState));
                d(this.Bind(ViewModel, vm => vm.AssignToTranslator,
                    v => v.AssignToTranslatorToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.StageName,
                    v => v.StageName.Text));
                d(this.Bind(ViewModel, vm => vm.ReviewStepName.StepName,
                    v => v.ReviewStepName.Text));
                d(this.Bind(ViewModel, vm => vm.ReviseStepName.StepName,
                    v => v.ReviseStepName.Text));
                d(this.Bind(ViewModel, vm => vm.ResponseSetupStepName.StepName,
                    v => v.ResponseSetupStepName.Text));
                d(this.Bind(ViewModel, vm => vm.RetellRequireSectionListen,
                    v => v.RetellRequireSectionListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.RetellRequirePassageListen,
                    v => v.RetellRequirePassageListenToggle.IsToggled));

                d(this.Bind(ViewModel, vm => vm.ResponseRequireContextListen,
                    v => v.RequireQuestionContextListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.ResponseRequireRecordResponse,
                    v => v.RequireRecordResponseToggle.IsToggled));

                //Only allow the user to turn off one of the checks in the stage - it's pointless without at least one
                d(this.Bind(ViewModel, vm => vm.ReviseRequireCommunityFeedback,
                    v => v.RequireCommunityFeedbackToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.ReviseRequireSectionListen,
                    v => v.RequireSectionListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.ReviseAllowEditing,
                    v => v.AllowEditingToggle.IsToggled));

                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.RetellRequirePassageListenToggle.IsEnabled,
                    state => state == RetellQuestionResponseSettings.Retell || state == RetellQuestionResponseSettings.Both));
                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.RetellRequirePassageListenLabel.IsEnabled,
                    state => state == RetellQuestionResponseSettings.Retell || state == RetellQuestionResponseSettings.Both));

                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.ResponseSetupStepNameBorder.IsEnabled,
                    state => state == RetellQuestionResponseSettings.QuestionAndResponse || state == RetellQuestionResponseSettings.Both));

                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.RequireQuestionContextListenToggle.IsEnabled,
                    state => state == RetellQuestionResponseSettings.QuestionAndResponse || state == RetellQuestionResponseSettings.Both));
                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.RequireQuestionContextListenLabel.IsEnabled,
                    state => state == RetellQuestionResponseSettings.QuestionAndResponse || state == RetellQuestionResponseSettings.Both));

                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.RequireRecordResponseToggle.IsEnabled,
                    state => state == RetellQuestionResponseSettings.QuestionAndResponse || state == RetellQuestionResponseSettings.Both));
                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.RequireRecordResponseLabel.IsEnabled,
                    state => state == RetellQuestionResponseSettings.QuestionAndResponse || state == RetellQuestionResponseSettings.Both));

                d(this.Bind(ViewModel, vm => vm.TranslateDoPassageReview,
                    v => v.DoPassageReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateRequirePassageReview,
                    v => v.RequirePassageReviewToggle.IsToggled));

                d(this.OneWayBind(ViewModel, vm => vm.TranslateDoPassageReview,
                    v => v.RequirePassageReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.TranslateDoPassageReview,
                    v => v.RequirePassageReviewLabel.IsEnabled));

                d(this.OneWayBind(ViewModel, vm => vm.SelectedState,
                    v => v.SelectedOptionLabel.Text, state =>
                    {
                        string value;

                        switch (state)
                        {
                            case RetellQuestionResponseSettings.Both:
                                value = AppResources.Both;
                                break;
                            case RetellQuestionResponseSettings.Retell:
                                value = AppResources.RetellOnly;
                                break;
                            case RetellQuestionResponseSettings.QuestionAndResponse:
                                value = AppResources.QuestionAndResponseOnly;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(state), state, null);
                        }

                        return value;
                    }));

                d(this.WhenAnyValue(x => x.ViewModel.FlowDirection)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(flowDirection =>
                    {
                        TopLevelElement.SetValue(FlowDirectionProperty, flowDirection);
                    }));
            });
        }

        #region Toggle event methods

        private void AssignToTranslatorToggleTapped(object sender, EventArgs e)
        {
            AssignToTranslatorToggle.IsToggled = !AssignToTranslatorToggle.IsToggled;
        }

        private void RetellRequirePassageListenToggleTapped(object sender, EventArgs e)
        {
            if (RetellRequirePassageListenToggle.IsEnabled)
            {
                RetellRequirePassageListenToggle.IsToggled = !RetellRequirePassageListenToggle.IsToggled;
            }
        }

        private void RetellRequireSectionListenToggleTapped(object sender, EventArgs e)
        {
            if (RetellRequireSectionListenToggle.IsEnabled)
            {
                RetellRequireSectionListenToggle.IsToggled = !RetellRequireSectionListenToggle.IsToggled;
            }
        }

        private void RequireQuestionContextListenToggleTapped(object sender, EventArgs e)
        {
            if (RequireQuestionContextListenToggle.IsEnabled)
            {
                RequireQuestionContextListenToggle.IsToggled = !RequireQuestionContextListenToggle.IsToggled;
            }
        }

        private void RequireRecordResponseToggleTapped(object sender, EventArgs e)
        {
            if (RequireRecordResponseToggle.IsEnabled)
            {
                RequireRecordResponseToggle.IsToggled = !RequireRecordResponseToggle.IsToggled;
            }
        }

        private void RequireSectionListenToggleTapped(object sender, EventArgs e)
        {
            var toggle = RequireSectionListenToggle;

            if (toggle.IsEnabled)
            {
                toggle.IsToggled = !toggle.IsToggled;
            }
        }

        private void RequireCommunityFeedbackToggleTapped(object sender, EventArgs e)
        {
            var toggle = RequireCommunityFeedbackToggle;

            if (toggle.IsEnabled)
            {
                toggle.IsToggled = !toggle.IsToggled;
            }
        }

        private void AllowEditingToggleTapped(object sender, EventArgs e)
        {
            AllowEditingToggle.IsToggled = !AllowEditingToggle.IsToggled;
        }
        #endregion

        private void RetellStackTapped(object sender, EventArgs e)
        {
            RetellStack.IsVisible = !RetellStack.IsVisible;

            RetellExpandStackIcon.IsVisible = !RetellStack.IsVisible;
            RetellCollapseStackIcon.IsVisible = RetellStack.IsVisible;
        }

        private void HandleCheck(object sender, CheckedChangedEventArgs e)
        {
            var radioButton = (RadioButton)sender;
            if (ViewModel != null && e.Value)
            {
                ViewModel.SelectedState = (RetellQuestionResponseSettings)radioButton.Value;
            }
        }

        private void SetRadioButtonState(RetellQuestionResponseSettings state)
        {
            switch (state)
            {
                case RetellQuestionResponseSettings.Both:
                    Both.IsChecked = true;
                    break;
                case RetellQuestionResponseSettings.Retell:
                    RetellOnly.IsChecked = true;
                    break;
                case RetellQuestionResponseSettings.QuestionAndResponse:
                    QuestionResponseOnly.IsChecked = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void PassageReviewStackTapped(object sender, EventArgs e)
        {
            PassageReviewStack.IsVisible = !PassageReviewStack.IsVisible;

            PassageReviewExpandStackIcon.IsVisible = !PassageReviewStack.IsVisible;
            PassageReviewCollapseStackIcon.IsVisible = PassageReviewStack.IsVisible;
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
    }
}