# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

WallTrek is a WinUI 3 application that generates AI-powered wallpapers using OpenAI's DALL-E 3 API. The application runs with a system tray icon and can automatically generate wallpapers at specified intervals.

## Build and Development Commands

```bash
# Build the project
dotnet.exe build

# Run the application
dotnet.exe run

# Clean build artifacts
dotnet.exe clean

# Restore NuGet packages
dotnet.exe restore
```

**Note**: When running in WSL (Windows Subsystem for Linux), use the `.exe` suffix for all dotnet commands to ensure proper Windows .NET execution.

## Troubleshooting

**Build Errors**: Some WinUI 3 and XAML compilation errors from `dotnet build` may be non-descriptive (e.g., "XamlCompiler.exe exited with code 1"). For detailed error messages including line numbers and specific issues, use Visual Studio which provides better diagnostics for XAML parsing errors, missing dependencies, and WinUI-specific issues.

## Architecture

### Core Components

- **App.xaml.cs**: Main application entry point with system tray integration using H.NotifyIcon
- **MainWindow**: Host window with navigation between MainView, PromptHistoryView, and SettingsView
- **Views/MainView**: Primary UI for wallpaper generation with prompt input and status display
- **Views/PromptHistoryView**: Comprehensive prompt history management with search, favorites, image management, and DeviantArt upload functionality
- **Views/SettingsView**: Tabbed configuration UI with General, API, and Random Prompt tabs for comprehensive settings management

### Services Layer

- **Services/ImageGenerator**: Handles OpenAI DALL-E 3 API integration and image generation
- **Services/DatabaseService**: SQLite database management for prompt history and image tracking
- **Services/PromptGeneratorService**: AI-powered random prompt generation using OpenAI's gpt-5 model
- **Services/TitleService**: AI-powered title and tag generation for DeviantArt uploads using OpenAI's gpt-5 model
- **Services/DeviantArt/DeviantArtService**: Core DeviantArt API integration with OAuth authentication and upload functionality
- **Services/DeviantArt/DeviantArtAuthService**: User authentication flow handling with dialog-based authorization code exchange
- **Services/DeviantArt/DeviantArtUploadService**: High-level upload orchestration with error handling and UI feedback
- **Services/Settings**: Singleton service for application configuration persistence (JSON-based)
- **Services/AutoGenerateService**: Timer-based service with support for current prompt or random generation modes
- **Services/StartupManager**: Windows registry integration for "Run on startup" functionality
- **Services/Wallpaper**: Windows system integration for setting desktop wallpapers via Win32 API

### Database Schema

- **Database Location**: `%APPDATA%\WallTrek\walltrek.db`
- **Prompts Table**: Stores prompt text, usage count, favorite status, and timestamps
- **GeneratedImages Table**: Tracks generated images with relationships to prompts, metadata, and DeviantArt upload status

### Key Technical Details

- **Framework**: .NET 9.0 with WinUI 3 (Windows App SDK 1.7.250606001)
- **Output**: Images saved to `%USERPROFILE%\Pictures\WallTrek\` with timestamp and prompt in filename
- **Settings**: Stored in `%APPDATA%\WallTrek\settings.json`
- **Database**: SQLite database at `%APPDATA%\WallTrek\walltrek.db`
- **Tray Behavior**: Application starts minimized to tray and minimizes to tray instead of closing

### Dependencies

- **OpenAI**: OpenAI API client (v2.1.0) for DALL-E 3 image generation and gpt-5 prompt generation
- **Microsoft.Data.Sqlite**: SQLite database integration for data persistence
- **System.Drawing.Common**: Image processing and metadata handling
- **H.NotifyIcon.WinUI**: System tray integration for WinUI 3

### UI Architecture

- **Multi-View Navigation**: Single MainWindow hosts multiple UserControl views
- **MVVM Pattern**: Views use data binding with INotifyPropertyChanged implementations
- **Custom Components**: FavoriteColorConverter for visual feedback in prompt history

### Application Flow

1. App starts minimized to system tray with optional Windows startup integration
2. Database initializes and loads prompt/image history
3. User can navigate between main generation, prompt history, and settings views
4. Auto-generation supports both current prompt and random AI-generated prompts
5. All prompts and generated images are tracked in the database with favorites support
6. Generated images include EXIF metadata with original prompt and are automatically set as wallpaper
7. Images can be uploaded to DeviantArt with AI-generated titles and tags via right-click context menu
8. DeviantArt OAuth flow handles user authorization with browser-based authentication

### Development Notes

- **MVVM Implementation**: Custom `Utilities/RelayCommand` with enhanced data binding patterns
- **Database Management**: Automatic schema creation and migration handling
- **UI State Management**: Expandable prompt cards with image collections in history view
- **Search Functionality**: Debounced text search across prompt history
- **Error Handling**: Comprehensive error handling across all services with user feedback
- **Thread Safety**: UI operations use proper dispatcher for background service integration