# WallTrek

WallTrek is a WinUI 3 application that generates AI-powered wallpapers using OpenAI's DALL-E 3 API. The application runs in the system tray and can automatically generate and set new wallpapers at specified intervals.

![Screenshot](assets/walltrek_screenshot.png)

## Features

- **AI-powered wallpaper generation** using OpenAI's DALL-E 3
- **Random prompt generation** using OpenAI's o3 model for creative variety
- **Prompt history management** with search, favorites, and usage tracking
- **System tray integration** with minimal UI footprint
- **Automatic wallpaper generation** with configurable intervals and source modes
- **Database persistence** for prompt and image history using SQLite
- **Image management** - view, set as background, or delete generated images
- **Windows startup integration** - optional run on system startup
- **EXIF metadata preservation** with original prompts embedded in images
- **Multi-view interface** for generation, history, and settings management

## Requirements

- Windows 10/11
- .NET 9.0 Runtime
- OpenAI API key

## Installation

1. Clone the repository
2. Restore NuGet packages: `dotnet restore`
3. Build the project: `dotnet build`
4. Run the project: `dotnet run`

**Note**: VS Code is supported, though for some XAML errors may need Visual Studio for more details.

### Configuration

1. **Initial Setup**: Right-click the system tray icon to open the application
2. **API Configuration**: Navigate to Settings and enter your OpenAI API key
3. **Auto-Generation**: Configure generation interval and choose between:
   - **Current Prompt**: Use your saved prompt for auto-generation
   - **Random Prompts**: Generate new AI-created prompts automatically
4. **Startup Options**: Enable "Run on Windows startup" for automatic launching
5. **Prompt Management**: Use the Prompt History view to manage and favorite prompts

### Generated Content

- **Image Storage**: Wallpapers saved to `%USERPROFILE%\Pictures\WallTrek\`
- **File Naming**: Includes timestamp and prompt in filename for easy identification
- **Metadata**: EXIF data contains original generation prompt for reference
- **Database Tracking**: All prompts and images tracked in local SQLite database
- **Auto-Wallpaper**: Desktop wallpaper automatically updated upon generation
- **History Access**: View, search, and manage all generated content through the UI

## Architecture

### User Interface

- **Views/MainView**: Primary wallpaper generation interface
- **Views/PromptHistoryView**: Search, browse, and manage prompt history with favorites
- **Views/SettingsView**: API configuration, auto-generation, and startup settings
- **System Tray**: Minimal footprint with quick access to all features

### Core Services

- **Services/ImageGenerator**: OpenAI DALL-E 3 API integration for wallpaper creation
- **Services/PromptGeneratorService**: AI-powered random prompt generation using OpenAI's o3
- **Services/DatabaseService**: SQLite persistence for prompt and image history
- **Services/AutoGenerateService**: Configurable timer-based automatic generation
- **Services/StartupManager**: Windows registry integration for startup functionality
- **Services/Wallpaper**: Desktop wallpaper integration via Win32 API

### Data Storage

- **Settings**: `%APPDATA%\WallTrek\settings.json` - Application configuration
- **Database**: `%APPDATA%\WallTrek\walltrek.db` - Prompt and image history
- **Images**: `%USERPROFILE%\Pictures\WallTrek\` - Generated wallpaper files

### Technical Stack

- **.NET 9.0** with **WinUI 3** (Windows App SDK 1.7.250606001)
- **OpenAI API v2.1.0** for image and prompt generation
- **SQLite** via Microsoft.Data.Sqlite for data persistence
- **H.NotifyIcon.WinUI** for system tray functionality
- **System.Drawing.Common** for image processing and metadata

## Packaging

Build for distribution: Self-Contained (~80 MB includes .NET 9 and Windows App SDK)

```bash
dotnet publish -c Release -r win-x64
```

Output will be in `bin/Release/net9.0-windows10.0.19041.0/win-x64/publish/`

## License

This project is open source. MIT license.