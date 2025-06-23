# WallTrek

WallTrek is a WinUI 3 application that generates AI-powered wallpapers using OpenAI's DALL-E 3 API. The application runs in the system tray and can automatically generate and set new wallpapers at specified intervals.

## Features

- AI-powered wallpaper generation using OpenAI's DALL-E 3
- System tray integration with minimal UI footprint
- Automatic wallpaper generation at configurable intervals
- Custom prompt support for personalized wallpapers
- EXIF metadata preservation with original prompts
- Automatic desktop wallpaper setting

## Requirements

- Windows 10/11
- .NET 9.0 Runtime
- OpenAI API key

## Installation

1. Clone the repository
2. Restore NuGet packages: `dotnet restore`
3. Build the project: `dotnet build`
4. Run the project: `dotnet run`

### Configuration

1. Right-click the system tray icon to access settings
2. Enter your OpenAI API key
3. Configure auto-generation interval (optional)
4. Set custom prompts for wallpaper generation

### Generated Content

- Wallpapers are saved to `%USERPROFILE%\Pictures\WallTrek\`
- Files include timestamp and prompt in the filename
- Images contain EXIF metadata with the original generation prompt
- Desktop wallpaper is automatically updated

## Architecture

### Core Components

- **App.xaml.cs**: Application entry point with system tray integration
- **MainWindow**: Primary UI for wallpaper generation
- **SettingsWindow**: Configuration interface

### Services

- **ImageGenerator**: OpenAI DALL-E 3 API integration
- **Settings**: JSON-based configuration management
- **AutoGenerateService**: Timer-based automatic generation
- **Wallpaper**: Windows desktop wallpaper integration

### Technical Stack

- **.NET 9.0** with **WinUI 3** (Windows App SDK 1.7.250606001)
- **OpenAI API** for image generation
- **H.NotifyIcon.WinUI** for system tray functionality
- **System.Drawing.Common** for image processing

## License

This project is open source. MIT license.