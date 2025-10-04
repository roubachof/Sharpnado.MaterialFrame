# Migration from Frame to Border

## Overview
This document describes the migration of MaterialFrame from inheriting `Frame` to inheriting `Border` in .NET MAUI.

## Changes Made

### 1. MaterialFrame Core Class (`MaterialFrame.cs`)

**Changed:**
- Base class: `Frame` → `Border`
- Added custom `CornerRadius` property that wraps Border's `StrokeShape` functionality
- Removed `HasShadow` property (not available in Border; shadow is now handled by platform renderers)

**Key Implementation:**
```csharp
public partial class MaterialFrame : Border
{
    // Custom CornerRadius property to maintain API compatibility
    // Uses float type (same as original Frame) and internally updates Border's StrokeShape
    public new static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius),
        typeof(float),
        typeof(MaterialFrame),
        defaultValue: -1.0f,
        propertyChanged: OnCornerRadiusChanged);
    
    private static void OnCornerRadiusChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MaterialFrame frame && newValue is float radius)
        {
            if (radius >= 0)
            {
                frame.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(radius) };
            }
        }
    }
    
    public new float CornerRadius
    {
        get => (float)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }
}
```

### 2. iOS Handler (`iOSMaterialFrameRenderer.cs`)

**Changed:**
- Base class: `FrameRenderer` (compatibility) → `BorderHandler` (modern handler)
- Property access: `Element` → `VirtualView` (for cross-platform view) and `PlatformView` (for native view)
- Lifecycle methods:
  - `OnElementChanged` → `ConnectHandler/DisconnectHandler`
  - `OnElementPropertyChanged` → `PropertyMapper`
- View hierarchy: `Subviews[0]` → `PlatformContentView`

**Handler Pattern:**
```csharp
public class iOSMaterialFrameRenderer : BorderHandler
{
    // Property mapper for reactive updates
    public static PropertyMapper<MaterialFrame, iOSMaterialFrameRenderer> MaterialFrameMapper = new(ViewMapper)
    {
        [nameof(MaterialFrame.CornerRadius)] = (handler, view) => handler.UpdateCornerRadius(),
        [nameof(MaterialFrame.Elevation)] = (handler, view) => handler.UpdateElevation(),
        // ... other properties
    };
    
    protected override void ConnectHandler(ContentView platformView) { }
    protected override void DisconnectHandler(ContentView platformView) { }
}
```

### 3. Handler Registration (`MauiAppBuilderExtensions.cs`)

**Updated:**
- iOS handler registration now uses the generic type parameter approach
- Changed from compatibility renderer registration to modern handler registration

```csharp
handlers.AddHandler<MaterialFrame, Sharpnado.MaterialFrame.iOS.iOSMaterialFrameRenderer>();
```

## Platform Status

### ✅ iOS
- **Status:** Migrated to BorderHandler
- **Changes:** Complete rewrite using modern handler pattern
- **Testing:** Needs validation

### ✅ Android
- **Status:** Migrated to BorderHandler
- **Changes:** Complete rewrite using modern handler pattern
- **Testing:** Needs validation
- **Note:** Uses ContentViewGroup's native elevation property for shadows, OutlineProvider for rounded corners

### ⚠️ Windows
- **Status:** Still using compatibility renderer
- **Future:** Should be migrated to BorderHandler

### ⚠️ macOS Catalyst
- **Status:** Still using compatibility renderer
- **Future:** Should be migrated to BorderHandler

## Breaking Changes

### API Compatibility
✅ **Maintained** - The public API remains the same:
- `CornerRadius` property still works as before
- All MaterialFrame-specific properties unchanged
- Theme switching functionality preserved

### Internal Changes
⚠️ **Different behavior possible:**
- Border doesn't have built-in shadow support (handled by platform renderers)
- Border uses `StrokeShape` internally instead of direct corner radius
- Layout behavior might differ slightly from Frame

## Migration Checklist for Other Platforms

To migrate Android/Windows/macOS to BorderHandler:

1. **Inherit from BorderHandler** instead of FrameRenderer
2. **Update property access:**
   - `Element` → `VirtualView`
   - Native view access via `PlatformView`
3. **Replace lifecycle methods:**
   - `OnElementChanged` → `ConnectHandler/DisconnectHandler`
   - `OnElementPropertyChanged` → Use PropertyMapper
4. **Update view hierarchy access:**
   - Platform-specific view access patterns
5. **Test thoroughly:**
   - Shadows/elevation
   - Corner radius
   - Blur effects
   - Theme switching

## Testing Recommendations

1. **Visual Testing:**
   - Verify shadows render correctly in Light/Acrylic themes
   - Check corner radius rendering
   - Test blur effects (AcrylicBlur theme)
   - Validate all four themes (Light, Dark, Acrylic, AcrylicBlur)

2. **Property Testing:**
   - Dynamic theme switching
   - Corner radius changes
   - Elevation changes
   - Background color updates

3. **Performance Testing:**
   - Blur performance on iOS
   - Memory leaks (proper disconnect handling)
   - Layout performance

## Known Issues

1. **iOS:** Dynamic theme switching to AcrylicBlur has known limitations (pre-existing)
2. **Android:** Still using compatibility renderer - may have different behavior
3. **Border API:** Some Frame features may behave differently

## Next Steps

1. ✅ Test iOS implementation thoroughly
2. ✅ Migrate Android renderer to BorderHandler
3. 🔲 Migrate Windows renderer to BorderHandler  
4. 🔲 Migrate macOS renderer to BorderHandler
5. 🔲 Update documentation and samples
6. 🔲 Update WARP.md with new architecture notes
