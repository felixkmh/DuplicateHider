## WARNING: The new features in this branch as well as the Playnite 9 API that they use are still under active development. So breaking changes are to be expected.

# DuplicateHider

[Playnite Forum Post](https://playnite.link/forum/thread-308.html)  
An extension for [Playnite](https://github.com/JosefNemec/Playnite/ "Playnite - video game library manager") by JosefNemec that hides additional copies of games.

## Extension Settings

![Extension Settings](https://i.ibb.co/prHsD6L/grafik.png "Plugin Settings")

### Priorites

Reordable list of Sources/Libraries that assigns each Source a priority according to their position in the priorites list. Higher position meaning a higher priority (lower value).
Duplicate hider will use this priority to compute a score for each copy of a game,

```csharp
    Score(game) = Priority(game.Source) - priorityList.Count * IsInstalled(game)?1:0
```

and copies are sorted in ascending order by their score. All but the first game are then hidden.

### Update Rank Automatically

If enabled, keeps the scores updated (and hides games accordingly) when changes to the library are made and hides games automatically when Playnite launches or settings are changed.

### Game Filters

The list of games that is checked for duplicats can be filtered by the _Indclude Platforms_, _Exclude Sources_ and _Exclude Categories_ filters.

### Ignored Games

Additionally to the aforementioned filters, games in the _Ignored Games_ list are also not considered by DuplicateHider.
To remove entries from the list, select one or more entries and right click to remove them.  
Also, enabling the _Add manually hidden/revealed Games_ option will cause games which hidden states are changed outside of DuplicateHider to be added to that list.

### Display String & Show Other Copies

![Other Copies](https://i.ibb.co/r7FGKfw/grafik.png "Other Copies")

If the _Show Copies in Game Menu_ option is enabled, right clicking a game will show other copies of the clicked game in the context menu.
The _Display string for other copies_ is used to generate the string that is used for a game's entry in the context menu.
Variables in the format `{Prefix'Variable'Suffix}` are only evaluated to a non-empty string, if `Variable` can be evaluated.
If `Variable` can be evaluated, the prefix and suffix strings are also shown as is.
If no prefix or suffix is needed, `{Prefix'Variable}`, `{Variable}` and `{Variable'Suffix}` can also be used.
To bring up a list of available variables, right click inside the text box and click on a variable to insert it.

The example above used `{Name} [{Installed}{ on 'Source}{, ROM: 'ImageNameNoExt}]` as the display string.

### UI Intgration

![Icon Stack](https://i.ibb.co/NjxdSZC/grafik.png "Icon Stack")

This extension provides a custom UI element that can be intgrated by theme creators.
To use it, the Theme you are using needs to support it and the _UI Integration_ option must be enabled. Available starting with Planite 9.

The custom UI element consists of a stack of icons associated with the copys of a game. Clicking on an icon will select this version of the game, double clicking launches it. Slightly grayed out icons indicate that a copy is not installed. An expample is shown above.

- _UI Intgration_: Enable UI integration for Themes that support it.
- _Enable Theme Icons_: Themes can provide their own icons for the different game libraries, like Steam, GOG and so on. If this option is enabled, those will be used if available.
- _Prefer User Icons_: Users can also supply their own icons by placing them into a special folder. This folder can be opened by pressing the _Open user icon folder_ button. If this option is enabled, existing user icons will always be preferred, only falling back to the ones included with the Theme or the default ones, if no user icon can be found for a given source.
- _Show icon if there is only one copy_: By default, icons are only displayed when the associated game has at least one additional copy. Enabling this option will always show an icon.

#### User Specified Icons

The extension comes with a set of predefined icons for common libraries, but users can also specify their own. For DuplicateHider to find those icons, they need to be named like the sources in the _Priority List_, for example `Steam.ico` or `Ubisoft Connect.png` and then placed into the _source\_icons_ folder that can be found by pressing the _Open user icon folder_ button in the plugin settings.

## Extension Menu

![Game Menu](https://i.ibb.co/G9r0BZ4/grafik.png "Game Menu")

Under _Extensions_ -> _DuplicateHider_ functions to manually hide and reveal duplicates can be found. Also, currently selected games can either be added or removed from the _Ignore List_.

## Theme Integration

The custom UI element can be intgrated into a Theme by, for example, placing

```xml
<ContentControl x:Name="DuplicateHider_SourceSelector" DockPanel.Dock="Right" MaxHeight="{Binding ElementName=PART_ImageIcon, Path=Height}"/>
```

into the `DetailsViewItemTemplate.xaml` file where appropriate. See Playnite documentation for more information. At runtime, Playnite will set its content to a Control holding a StackPanel of icons (ContentControls), depending on the GameContext. This might look like this

![Icon Stack](https://i.ibb.co/NjxdSZC/grafik.png "Icon Stack")

when placed in a ListViewItem in the DetailsView. See Playnite Documentation to see where else custom UI elements can be used. Go [here](UiIntegrationExamples/) for  better examples.

### SourceSelector

There are up to 10 SourceSelectors, ```DuplicateHider_SourceSelector```, ```DuplicateHider_SourceSelector1```, ```DuplicateHider_SourceSelector2```, and so on that you can use by using their names as the name of a  ```ContentControl``` in a supported template or view. 

For each SourceSelector, you can provide styles for their ```StackPanel``` and the icons which are just ```ContentControls```s (with the Icon as its content if no style is provided). The styles need to have the keys ```DuplicateHider_IconContentControlStyle``` and ```DuplicateHider_IconStackPanelStyle``` (or with the added number for the other ones). 

SourceSelector utilizes a cache for the UI elements and Icons and is suitable for use in the Item Templates for the GridView and DetailsView. 

> __Note__: The icon ui elements are also recycled when a SourceSelector's IsVisible property is false, so when animating the opacity, the Visibility property should be Hidden or Collapsed when the opacity is 0.

Example Styles can be found [here](UiIntegrationExamples/DuplicateHider_SourceSelector_Styles_Example.xaml). Each Icon ContentControl has a corresponding ListData object set as its DataContext. An overview of available Properties can be found in the next section.

> __Note__: ```DuplicateHider_SourceSelectorN``` is only registered if either ```DuplicateHider_IconContentControlStyleN``` or ```DuplicateHider_IconStackPanelStyleN``` is found. ```DuplicateHider_SourceSelector``` is always registered.

### ContentControl

There are also 10 instances of ```DuplicateHider_ContentControl``` (in case you want to use several in the same View), numbered like the SourceSelector. These basically only provide a DataContext and need a Style/Template to do anything. To apply a Style, set it as the __Tag__ of a ContentControl with x:Name="DuplicateHider_ContentControl" (or one of the 9 others):

```xml
<Style x:Key="DuplicateHider_ContentControlHeader_Style" TargetType="ContentControl"> ... </Style>
...
<ContentControl x:Name="DuplicateHider_ContentControl" Tag="{DynamicResource DuplicateHider_ContentControlHeader_Style}"/> 
```

The styled ContentControl has access to the following DataContext: 

 ```csharp
DataContext = {
    ListData CurrentGame;                  // Game currently in view
    ObversableCollection<ListData> Games;  // Data of copys of current game, including itself
    Boolean MoreThanOneCopy;               // Games.Count() > 1
    Boolean SwitchedGroup;                 // True iff the newly selected Game 
                                           // is not a copy of the previously selected one
}

class ListData {
    Playnite.SDK.Models.Game Game;
    Boolean IsCurrent;        // True if this copy is the current GameContext property.
    BitmapImage Icon;         // Source Icon
    String SourceName;        // Source name. Use this rather than Game.Source.Name, 
                              // because Source might be null.
    ICommand LaunchCommand;
    ICommand SelectCommand;
    ICommand InstallCommand;
    ICommand UninstallCommand;
}
```

The Icons are still cached for this component, but using this in the DetailsViewItemTemplate or GridViewItemTemplate may cause a lot of objects to be created whenever the views are switched or view filters are changed, depending on the used Template.

Example Styles can be found [here](UiIntegrationExamples/DuplicateHider_ContentControl_Style_Examples.xaml). A selfcontained example of multiple DuplicateHider_ContentControls in the DetailsViewGameOverview.xaml can be found [here](UiIntegrationExamples/UiIntegrationDetailsViewExample). See [Example Overview](#example-overview).

### Theme Icons

Themes can also supply their own source icons, by adding entries to the resource dictionary and adding the icon files into the appropriate folder. The entries need to have the key  `DuplicateHider_SOURCENAME_Icon`, where _SOURCENAME_ needs to be replaced by the name of the source as seen in the _Priority List_. For example, if you want to add an icon for Uplay aka Ubisoft Connect, you might add

```xml
<BitmapImage x:Key="DuplicateHider_Ubisoft Connect_Icon" UriSource="{ThemeFile 'Images/Icons/ubisoft.png'}" RenderOptions.BitmapScalingMode="Fant" popt:Freeze="True"/>
```

to the `Media.xaml` file and place `ubisoft.png` into the `Image/Icons` folder. A default icon can be specified by giving it the key ```DuplicateHider_Default_Icon```
> __Note__: Theme Icons are disabled by default and need to be enabled in the settings in order for them to be used.

### Icon Number Limit

By adding

```xml
<sys:Int32 x:Key="DuplicateHider_MaxNumberOfIcons">4</sys:Int32>
```

to the resource dictionary, a Theme can also specify the maximum number of icons per element. In the example above, it is set to 4, which is also the default if no entry is supplied. Each ```DuplicateHider_MaxNumberOfIconsN``` will apply to both ```DuplicateHider_SourceSelectorN``` and ```DuplicateHider_ContentControlN```. A value less than 1 means no limit on the number of icons.

### Example Overview

| File | Description | Preview |
|------|-------------|---------|
| [DetailsView<br/>ItemTemplate.xaml](UiIntegrationExamples/UiIntegrationSimpleExample/DetailsViewItemTemplate.xaml) | Places clickable Icons next to the game name in the DetailsView. | ![grafik](https://user-images.githubusercontent.com/24227002/113683300-55130b80-96c4-11eb-9f27-366ef4cb4bad.png) |
| [DetailsView<br/>ItemTemplate.xaml](UiIntegrationExamples/UiIntegrationFadingExample/DetailsViewItemTemplate.xaml) | Similar to above example, but here the icons fade in and out when the mouse is over a ListItem. |  |
| [DetailsView<br/>ItemTemplate.xaml](UiIntegrationExamples/UiIntegrationFadingExample2/DetailsViewItemTemplate.xaml) | Similar to above example, but here the width of the StackPanel is also animated such that long game names are pushed away when it appears. |  |
| [DetailsView<br/>GameOverview.xaml](UiIntegrationExamples/UiIntegrationDetailsViewExample/DetailsViewGameOverview.xaml) | Contains Styles for the DuplicateHider_ContentControl and uses them to display available sources as header and adds an conditional extension to the PlayButton. | ![grafik](https://user-images.githubusercontent.com/24227002/113638466-363a5800-9677-11eb-869d-e73507df7928.png) |
| [DuplicateHider_<br/>ContentControl_<br/>Style_Examples.xaml](UiIntegrationExamples/DuplicateHider_ContentControl_Style_Examples.xaml) | Two Styles for DuplicateHider_ContentControl. ```DH_ContentControl_Style``` contains some animations to indicate the current game.| DH_ContentControl_Simple_Style: ![grafik](https://user-images.githubusercontent.com/24227002/113685683-c94eae80-96c6-11eb-9241-3675010d25e6.png) DH_ContentControl_Style: ![grafik](https://user-images.githubusercontent.com/24227002/113685711-d075bc80-96c6-11eb-92d1-c9c2abba3900.png) |
| [DuplicateHider_<br/>SourceSelector_<br/>Styles_Example.xaml](UiIntegrationExamples/DuplicateHider_SourceSelector_Styles_Example.xaml) | Contains fairly barebones Styles for the DuplicateHider_SourceSelector. Basically the default Styles if none are provided for a SourceSelector. ```DuplicateHider_IconContentControlStyle``` for the Icons and ```DuplicateHider_IconStackPanelStyle``` for the StackPanel |  |
| [ListGameItem<br/>Template.xaml](UiIntegrationExamples/UiIntegrationFullscreenExample/ListGameItemTemplate.xaml) | Simple example for the ```SourceSelector``` on a game card in a fullscreen theme. | ![grafik](https://user-images.githubusercontent.com/24227002/113946975-dc1dcc00-9809-11eb-8276-b9bc1290344e.png) |
| [GameDetails.xaml](UiIntegrationExamples/UiIntegrationFullscreenExample/GameDetails.xaml) | Example for a DetailsView in a fullscreen theme using ```DuplicateHider_ContentControl```. | ![grafik](https://user-images.githubusercontent.com/24227002/113946801-8ba66e80-9809-11eb-8a2e-b9b537d5a2be.png) |
