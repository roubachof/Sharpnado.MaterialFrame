# Release Notes v3.0.0

## 🔥 Breaking Changes

### Android: RenderScript → StackBlur
- **Fixed:** Android 15+ crashes on devices with 16KB page size (Pixel 8, Pixel 9, etc.)
- **Changed:** Replaced deprecated RenderScript with pure C# StackBlur algorithm
- **No API changes:** Your existing code works as-is

## ✨ New Features

- **MacCatalyst Support:** Full blur support on macOS
- **Modern Handlers:** Migrated all platforms from Renderers to MAUI Handlers
- **Async Blur:** Background processing with double buffering
- **Change Detection:** Skips blur when content unchanged (0% CPU when static)

## 🚀 Performance

| Metric | Before | After |
|--------|--------|-------|
| Android 15+ | 💥 Crashes | ✅ Works |
| UI thread | ~22ms | ~3ms |
| Static CPU | 100% | 0% |
| Frame rate | 30-45 FPS | 60 FPS |

## 📦 Installation

```bash
dotnet add package Sharpnado.MaterialFrame.Maui --version 3.0.0
```

## 🐛 Bug Fixes

- Fixed Android 15+ compatibility issues
- Eliminated frame drops during scrolling
- Improved memory management and resource cleanup

## 📝 Full Changelog

**Added:**
- MacCatalyst platform support
- StackBlur algorithm for Android
- Async blur processing with double buffering
- Change detection optimization
- Modern MAUI Handlers for all platforms

**Changed:**
- Android blur: RenderScript → StackBlur
- All platforms: Renderers → Handlers
- UI thread blocking: ~22ms → ~3ms

**Fixed:**
- Android 15+ crashes (16KB page size)
- Pixel 8/9 device compatibility
- Frame drops during blur scrolling
- Memory leaks in disposal

**Removed:**
- RenderScript dependency
- Legacy Renderer implementations
- UWP-specific properties
