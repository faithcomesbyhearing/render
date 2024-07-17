using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;

namespace Render.Pages.Settings.AudioExport
{
    public partial class SectionToExportView
    {
        public SectionToExportView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
               d(this.OneWayBind(ViewModel, vm => vm.Section.Title.Text, v => v.SectionName.Text));
				d(this.OneWayBind(ViewModel, vm => vm.Section.Number, v => v.SectionNumber.Text)); 
               d(this.OneWayBind(ViewModel, vm => vm.Section.ScriptureReference, v => v.SectionReference.Text));
               d(this.OneWayBind(ViewModel, vm => vm.Selected, v => v.Checkmark.IsVisible));
               d(this.OneWayBind(ViewModel, vm => vm.NoAudioText, v => v.NoAudio.Text));
				d(this.BindCommand(ViewModel, vm => vm.SelectCommand, v => v.SelectTap));
				d(this.WhenAnyValue(x => x.ViewModel.Selected)
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Subscribe(BackgroundSelector));
               d(this.WhenAnyValue(x => x.ViewModel.HasSnapshot)
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Subscribe(OpacitySelector));
               d(this.OneWayBind(ViewModel, vm => vm.IsEmpty, v => v.NoSectionView.IsVisible));
			   d(this.OneWayBind(ViewModel, vm => vm.IsEmpty, v => v.CheckmarkFrame.IsVisible, Selector));
				d(this.OneWayBind(ViewModel, vm => vm.IsEmpty, v => v.NoAudio.IsVisible, Selector));
				d(this.OneWayBind(ViewModel, vm => vm.ShowSeparator, v => v.Separator.IsVisible));
               d(this.WhenAnyValue(x => x.ViewModel.ShowSeparator)
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Subscribe(showSeparator =>
                   {
                       Separator.SetValue(IsVisibleProperty, showSeparator);
                   }));
            });
        }
        private static bool Selector(bool arg)
        {
            return !arg;
        }

        private void OpacitySelector(bool hasSnapshot)
        {
            CheckmarkFrame.SetValue(OpacityProperty, hasSnapshot ? 1 : 0.3);
        }
        
        private void BackgroundSelector(bool selected)
        {
            CheckmarkFrame.SetValue(BackgroundColorProperty, selected ? ResourceExtensions.GetColor("Option") : ResourceExtensions.GetColor("SecondaryText"));
            SectionInfo.SetValue(OpacityProperty, selected? 0.5 : 1);
        }
    }
}