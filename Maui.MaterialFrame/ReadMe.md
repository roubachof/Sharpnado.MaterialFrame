# Sharpnado's CollectionView
* Performance oriented
* Horizontal, Grid, Carousel or Vertical layout
* Header, Footer and GroupHeader
* Reveal custom animations
* Drag and Drop
* Column count
* Infinite loading with Paginator component
* Snapping on first or middle element
* Padding and item spacing
* Handles NotifyCollectionChangedAction Add, Remove and Reset actions
* View and data template recycling
* RecyclerView on Android
* UICollectionView on iOS

## Installation

* In Core project, in `MauiProgram.cs`:

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp()
        .UseSharpnadoCollectionView(loggerEnabled: false);
}
```

## Usage

```xml
<!--  As a Grid  -->
<sho:GridView
    x:Name="HorizontalListView"
    CollectionPadding="30"
    ColumnCount="3"
    EnableDragAndDrop="True"
    HeightRequest="390"
    HorizontalOptions="Fill"
    ItemHeight="110"
    ItemsSource="{Binding Logo, Mode=OneTime}" />

<!--  As a List with groups  -->
<sho:CollectionView
    CollectionLayout="Vertical"
    CollectionPadding="0,30,0,30"
    CurrentIndex="{Binding CurrentIndex}"
    ItemHeight="120"
    ItemTemplate="{StaticResource HeaderFooterGroupingTemplateSelector}"
    ItemsSource="{Binding SillyPeople}"
    ScrollBeganCommand="{Binding OnScrollBeginCommand}"
    ScrollEndedCommand="{Binding OnScrollEndCommand}"
    TapCommand="{Binding TapCommand}" />

<!--  As a carousel -->
<sho:CarouselView />

<!--  As a HorizontalListView -->
<sho:HorizontalListView />
```
