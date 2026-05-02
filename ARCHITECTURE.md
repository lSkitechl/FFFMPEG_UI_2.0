# Architecture

This solution starts with a simple Clean Architecture style split:

```text
FFMPEG.Core
    Domain models and rules. No references to other projects.

FFMPEG.Application
    Application scenarios, service contracts, and use-case logic.
    References Core.

FFMPEG.Infrastructure
    External implementations: processes, file system, ffmpeg executable, storage.
    References Application and Core.

FFFMPEG_UI
    WPF presentation layer: Views, ViewModels, commands, UI helpers.
    References Application and Infrastructure only from the composition root.
```

## Dependency Direction

```text
UI -> Application -> Core
UI -> Infrastructure -> Application -> Core
```

The important rule is that the domain and application layers do not know about WPF.
`MainWindowViewModel` belongs to the UI project because it exists to serve the WPF view.

## Startup Flow

`App.xaml.cs` is the composition root:

1. Create application services.
2. Create `MainWindowViewModel`.
3. Create `MainWindow`.
4. Set the ViewModel as the window `DataContext`.

This keeps `MainWindow` simple and makes dependencies explicit.

## Current Example Flow

`MainWindow.xaml`
binds controls to
`MainWindowViewModel`.

`MainWindowViewModel`
keeps UI state and calls
`IFfmpegCommandPreviewService`.

`FfmpegCommandPreviewService`
builds a preview command from
`FfmpegCommandDraft`.

`FfmpegProcessRunner`
is an Infrastructure implementation for the future moment when the app actually starts `ffmpeg`.
