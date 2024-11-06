using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reactive.Linq;

using ReactiveUI;
using Render.Components.ProfileAvatar;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.AppStart.Login
{
    public partial class Login
    {
        private const int _height = 200;

        public Login()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.ShowIconLogin, v => v.IconLoginStack.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.UserLoginViewModels, v => v.UserIconCollection.ItemsSource));
                d(this.OneWayBind(ViewModel, vm => vm.TopUserLoginViewModels, v => v.TopUserIconCollection.ItemsSource));
                d(this.BindCommandCustom(BackButtonTap, v => v.ViewModel.BackButtonCommand));
                d(this.BindCommandCustom(AddUserFrameGesture, v => v.ViewModel.AddNewUserCommand));
                d(this.BindCommandCustom(ViewAllUsersGesture, v => v.ViewModel.ViewAllUsersCommand));
                d(this.BindCommandCustom(LoginFrameGesture, v => v.ViewModel.TryLoginCommand));
                d(this.OneWayBind(ViewModel, vm => vm.ShowBackButton, v => v.BackButton.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowIconPassword, v => v.IconPasswordLoginStack.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowGrid, v => v.PasswordGrid.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowGrid, v => v.ValidationLabel.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowGrid, v => v.IconPassword.IsVisible, Selector));
                d(this.OneWayBind(ViewModel, vm => vm.UserLoginIconViewModel, v => v.UserLoginIcon.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.PasswordViewModel, v => v.IconPassword.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.PasswordGridViewModel, v => v.PasswordGrid.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ShowAllUsers, v => v.UsersSeparator.IsVisible, Selector));
                d(this.OneWayBind(ViewModel, vm => vm.ShowAllUsers, v => v.TopUserIconCollection.IsVisible, Selector));
                d(this.OneWayBind(ViewModel, vm => vm.PasswordGridViewModel.ValidationMessage, v => v.ValidationLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.loadingView.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.BackButton.Text, SetBackButtonDirection));
                
                d(this.WhenAnyValue(
                        x => x.ViewModel.ShowAllUsers,
                        x => x.ViewModel.UserLoginIconViewModel,
                        x => x.ViewModel.ShowIconPassword)
                    .Subscribe(args =>
                    {
                        var panelVisible = !args.Item1 &&
                                           (args.Item2 != null && !args.Item2.IsRenderUser || args.Item2 == null) &&
                                           !args.Item3;
                        ButtonStack.SetValue(IsVisibleProperty, panelVisible);
                        if (args.Item3)
                        {
                            ViewModel.PasswordViewModel.PutEntryInFocus(true);
                        }
                    }));
                d(this.WhenAnyValue(x => x.ViewModel.TopUserLoginViewModels)
                    .Subscribe(CalculateTopCollectionHeight));
                d(this.WhenAnyValue(x => x.ViewModel.UserLoginViewModels)
                    .Subscribe(CalculateCollectionHeight));
                d(this.WhenAnyValue(x => x.ViewModel.Loading)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(loading =>
                    {
                        LoginButtonFrame.IsEnabled = !loading;

                        if (!loading)
                        {
                            LoginText.SetValue(Label.TextColorProperty, (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText"));
                            // LoginButtonFrame.SetValue(Frame.OpacityProperty, 1);
                        }
                    }));
            });
            //this provide a background color for the container of two buttons with full screen width
            ButtonStack.SetValue(WidthRequestProperty, Application.Current.MainPage.Width);
            LoginButtonFrame.GestureRecognizers.Add(
                new TapGestureRecognizer
                {
                    Command = new Command(async (o) =>
                    {
                        await LoginButtonFrame.ScaleTo(0.95, 100, Easing.CubicOut);
                        await LoginButtonFrame.ScaleTo(1, 100, Easing.CubicIn);
                    })
                });
        }

        private bool Selector(bool arg)
        {
            return !arg;
        }

        private string SetBackButtonDirection(FlowDirection flowDirection)
        {
            return flowDirection == FlowDirection.RightToLeft
                ? IconExtensions.GetIconGlyph(Icon.ChevronRight)
                : IconExtensions.GetIconGlyph(Icon.ChevronLeft);
        }

        private async void OnAddUserFrameTapped(object sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                await ViewModel.AddNewUserAsync();
            }
        }

        protected override bool OnBackButtonPressed()
        {
            ViewModel?.GoBackAsync();
            return true;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RenderTitle.FadeUpInAsync(distance: 50);
        }

        private void CalculateTopCollectionHeight(ReadOnlyObservableCollection<UserLoginIconViewModel> data)
        {
            var row = CalculateRowCount(data.Count);
            TopUserIconCollection.SetValue(HeightRequestProperty, row * _height);
        }

        private void CalculateCollectionHeight(ReadOnlyObservableCollection<UserLoginIconViewModel> data)
        {
            var row = CalculateRowCount(data.Count);
            UserIconCollection.SetValue(HeightRequestProperty, row * _height);
        }

        private int CalculateRowCount(int dataCount)
        {
            var row = 1;
            // UWP list 4 users per row
            if (dataCount > 4)
            {
                row = Int32.Parse(Math.Floor(dataCount / 4.0).ToString(CultureInfo.InvariantCulture));
            }
            return row;
        }
    }
}