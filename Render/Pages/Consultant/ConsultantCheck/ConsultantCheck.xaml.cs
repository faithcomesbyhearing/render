using System.Reactive.Linq;
using ReactiveUI;
using Render.Extensions;

namespace Render.Pages.Consultant.ConsultantCheck;

public partial class ConsultantCheck
{
    public ConsultantCheck()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopLevelElement.FlowDirection));

            d(this.WhenAnyValue(x => x.ViewModel.MenuButtonSource.Items)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(buttons =>
                {
                    var source = BindableLayout.GetItemsSource(ButtonList);
                    if (source == null)
                    {
                        BindableLayout.SetItemsSource(ButtonList, buttons);
                    }
                }));

            d(this.OneWayBind(ViewModel, vm => vm.TranscriptionWindowViewModel,
                v => v.TranscriptionWindow.BindingContext));

            d(this.OneWayBind(ViewModel, vm => vm.SequencerPlayerViewModel,
                v => v.Sequencer.BindingContext));

            d(this.OneWayBind(ViewModel, vm => vm.BarPlayerViewModels,
                v => v.References.ItemsSource));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel,
                v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel,
                v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading,
                v => v.LoadingView.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.RevisionActionViewModel,
                v => v.RevisionComponent.BindingContext));

            d(this.WhenAnyValue(x => x.ViewModel.ReferencesPanelIsVisible).Subscribe(x =>
            {
                ReferencesStack.SetValue(IsVisibleProperty, x);
            }));
            d(this.WhenAnyValue(x => x.ViewModel.TranscribePanelIsVisible).Subscribe(x =>
            {
                TranscriptionWindow.SetValue(IsVisibleProperty, x);
            }));

            d(this.WhenAnyValue(
                    consultantCheck => consultantCheck.ReferencesStack.IsVisible,
                    consultantCheck => consultantCheck.TranscriptionWindow.IsVisible)
                .Subscribe(((bool ReferencesVisible, bool TranscriptionVisible) props) =>
                {
                    var referencesMargin = props.ReferencesVisible ? ReferencesStack.WidthRequest : 0;
                    var transcriptionMargin = props.TranscriptionVisible ? TranscriptionWindow.WidthRequest : 0;

                    Sequencer.WaveFormsMargin = new Thickness(
                        left: TopLevelElement.FlowDirection == FlowDirection.RightToLeft ? transcriptionMargin : referencesMargin,
                        top: Sequencer.WaveFormsMargin.Top,
                        right: TopLevelElement.FlowDirection == FlowDirection.RightToLeft ? referencesMargin : transcriptionMargin,
                        bottom: Sequencer.WaveFormsMargin.Bottom);
                }));
        });
    }
    protected override void Dispose(bool disposing)
    {
        ButtonList.Children.Where(c => c is IDisposable)
                           .Cast<IDisposable>()
                           .ForEach(d => d.Dispose());

        RevisionComponent?.Dispose();
        base.Dispose(disposing);
    }
}