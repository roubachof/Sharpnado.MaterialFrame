# Release Notes v3.0.2

## 📦 Windows Platform Inclusion

**Type:** Packaging Fix

This is a re-release of v3.0.1 to include Windows platform binaries.

## 🐛 Issue

Version 3.0.1 was packaged on macOS, which resulted in the Windows target framework (`net9.0-windows10.0.19041.0`) being excluded from the NuGet package.

## ✅ Fix

Version 3.0.2 includes all platform binaries:
- ✅ `net9.0` - .NET 9 base
- ✅ `net9.0-android` - Android
- ✅ `net9.0-ios` - iOS
- ✅ `net9.0-maccatalyst` - MacCatalyst
- ✅ `net9.0-windows10.0.19041.0` - Windows (now included)

## 📝 Code Changes

**No code changes** - This release is functionally identical to v3.0.1.

All features and fixes from v3.0.1 remain unchanged:
- Android CollectionView blur support
- Configurable blur update interval
- Navigation back fixes
- Predraw loop fixes

## 📦 Installation

```bash
dotnet add package Sharpnado.MaterialFrame.Maui --version 3.0.2
```

## 🔄 Migration from 3.0.1

Simply update the package version. No code changes required.

Windows developers who were unable to use v3.0.1 can now use v3.0.2.

## 📚 Documentation

For full v3.0.1 feature documentation, see [RELEASE_NOTES_v3.0.1.md](RELEASE_NOTES_v3.0.1.md)

Repository: https://github.com/roubachof/Sharpnado.MaterialFrame
