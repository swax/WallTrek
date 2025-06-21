# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

Since this repository is accessed through WSL, always use `.exe` suffix for Windows commands:

- **Build**: `dotnet.exe build`
- **Run**: `dotnet.exe run`  
- **Clean**: `dotnet.exe clean`
- **Restore packages**: `dotnet.exe restore`

## Project Architecture

WallTrek is a .NET 9.0 Windows Forms application that generates AI wallpapers using OpenAI's DALL-E 3 API.

### Core Components

- **Program.cs**: Application entry point, launches MainForm
- **Forms/MainForm.cs**: Main UI with system tray integration, prompt input, and auto-generation timer
- **Forms/SettingsForm.cs**: Settings dialog for API key and other configuration
- **Services/ImageGenerator.cs**: Handles OpenAI DALL-E 3 API calls and image saving
- **Services/Settings.cs**: JSON-based configuration persistence 
- **Services/Wallpaper.cs**: Windows wallpaper setting functionality

### Key Features

- System tray application that runs in background
- Auto-generation timer with configurable intervals
- Images saved to `%UserProfile%\Pictures\WallTrek\` with timestamp and prompt in filename
- Settings persisted to JSON file
- High-quality 1792x1024 wallpapers using DALL-E 3

### Project Structure

- **Forms/**: WinForms UI components (.cs, .Designer.cs, .resx files)
- **Services/**: Business logic classes
- **Assets/**: Application icon resources
- **Output**: Images saved to user's Pictures folder, not project directory

### Dependencies

- **OpenAI** package (v2.1.0) for DALL-E 3 API integration
- **.NET 9.0-windows** target framework with Windows Forms

### Settings Storage

Settings are automatically saved to a JSON file and include:
- OpenAI API key
- Last used prompt
- Auto-generation preferences (enabled/interval)