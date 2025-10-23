# Release Notes v3.0.1

## 🎯 Android: CollectionView Blur Support

**Platform:** Android only

This release fixes blur functionality in Android CollectionView/RecyclerView scenarios and adds performance tuning capabilities.

**Note:** iOS, MacCatalyst, and Windows are unchanged in this release.

## ✨ New Features

### Android: CollectionView/RecyclerView Blur Support
- **Fixed:** Blur now works correctly when MaterialFrame is inside CollectionView items
- **Solution:** Alpha-based transparency during capture with time-based throttling
- **Use Case:** Product cards, image galleries, chat bubbles with blurred backgrounds

### Configurable Update Interval
```csharp
#if ANDROID
// Adjust blur update frequency
AndroidMaterialFrameHandler.BlurUpdateIntervalMs = 16;  // 60fps - smoother
AndroidMaterialFrameHandler.BlurUpdateIntervalMs = 32;  // 30fps - default
AndroidMaterialFrameHandler.BlurUpdateIntervalMs = 50;  // 20fps - battery saver
#endif
```

## 🐛 Bug Fixes

- **Fixed:** Blur not working in RecyclerView ViewHolders (sibling view capture issue)
- **Fixed:** Navigation back losing blur root view reference
- **Fixed:** Infinite predraw loops when changing view properties during capture
- **Fixed:** AndroidBlurRootElement causing issues in same-container scenarios

## 🚀 Performance Improvements

| Setting | FPS | Use Case |
|---------|-----|----------|
| 16ms | ~60fps | Smooth animations, gaming |
| 32ms (default) | ~30fps | General use, balanced |
| 50ms | ~20fps | Battery saving |

## 📦 Installation

```bash
dotnet add package Sharpnado.MaterialFrame.Maui --version 3.0.1
```

## 💡 Usage Tips

### CollectionView Best Practices

**❌ Don't use AndroidBlurRootElement in CollectionView items:**
```xml
<CollectionView>
    <CollectionView.ItemTemplate>
        <Grid x:Name="ItemGrid">
            <Image Source="background.png" />
            <!-- DON'T: AndroidBlurRootElement="{x:Reference ItemGrid}" -->
            <sho:MaterialFrame MaterialTheme="AcrylicBlur" />
        </Grid>
    </CollectionView.ItemTemplate>
</CollectionView>
```

**✅ Do let blur use default root (activity decor view):**
```xml
<CollectionView>
    <CollectionView.ItemTemplate>
        <Grid>
            <Image Source="background.png" />
            <!-- Just omit AndroidBlurRootElement -->
            <sho:MaterialFrame MaterialTheme="AcrylicBlur" />
        </Grid>
    </CollectionView.ItemTemplate>
</CollectionView>
```

## 🔧 Technical Details

### Root View Lifecycle Fix
- `OnDetachedFromWindow` no longer releases root view reference
- Preserves root for navigation back scenarios
- Only releases bitmaps and cancels blur operations on detach

### Alpha Transparency Capture
- Sets parent alpha to 0 during blur capture
- Allows sibling views to be captured without MaterialFrame
- More efficient than visibility changes (no layout recalculation)

### Time-Based Throttling
- Prevents rapid predraw callbacks from alpha/visibility changes
- Default 32ms interval prevents infinite loops
- User-configurable for performance tuning

## 📝 Migration from 3.0.0

No breaking changes - this is a bug fix and feature release.

If you're using `AndroidBlurRootElement` in CollectionView items, remove it for best results.

## 🙏 Credits

Special thanks to the community for reporting the CollectionView blur issue!
