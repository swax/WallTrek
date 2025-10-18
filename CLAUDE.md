# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

WallTrek is a WinUI 3 application that generates AI-powered wallpapers using multiple AI providers (OpenAI DALL-E 3, Google Imagen). The application runs with a system tray icon and can automatically generate wallpapers at specified intervals with customizable random prompt elements.

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
- **Views/SettingsView**: Tabbed configuration UI with General, API (supporting multiple providers), and Random Prompt tabs for comprehensive settings management

### Services Layer

#### Core Services
- **Services/FileService**: Image file persistence with EXIF metadata handling
- **Services/DatabaseService**: SQLite database management for prompt history and image tracking with LLM/image model metadata
- **Services/Settings**: Singleton service for application configuration persistence (JSON-based)
- **Services/AutoGenerateService**: Timer-based service with support for current prompt or random generation modes
- **Utilities/StartupManager**: Windows registry integration for "Run on startup" functionality
- **Services/Wallpaper**: Windows system integration for setting desktop wallpapers via Win32 API

#### Image Generation Services
- **Services/ImageGen/IImageGenerationService**: Interface for image generation providers
- **Services/ImageGen/ImageGenerationServiceFactory**: Factory for creating appropriate image generation service instances
- **Services/ImageGen/OpenAiImageGenerator**: OpenAI DALL-E 3 image generation implementation (returns MemoryStream)
- **Services/ImageGen/GoogleImagenService**: Google Imagen API integration for image generation (returns MemoryStream)

#### Text Generation Services
- **Services/TextGen/ILlmService**: Interface for LLM providers
- **Services/TextGen/LlmServiceFactory**: Factory for creating appropriate LLM service instances
- **Services/TextGen/OpenAILlmService**: OpenAI API integration for text generation
- **Services/TextGen/AnthropicLlmService**: Anthropic API integration for text generation and image descriptions
- **Services/TextGen/PromptGeneratorService**: AI-powered random prompt generation with customizable elements at key level
- **Services/TextGen/TitleService**: AI-powered title and tag generation for DeviantArt uploads

#### DeviantArt Integration
- **Services/DeviantArt/DeviantArtService**: Core DeviantArt API integration with OAuth authentication and upload functionality
- **Services/DeviantArt/DeviantArtAuthService**: User authentication flow handling with dialog-based authorization code exchange
- **Services/DeviantArt/DeviantArtUploadService**: High-level upload orchestration with error handling and UI feedback

### Database Schema

- **Database Location**: `%APPDATA%\WallTrek\walltrek.db`
- **Prompts Table**: Stores prompt text, usage count, favorite status, and timestamps
- **GeneratedImages Table**: Tracks generated images with relationships to prompts, metadata, LLM/image model information, and DeviantArt upload status

### Key Technical Details

- **Framework**: .NET 9.0 with WinUI 3 (Windows App SDK 1.7.250606001)
- **Output**: Images saved to `%USERPROFILE%\Pictures\WallTrek\` with timestamp and prompt in filename
- **Settings**: Stored in `%APPDATA%\WallTrek\settings.json`
- **Database**: SQLite database at `%APPDATA%\WallTrek\walltrek.db`
- **Tray Behavior**: Application starts minimized to tray and minimizes to tray instead of closing

### Dependencies

- **OpenAI**: OpenAI API client (v2.1.0) for DALL-E 3 image generation and GPT model text generation
- **Anthropic.SDK**: Anthropic API client for Claude model text generation and image descriptions
- **Google AI**: Google Generative AI client for Imagen image generation
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
4. Auto-generation supports both current prompt and random AI-generated prompts with customizable elements
5. Users can select from multiple AI providers (OpenAI, Google, Anthropic) for different services
6. All prompts and generated images are tracked in the database with favorites support and model metadata
7. Generated images include EXIF metadata with original prompt and are automatically set as wallpaper
8. Images can be uploaded to DeviantArt with AI-generated titles and tags via right-click context menu or "I'm feeling lucky" command
9. DeviantArt OAuth flow handles user authorization with browser-based authentication

### Development Notes

- **MVVM Implementation**: Custom `Utilities/RelayCommand` with enhanced data binding patterns
- **Database Management**: Automatic schema creation and migration handling
- **UI State Management**: Expandable prompt cards with image collections in history view
- **Search Functionality**: Debounced text search across prompt history
- **Error Handling**: Comprehensive error handling across all services with user feedback
- **Thread Safety**: UI operations use proper dispatcher for background service integration
- **Service Architecture**: Clear separation of concerns with image generation services returning MemoryStreams, FileService handling file persistence with EXIF metadata, and DatabaseService managing data persistence - orchestrated in MainView as: generate → save → register