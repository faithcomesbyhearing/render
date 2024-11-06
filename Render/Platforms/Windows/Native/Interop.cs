using System.Runtime.InteropServices;

namespace Render.Native
{
    public class Interop
    {
        //The SetWindowLongPtr64 function allows you to change specific properties of a window,
        //such as its window style, extended window style, window procedure address, user data, and more.
        //It is commonly used for customizing window behavior or appearance.
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")] // 64-bit
        public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        //The CallWindowProc function allows you to explicitly call a window procedure (WndProc) associated with a specific window
        //It is commonly used when you’ve subclassed a window (changed its window procedure) and need to pass messages to the original window procedure.
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    }
}