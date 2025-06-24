using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WallTrek.Services
{
    public sealed class Wallpaper
    {
        Wallpaper() { }

        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);


        public static void Set(string wpaper)
        {
            RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

            if (key == null)
            {
                throw new Exception("Could not access registry key.");
            }

            // Fill
            key.SetValue(@"WallpaperStyle", "10"); // Fill
            key.SetValue(@"TileWallpaper", "0");

            SystemParametersInfo(SPI_SETDESKWALLPAPER,
                0,
                wpaper,
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }
}