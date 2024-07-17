using ReactiveUI;

namespace Render.Pages.Settings.AudioExport.AllSectionsView
{
    public partial class AllSectionsView
    {
        public AllSectionsView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.WhenAnyValue(x => x.ViewModel.Sections)
                    .Subscribe(source =>
                    {
                        BindableLayout.SetItemsSource(SectionList, source);
                    }));
            });
        }
    }
}