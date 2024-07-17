using System.Diagnostics;
using Render.Resources;
using ReactiveUI;
using Render.Components.TitleBar.MenuActions;

#if DEBUG
using Render.Kernel;
using Render.Pages.Debug;
using Splat;
#endif

namespace Render.Components.TitleBar
{
    public partial class TitleBarMenu
    {
        public const double MenuPaddingTop = 85;
        public const double MenuPaddingRight = 10;
        public static readonly Thickness TopLevelElementPadding = new Thickness(0, 0, MenuPaddingRight, 0);

        public readonly double MenuWidth = 300 + MenuPaddingRight;

        // Calculate menu height based on sum of heights of nested elements as they have the correct size.
        // TopLevelElement height is not correct because the Popup control sets default height (300px).
        public double MenuHeight => UpperTransparentArea.Height + MenuBorder.Height;

        public TitleBarMenu()
        {
            InitializeComponent();
            AddDebugButton();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.TopLevelElement.FlowDirection));

                d(this.BindCommand(ViewModel, vm => vm.CloseCommand, v => v.CloseMenuGestureRecognizer));

                d(this.OneWayBind(ViewModel, vm => vm.Username, v => v.UserName.Text));
                d(this.OneWayBind(ViewModel, vm => vm.ShowUser, v => v.UserStack.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.Actions.Items, v => v.MenuActionsCollection.ItemsSource));
                d(this.OneWayBind(ViewModel, vm => vm.ShowActionItems, v => v.MenuActionsCollection.IsVisible));

                d(this.OneWayBind(ViewModel, vm => vm.DividePassageViewModel, v => v.DividePassageButton.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ShowDividePassage, v => v.DividePassageButton.IsVisible));

                d(this.OneWayBind(ViewModel, vm => vm.HomeViewModel, v => v.HomeButton.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ShowProjectHome, v => v.HomeButton.IsVisible));

                d(this.OneWayBind(ViewModel, vm => vm.SectionStatusActionViewModel, v => v.SectionPageButton.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ShowSectionStatus, v => v.SectionPageButton.IsVisible));

                d(this.OneWayBind(ViewModel, vm => vm.LogOutViewModel, v => v.LogOutButton.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ShowUser, v => v.LogOutButton.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ProjectListMenuViewModel, v => v.ProjectListButton.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ShowProjectList, v => v.ProjectListButton.IsVisible));

                d(this.OneWayBind(ViewModel, vm => vm.SyncViewModel, v => v.SyncButton.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ShowUser, v => v.SyncButton.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.AudioExportViewModel, v => v.AudioExport.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ShowAudioExport, v => v.AudioExport.IsVisible));
            });

#if DEMO
            SyncButton.HeightRequest = 0;
#endif
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            PageRelatedItemsSeparator.SetValue(IsVisibleProperty, PageRelatedItemsContainer.Children
                .Where(x => x.GetType() == typeof(MenuAction))
                .Any(item => ((MenuAction)item).IsVisible));

            ProjectRelatedItemsSeparator.SetValue(IsVisibleProperty, ProjectRelatedItemsContainer.Children
                .Where(x => x.GetType() == typeof(MenuAction))
                .Any(item => ((MenuAction)item).IsVisible));
        }

        [Conditional("DEBUG")]
        private void AddDebugButton()
        {
            var debugMenuItem = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Margin = 15,
                Children =
                {
                    new Label
                    {
                        WidthRequest = 50,
                        Margin = new Thickness(0, 0, 15, 0),
                        FontFamily = "Icons",
                        FontSize = 40,
                        Text = "",
                        TextColor = (Color)(ResourceExtensions.GetResourceValue("Blue") ?? Colors.Transparent)
                    },
                    new Label
                    {
                        Style = ResourceExtensions.GetResourceValue("MediumText") as Style,
                        VerticalOptions = LayoutOptions.Center,
                        Text = "Open Debug Page"
                    }
                },
                GestureRecognizers =
                {
                    new TapGestureRecognizer
                    {
                        Command = new Command(DebugButtonClicked)
                    }
                }
            };

            MenuActionsStack.Children.Add(debugMenuItem);
        }

        private void DebugButtonClicked()
        {
#if DEBUG
            var viewModelContextProvider = Locator.Current.GetService<IViewModelContextProvider>();

            viewModelContextProvider.GetMenuPopupService().Close();

            Application
                .Current
                .MainPage
                .Navigation
                .PushModalAsync(new DebugPage());
#endif
        }
    }
}