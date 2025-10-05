# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

Sharpnado.MaterialFrame is a .NET MAUI library that provides enhanced Frame controls with modern material design themes including Light, Dark, Acrylic, and AcrylicBlur effects. The library supports multiple platforms (Android, iOS, Windows, macOS) with platform-specific renderers.

## Build and Development Commands

### Building the Library
```bash
# Build the MAUI MaterialFrame library
dotnet build Maui.MaterialFrame/Maui.MaterialFrame.sln

# Build in Release mode for packaging
dotnet build Maui.MaterialFrame/Maui.MaterialFrame.sln -c Release

# Create NuGet package (automatically created on Release build)
dotnet pack Maui.MaterialFrame/Maui.MaterialFrame.csproj -c Release
```

### Building Sample Application
```bash
# Build the sample app
dotnet build Samples/Sharpnado.Acrylic.Maui/Sharpnado.Acrylic.Maui.sln

# Run sample app on specific platform
dotnet build Samples/Sharpnado.Acrylic.Maui/Sharpnado.Acrylic.Maui.csproj -f net9.0-android
dotnet build Samples/Sharpnado.Acrylic.Maui/Sharpnado.Acrylic.Maui.csproj -f net9.0-ios
```

### Platform-Specific Building
```bash
# For Android
dotnet build -f net9.0-android

# For iOS  
dotnet build -f net9.0-ios

# For Windows
dotnet build -f net9.0-windows10.0.19041.0

# For macOS Catalyst
dotnet build -f net9.0-maccatalyst
```

## Architecture Overview

### Core Components

**MaterialFrame** - The main control class (`MaterialFrame.cs`) extends MAUI Frame with material design themes:
- Supports 4 themes: Light, Dark, Acrylic, AcrylicBlur
- Platform-agnostic bindable properties for theming
- Global theme switching capability

**Platform Renderers** - Platform-specific implementations handle native rendering:
- **Android**: `AndroidMaterialFrameRenderer` - Uses LayerDrawable for effects, RealtimeBlurView for blur
- **iOS**: `iOSMaterialFrameRenderer` - Uses CALayer for effects, UIVisualEffectView for blur  
- **Windows**: `WinUIMaterialFrameRenderer` - Uses AcrylicBrush for native acrylic effects
- **macOS**: `MacCatalystMaterialFrameRenderer` - macOS-specific adaptations

**Blur Implementation**:
- Android: Custom port of RealtimeBlurView with render-to-texture blur
- iOS: Native UIVisualEffectView with Light/ExtraLight/Dark styles
- Windows: Native AcrylicBrush system

### Key Architecture Patterns

1. **Multi-Platform Rendering**: Uses MAUI handlers pattern with platform-specific renderers
2. **Theme System**: Global and per-control theming with dynamic theme switching
3. **Blur Abstraction**: Platform-specific blur implementations behind common interface
4. **Property Binding**: Extensive use of BindableProperty for XAML data binding

### Project Structure
```
Maui.MaterialFrame/           # Main library project
├── MaterialFrame.cs          # Core control class
├── Platforms/               # Platform-specific renderers
│   ├── Android/            # Android renderer + blur implementation
│   ├── iOS/                # iOS renderer
│   ├── Windows/            # WinUI renderer  
│   └── MacCatalyst/        # macOS renderer
└── MauiAppBuilderExtensions.cs # Library initialization

Samples/                     # Sample application
└── Sharpnado.Acrylic.Maui/ # MAUI sample app demonstrating features
```

## Integration and Usage

### Library Initialization
In consumer projects, initialize in `MauiProgram.cs`:
```csharp
builder.UseSharpnadoMaterialFrame(loggerEnable: false, debugLogEnable: false)
```

### XAML Namespace
Use the custom XAML namespace:
```xml
xmlns:sho="http://sharpnado.com"
```

### Platform-Specific Considerations

**Android**: 
- Acrylic glow effect requires API 23+ (LayerDrawable limitation)
- Blur is computationally expensive, use AndroidBlurRootElement to optimize
- Configure static renderer properties for performance tuning

**iOS**:
- Dynamic theme switching to AcrylicBlur doesn't work properly
- Use native UIVisualEffectView blur styles

**Windows**:
- Native AcrylicBrush support provides optimal performance
- HostBackdropBlur available on UWP only (not WinUI 3)

## Development Guidelines

### Adding New Features
1. Add properties to `MaterialFrame.cs` with BindableProperty backing
2. Update each platform renderer's `OnElementPropertyChanged` method
3. Implement platform-specific rendering logic in renderer classes
4. Update sample app to demonstrate new functionality

### Platform Renderer Pattern
Each renderer follows this pattern:
- Inherit from appropriate base renderer (FrameRenderer, etc.)
- Override `OnElementPropertyChanged` to handle property updates
- Implement platform-specific drawing/styling in renderer methods
- Handle resource cleanup in `Dispose` method

### Theme Implementation
- Light: Standard material elevation shadows
- Dark: Google dark theme elevation colors (no shadows)  
- Acrylic: White glow layer + shadows
- AcrylicBlur: Platform blur effects (no shadows)

### Performance Considerations
- Android blur is expensive - limit concurrent blur frames
- Use blur root element optimization on Android
- Dispose renderers properly to prevent memory leaks
- Cache drawable/layer objects where possible

## Platform Compatibility

| Platform | Support | Notes |
|----------|---------|-------|
| Android | ✅ | API 21+, Acrylic glow requires API 23+ |
| iOS | ✅ | iOS 14.2+ |
| Windows | ✅ | WinUI, Windows 10.0.17763.0+ |
| macOS | ❓ | Catalyst support, not fully tested |

## Troubleshooting

### Common Issues
- **Android emulator blur stalls**: Reduce concurrent blur frames
- **iOS dynamic blur switching**: Known limitation, avoid dynamic AcrylicBlur changes
- **Memory leaks**: Ensure proper renderer disposal and unsubscribe from events