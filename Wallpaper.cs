using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WallTrek
{
    public sealed class Wallpaper
    {
        Wallpaper() { }

        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public enum Style : int
        {
            Tiled,
            Centered,
            Stretched
        }

        public static void Set(string wpaper)
        {
            string tempPath = "";

            //using (Image img = Image.FromFile(Path.GetFullPath(wpaper)))
            //{
                tempPath = Path.Combine(Path.GetTempPath(), "wallpaper.bmp");

                //img.Save(tempPath, System.Drawing.Imaging.ImageFormat.Bmp);

                File.Copy(wpaper, tempPath, true);

            //}

            RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

            if (key == null)
            {
                throw new Exception("Could not access registry key.");
            }
            /*string wallpaperStyle = style == Style.Stretched ? "2" : "1";
            string tileWallpaper = style == Style.Tiled ? "1" : "0";

            key.SetValue(@"WallpaperStyle", wallpaperStyle);
            key.SetValue(@"TileWallpaper", tileWallpaper);*/

            // Fill
            key.SetValue(@"WallpaperStyle", "10");
            key.SetValue(@"TileWallpaper", "0");


            //string resourcePath = "Resources\\" + wpaper;
            SystemParametersInfo(SPI_SETDESKWALLPAPER,
                0,
                tempPath,
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }
}