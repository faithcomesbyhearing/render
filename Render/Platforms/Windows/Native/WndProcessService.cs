using Windows.Graphics;
using Microsoft.UI.Windowing;

namespace Render.Native
{
    internal class WndProcessService
    {
        private static Microsoft.UI.Xaml.Window _coreWindow;

        //Window process message key's
        private const int WmSysCommand = 0x0112;
        private const int ScRestoreDown = 0xF120;
        private const int ScResize = 0x0005;
        private const int WmSizing = 0x0214;
        private const int ScMaximized = 0xF030;
        private const int ScMinimized = 0xF020;
        private const int WmNotFullScreenInitialize = 0x18;

        private bool _isManualRestoreDown;
        private bool _isMinimized;
        private bool _isManualResize;

        private const int Width = 1280;
        private const int Height = 1024;

        private IntPtr _oldWndProc;

        public static void StartProcess(Microsoft.UI.Xaml.Window coreWindow)
        {
            var srvc = new WndProcessService(coreWindow);
            srvc.SetWndProcess();
        }

        private WndProcessService(Microsoft.UI.Xaml.Window coreWindow)
        {
            _coreWindow = coreWindow;
        }

        public void SetWndProcess()
        {
            _oldWndProc = WndProc.SetWndProc(_coreWindow, WindowProcess);
        }

        //here is a delegate callback that reacts to any action in the application
        private IntPtr WindowProcess(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
        {
            switch (message)
            {
                //if the application did not start in full screen, set the required values for the window
                case WmNotFullScreenInitialize when wParam == 0x1:
                    ResizeWindow();
                    break;

                case WmSysCommand:
                {
                    switch (wParam)
                    {
                        //maximized button click
                        case ScMaximized:
                            _isMinimized = false;
                            break;
                        //minimized button clicked
                        case ScMinimized:
                            _isMinimized = true;
                            break;
                        //restore down 
                        case ScRestoreDown:
                            _isManualRestoreDown = !_isMinimized;
                            break;
                    }

                    break;
                }

                //handling the case if the main window was resized using the restore down , minimize or maximized buttons
                case ScResize when _isManualRestoreDown 
                                   && _isManualResize is false:
                    ResizeWindow();
                    _isManualRestoreDown = false;
                    break;

                //handling the case if the main window was manually resized by dragging the application border
                case WmSizing:
                    _isManualResize = true;
                    break;
                
            }

            // Call the "base" WndProc
            return Interop.CallWindowProc(_oldWndProc, hwnd, message, wParam, lParam);
        }

        private static void ResizeWindow()
        {
            var window = _coreWindow.AppWindow;
            if (window != null && window.Presenter.Kind != AppWindowPresenterKind.FullScreen)
            {
                window.Resize(new SizeInt32(Width, Height));
            }
        }
    }
}