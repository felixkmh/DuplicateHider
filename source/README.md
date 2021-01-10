DuplicateHider
==============
[Playnite Forum Post](https://playnite.link/forum/thread-308.html)  
An extension for [Playnite](https://github.com/JosefNemec/Playnite/ "Playnite - video game library manager") by JosefNemec that hides additional copies of games.

Extension Settings
---------------
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

Extension Menu
--------------
![Game Menu](https://i.ibb.co/G9r0BZ4/grafik.png "Game Menu")

Under _Extensions_ -> _DuplicateHider_ functions to manually hide and reveal duplicates can be found. Also, currently selected games can either be added or removed from the _Ignore List_.
