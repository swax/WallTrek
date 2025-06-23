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

## Architecture

### Core Components

- **App.xaml.cs**: Main application entry point with system tray integration using H.NotifyIcon
- **MainWindow**: Primary UI for wallpaper generation with prompt input and status display
- **SettingsWindow**: Configuration UI for API keys and auto-generation settings

### Services Layer

- **ImageGenerator**: Handles OpenAI DALL-E 3 API integration and image generation
- **Settings**: Singleton service for application configuration persistence (JSON-based)
- **AutoGenerateService**: Timer-based service for scheduled wallpaper generation
- **Wallpaper**: Windows system integration for setting desktop wallpapers via Win32 API

### Key Technical Details

- **Framework**: .NET 9.0 with WinUI 3 (Windows App SDK 1.7.250606001)
- **Namespace**: Primary namespace is `WallTrek`, but project uses `Tabavoco` as RootNamespace
- **Output**: Images saved to `%USERPROFILE%\Pictures\WallTrek\` with timestamp and prompt in filename
- **Settings**: Stored in `%APPDATA%\WallTrek\settings.json`
- **Tray Behavior**: Application starts minimized to tray and minimizes to tray instead of closing

### Dependencies

- **OpenAI**: OpenAI API client for DALL-E 3 image generation
- **System.Drawing.Common**: Image processing and metadata handling
- **H.NotifyIcon.WinUI**: System tray integration for WinUI 3

### Application Flow

1. App starts minimized to system tray
2. User can open main window from tray or generate wallpapers via auto-timer
3. Wallpaper generation uses saved prompts and API keys from Settings
4. Generated images include EXIF metadata with the original prompt
5. Wallpapers are automatically set as desktop background using Win32 API

### Development Notes

- The application uses a custom `RelayCommand` implementation for MVVM patterns
- Settings are managed through a singleton pattern with automatic JSON serialization
- Auto-generation service uses `DispatcherQueueTimer` for UI thread-safe operations
- Window closing is intercepted to minimize to tray instead of actual application exit
- Update CLAUDE.md if the architecture changes