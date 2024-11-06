using System.Reactive.Linq;
using ReactiveUI;

namespace Render.Components.StageSettings.DraftingStageSettings
{
    public partial class DraftingStageSettings
    {
        public DraftingStageSettings()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.Bind(ViewModel, vm => vm.StageName,
                    v => v.StageName.Text));
                d(this.Bind(ViewModel, vm => vm.DraftingStepName.StepName,
                    v => v.DraftingStepName.Text));
                
                d(this.Bind(ViewModel, vm => vm.TranslateDoSectionListen,
                    v => v.DoSectionListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateRequireSectionListen,
                    v => v.RequireSectionListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateDoPassageListen,
                    v => v.DoPassageListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateRequirePassageListen,
                    v => v.RequirePassageListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateDoSectionReview,
                    v => v.DoSectionReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateRequireSectionReview,
                    v => v.RequireSectionReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateDoPassageReview,
                    v => v.DoPassageReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateRequirePassageReview,
                    v => v.RequirePassageReviewToggle.IsToggled));

                d(this.OneWayBind(ViewModel, vm => vm.TranslateDoSectionListen,
                    v => v.RequireSectionListenToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.TranslateDoSectionListen,
                    v => v.RequireSectionListenLabel.IsEnabled));

                d(this.OneWayBind(ViewModel, vm => vm.TranslateDoPassageListen,
                    v => v.RequirePassageListenToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.TranslateDoPassageListen,
                    v => v.RequirePassageListenLabel.IsEnabled));

                d(this.OneWayBind(ViewModel, vm => vm.TranslateDoSectionReview,
                    v => v.RequireSectionReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.TranslateDoSectionReview,
                    v => v.RequireSectionReviewLabel.IsEnabled));

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

        private void DoSectionListenToggleTapped(object sender, EventArgs e)
        {
            DoSectionListenToggle.IsToggled = !DoSectionListenToggle.IsToggled;
        }

        private void RequireSectionListenToggleTapped(object sender, EventArgs e)
        {
            if (RequireSectionListenToggle.IsEnabled)
            {
                RequireSectionListenToggle.IsToggled = !RequireSectionListenToggle.IsToggled;
            }
        }

        private void DoPassageListenToggleTapped(object sender, EventArgs e)
        {
            DoPassageListenToggle.IsToggled = !DoPassageListenToggle.IsToggled;
        }

        private void RequirePassageListenToggleTapped(object sender, EventArgs e)
        {
            if (RequirePassageListenToggle.IsEnabled)
            {
                RequirePassageListenToggle.IsToggled = !RequirePassageListenToggle.IsToggled;
            }
        }

        private void DoSectionReviewToggleTapped(object sender, EventArgs e)
        {
            DoSectionReviewToggle.IsToggled = !DoSectionReviewToggle.IsToggled;
        }

        private void RequireSectionReviewToggleTapped(object sender, EventArgs e)
        {
            if (RequireSectionReviewToggle.IsEnabled)
            {
                RequireSectionReviewToggle.IsToggled = !RequireSectionReviewToggle.IsToggled;
            }
        }

        private void DoPassageReviewToggleTapped(object sender, EventArgs e)
        {
            DoPassageReviewToggle.IsToggled = !DoPassageReviewToggle.IsToggled;
        }

        private void RequirePassageReviewToggleTapped(object sender, EventArgs e)
        {
            if (RequirePassageListenToggle.IsEnabled)
            {
                RequirePassageReviewToggle.IsToggled = !RequirePassageReviewToggle.IsToggled;
            }
        }

        #endregion

        private void SectionListenStackTapped(object sender, EventArgs e)
        {
            SectionListenStack.IsVisible = !SectionListenStack.IsVisible;

            SectionListenExpandStackIcon.IsVisible = !SectionListenStack.IsVisible;
            SectionListenCollapseStackIcon.IsVisible = SectionListenStack.IsVisible;
        }

        private void PassageListenStackTapped(object sender, EventArgs e)
        {
            PassageListenStack.IsVisible = !PassageListenStack.IsVisible;

            PassageListenExpandStackIcon.IsVisible = !PassageListenStack.IsVisible;
            PassageListenCollapseStackIcon.IsVisible = PassageListenStack.IsVisible;
        }

        private void SectionReviewStackTapped(object sender, EventArgs e)
        {
            SectionReviewStack.IsVisible = !SectionReviewStack.IsVisible;

            SectionReviewExpandStackIcon.IsVisible = !SectionReviewStack.IsVisible;
            SectionReviewCollapseStackIcon.IsVisible = SectionReviewStack.IsVisible;
        }

        private void PassageReviewStackTapped(object sender, EventArgs e)
        {
            PassageReviewStack.IsVisible = !PassageReviewStack.IsVisible;

            PassageReviewExpandStackIcon.IsVisible = !PassageReviewStack.IsVisible;
            PassageReviewCollapseStackIcon.IsVisible = PassageReviewStack.IsVisible;
        }
    }
}