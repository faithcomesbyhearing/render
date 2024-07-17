﻿using ReactiveUI;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.Configurator.SectionAssignment
{
    public partial class SectionAssignmentPage
    {
        public SectionAssignmentPage()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                    v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.SectionViewViewModel,
                    v => v.SectionView.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.TeamViewViewModel,
                    v => v.TeamView.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, 
                   v => v.ProceedButton.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, 
                   v => v.TitleBar.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ShowSectionView, 
                   v => v.SectionView.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowSectionView, 
                   v => v.TeamView.IsVisible, Selector));

                d(this.BindCommand(ViewModel, vm => vm.SelectSectionViewCommand,
                    v => v.SelectSectionViewTap));
                d(this.BindCommand(ViewModel, vm => vm.SelectTeamViewCommand,
                    v => v.SelectTeamViewTap));
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));
                d(this.WhenAnyValue(x => x.ViewModel.ShowSectionView)
                   .Subscribe(ChangeLabelColors));
            });
        }

        public bool Selector(bool isVisible)
        {
            return !isVisible;
        }

        private void ChangeLabelColors(bool showSectionView)
        {
            if (showSectionView)
            {
                SetButtonActive(SectionViewButton);
                SetButtonInactive(TeamViewButton);
            }
            else
            {
                SetButtonActive(TeamViewButton);
                SetButtonInactive(SectionViewButton);
            }
        }

        private void SetButtonActive(View labelButton)
        {
            var option = (ColorReference)ResourceExtensions.GetResourceValue("Option");
            var textColor = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
            labelButton.SetValue(BackgroundColorProperty, option);
            labelButton.SetValue(Label.TextColorProperty, textColor);
        }

        private void SetButtonInactive(View labelButton)
        {
            var offBackground = (ColorReference)ResourceExtensions.GetResourceValue("AlternateBackground");
            var offText = (ColorReference)ResourceExtensions.GetResourceValue("Option");
            labelButton.SetValue(BackgroundColorProperty, offBackground);
            labelButton.SetValue(Label.TextColorProperty, offText);
        }
    }
}