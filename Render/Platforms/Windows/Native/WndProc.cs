using System.Runtime.InteropServices;
using WinRT.Interop;

namespace Render.Native
{
    public static class WndProc
    {
        public delegate IntPtr WndProcDelegate(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
        
        //The constant GWLP_WNDPROC with a value of -4 is used in Windows programming
        //to specify that we are modifying the window procedure (also known as the window callback function)
        //associated with a window. 
        private const int GWLP_WNDPROC = -4;
        
        // Make sure to hold a reference to the delegate so it doesn't get garbage
        // collected, or you'll get baffling ExecutionEngineExceptions when
        // Windows tries to call your function pointer which no longer points
        // to anything.
        private static WndProcDelegate _currDelegate = null;

        //Replace the default window procedure (window callback function) for a given window.
        public static IntPtr SetWndProc(Microsoft.UI.Xaml.Window coreWindow, WndProcDelegate newProc)
        {
            _currDelegate = newProc;
            var m_hwnd = WindowNative.GetWindowHandle(coreWindow);
            IntPtr newWndProcPtr = Marshal.GetFunctionPointerForDelegate(newProc);
            return Interop.SetWindowLongPtr64(m_hwnd, GWLP_WNDPROC, newWndProcPtr);
        }
    }
}
