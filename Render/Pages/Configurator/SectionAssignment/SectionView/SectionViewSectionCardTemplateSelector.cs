namespace Render.Pages.Configurator.SectionAssignment.SectionView
{
    public class SectionViewSectionCardTemplateSelector : DataTemplateSelector
    {
        public SectionViewSectionCardTemplateSelector()
        {
            _tabletTemplate = new DataTemplate (typeof (TabletSectionViewSectionCard));
        }
        
        protected override DataTemplate OnSelectTemplate (object item, BindableObject container)
        {
            return _tabletTemplate;
        }
        
        private readonly DataTemplate _tabletTemplate;
    }
}