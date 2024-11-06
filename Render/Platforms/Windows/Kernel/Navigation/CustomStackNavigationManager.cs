using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Render.Utilities;
using ContentPresenter = Microsoft.UI.Xaml.Controls.ContentPresenter;
using Frame = Microsoft.UI.Xaml.Controls.Frame;
using Page = Microsoft.UI.Xaml.Controls.Page;

namespace Render.Platforms.Kernel
{
    /// <summary>
    /// Copy of base StackNavigationManager with modifications 
    /// to avoid WinRT native navigation crash.
    /// See details: https://github.com/dotnet/maui/issues/22790
    /// TODO: Remove when bug will be fixed.
    /// </summary>
    public class CustomStackNavigationManager
    {
        private class ActionDisposable : IDisposable
        {
            private volatile Action _action;

            public ActionDisposable(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _action, null)?.Invoke();
            }
        }

        private static void Arrange(IView view, FrameworkElement frameworkElement)
        {
            var rect = new Rect(0, 0, frameworkElement.ActualWidth, frameworkElement.ActualHeight);

            if (!view.Frame.Equals(rect))
            {
                view.Arrange(rect);
            }
        }

        private static IDisposable OnLoaded(FrameworkElement frameworkElement, Action action)
        {
            if (frameworkElement?.IsLoaded is true)
            {
                action();
                return new ActionDisposable(() => { });
            }

            RoutedEventHandler routedEventHandler = null;
            ActionDisposable disposable = new ActionDisposable(() =>
            {
                if (routedEventHandler != null)
                {
                    frameworkElement.Loaded -= routedEventHandler;
                }
            });

            routedEventHandler = (_, __) =>
            {
                disposable.Dispose();
                action();
            };

            frameworkElement.Loaded += routedEventHandler;
            return disposable;
        }

        private IView _currentPage;
        private IMauiContext _mauiContext;
        private bool _connected;
        private Frame _navigationFrame;
        private Action _pendingNavigationFinished;

        protected NavigationRootManager WindowManager
        {
            get => _mauiContext.Services.GetRequiredService<NavigationRootManager>();
        }

        internal IStackNavigation? NavigationView { get; private set; }

        public IReadOnlyList<IView> NavigationStack { get; set; } = [];

        public IMauiContext MauiContext
        {
            get => _mauiContext;
        }

        public IView CurrentPage
        {
            get => _currentPage ?? throw new InvalidOperationException("CurrentPage cannot be null");
        }

        public Frame NavigationFrame
        {
            get => _navigationFrame ?? throw new InvalidOperationException("NavigationFrame Null");
        }

        public CustomStackNavigationManager(IMauiContext mauiContext)
        {
            _mauiContext = mauiContext;
        }

        public virtual void Connect(IStackNavigation navigationView, Frame navigationFrame)
        {
            _connected = true;

            if (_navigationFrame != null)
            {
                _navigationFrame.Navigated -= OnNavigated;
            }

            FirePendingNavigationFinished();

            navigationFrame.Navigated += OnNavigated;
            _navigationFrame = navigationFrame;

            NavigationView = (IStackNavigation)navigationView;

            if (WindowManager?.RootView is NavigationView paneView)
            {
                paneView.IsPaneVisible = true;
            }
        }

        public virtual void Disconnect(IStackNavigation navigationView, Frame navigationFrame)
        {
            _connected = false;

            if (_navigationFrame != null)
            {
                _navigationFrame.Navigated -= OnNavigated;
            }

            FirePendingNavigationFinished();

            _navigationFrame = null;
            NavigationView = null;
        }

        public virtual void NavigateTo(NavigationRequest args)
        {
            IReadOnlyList<IView> newPageStack = new List<IView>(args.NavigationStack);
            var previousNavigationStack = NavigationStack;
            var previousNavigationStackCount = previousNavigationStack.Count;
            bool initialNavigation = NavigationStack.Count == 0;

            // User has modified navigation stack but not the currently visible page
            // So we just sync the elements in the stack
            if (!initialNavigation &&
                newPageStack[newPageStack.Count - 1] ==
                previousNavigationStack[previousNavigationStackCount - 1])
            {
                SyncBackStackToNavigationStack(newPageStack);
                NavigationStack = newPageStack;
                FireNavigationFinished();
                return;
            }

            NavigationTransitionInfo? transition = GetNavigationTransition(args);
            _currentPage = newPageStack[newPageStack.Count - 1];

            _ = _currentPage ?? throw new InvalidOperationException("Navigation Request Contains Null Elements");
            if (previousNavigationStack.Count < args.NavigationStack.Count)
            {
                Type destinationPageType = GetDestinationPageType();
                NavigationStack = newPageStack;
                NavigationFrame.Navigate(destinationPageType, null, transition);
            }
            else if (previousNavigationStack.Count == args.NavigationStack.Count)
            {
                Type destinationPageType = GetDestinationPageType();
                NavigationStack = newPageStack;
                NavigationFrame.Navigate(destinationPageType, null, transition);
            }
            else
            {
                NavigationStack = newPageStack;
                NavigationFrame.GoBack(transition);
            }
        }

        protected virtual Type GetDestinationPageType()
        {
            return typeof(Page);
        }

        protected virtual NavigationTransitionInfo GetNavigationTransition(NavigationRequest args)
        {
            if (args.Animated is false)
            {
                return null;
            }

            // GoBack just plays the animation in reverse so we always just return the same animation
            return new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight };
        }

        private void SyncBackStackToNavigationStack(IReadOnlyList<IView> pageStack)
        {
            // Back stack depth doesn't count the currently visible page
            var nativeStackCount = NavigationFrame.BackStackDepth + 1;

            // BackStack entries have no hard relationship with a specific IView
            // Everytime an entry is about to become visible it just grabs whatever
            // IView is going to be the visible so all we're doing here is syncing
            // up the number of things on the stack
            while (nativeStackCount != pageStack.Count)
            {
                if (nativeStackCount > pageStack.Count)
                {
                    NavigationFrame.BackStack.RemoveAt(0);
                }
                else
                {
                    NavigationFrame.BackStack.Insert(0, new PageStackEntry(GetDestinationPageType(), null, null));
                }

                nativeStackCount = NavigationFrame.BackStackDepth + 1;
            }
        }

        // This is used to fire NavigationFinished back to the xplat view
        // Firing NavigationFinished from Loaded is the latest reliable point
        // in time that I know of for firing `NavigationFinished`
        // Ideally we could fire it when the `NavigationTransitionInfo` is done but
        // I haven't found a way to do that
        private async void OnNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            // If the user has inserted or removed any extra pages
            SyncBackStackToNavigationStack(NavigationStack);

            if (sender is not Frame frame || e.Content is not (Page page and FrameworkElement fe))
            {
                return;
            }

            // Occasionally, application crashes while setting the page's content
            Exception exception = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    SetContent(frame, page, MauiContext);
                    
                    exception = null;
                    break;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                await Task.Delay(25);
            }

            // If we crashed during previous step, try reset content with new context.
            if (exception is not null)
            {
                try
                {
                    SetContent(frame, page, new MauiContext(MauiContext.Services));
                }
                catch (Exception)
                {
                    FireNavigationFinished();

                    throw;
                }
            }

            _pendingNavigationFinished = () =>
            {
                if (page?.Content is not FrameworkElement pc)
                {
                    FireNavigationFinished();
                }
                else
                {
                    OnLoaded(pc, FireNavigationFinished);
                }

                if (NavigationView is IView view && _connected)
                {
                    Arrange(view, fe);
                }
            };

            OnLoaded(fe, FirePendingNavigationFinished);
        }

        private void SetContent(UIElement frame, UserControl page, IMauiContext mauiContext)
        {
            if (_currentPage is null)
            {
                return;
            }

            frame.XamlRoot ??= WindowManager.RootView.XamlRoot ?? WindowStateManager.Default.GetActiveWindow()?.Content.XamlRoot;
            page.XamlRoot ??= frame.XamlRoot;

            var presenter = page.Content as ContentPresenter ?? new ContentPresenter
            {
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch,
                XamlRoot = page.XamlRoot
            };

            var content = _currentPage.ToPlatform(mauiContext);
            content.XamlRoot ??= presenter.XamlRoot;

            presenter.Content = content;
            page.Content = presenter;
        }

        private void FireNavigationFinished()
        {
            _pendingNavigationFinished = null;
            NavigationView?.NavigationFinished(NavigationStack);
        }

        private void FirePendingNavigationFinished()
        {
            Interlocked.Exchange(ref _pendingNavigationFinished, null)?.Invoke();
        }
    }
}