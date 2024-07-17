using ReactiveUI;

namespace Render.Pages.Settings.AudioExport.StageView
{
    public partial class AudioExportStageView
    {
        public AudioExportStageView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.WhenAnyValue(x => x.ViewModel.Stages.Items)
                    .Subscribe(s =>
                    {
                        BindableLayout.SetItemsSource(Stages, s);
                    }));
            });
        }
    }
}