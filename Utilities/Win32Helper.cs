using System;
using System.Runtime.InteropServices;

namespace WallTrek.Utilities
{
    public static class Win32Helper
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        public static void BringWindowToFront(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return;

            // If window is minimized, restore it
            if (IsIconic(windowHandle))
            {
                ShowWindow(windowHandle, SW_RESTORE);
            }
            else
            {
                ShowWindow(windowHandle, SW_SHOW);
            }

            // Bring window to foreground
            SetForegroundWindow(windowHandle);
        }
    }
}