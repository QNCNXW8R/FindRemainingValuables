# FindRemainingValuables

Tired of running in circles wondering if there are any more items left? No more! This mod adds a configurable threshold for leftover items, after which they will become visible on the map.

## Features
- When the remaining items are under a threshold, reveal every item on the map.
- Only the host needs to install it to reveal everything. Other players with the mod will get a notification when items get revealed.
- Fully configurable!

## Configuration

### General

- "RevealThreshold": Proportion of goal remaining before revealing all valuables: 0 is never, 1 is always in LevelLoot mode, 4 is probably always in other modes (default: 0.1)
- "TrackingMethod: Method to track progress towards the threshold: Haul or Discovery (default: Haul)
- "GoalType": Which goal to use when calculating reveal threshold: ExtractionGoal, LevelGoal, LevelLoot, Extractions (default: LevelLoot)

#### Examples

Here are some example configurations and the resulting behaviour:

| RevealThreshold | TrackingMethod | GoalType | Behaviour |
| - | - | - | - |
| 0.5 | Haul | ExtractionGoal | Reveal everything when the ungathered loot is less than half the goal of a single extraction point. Fairly easy on the first level, but gets much stricter as you progress. |
| 0.1 | Discovery | LevelGoal | Reveal everything when the undiscovered loot is less than a tenth of the goal of the entire level. This requires finding all but one or two items on the first level, but gets a bit easier on later levels. |
| 0.2 | Discovery | LevelLoot | Reveal everything once you've seen 80% of the loot on the level. Consistent between big and small levels.
| 0 ~ 0.99 | N/A | Extractions | Reveal everything when all the extractions are finished. Probably not very helpful. |
| 2 ~ 2.99 | N/A | Extractions | Reveal everything when there are only two extraction points left. This means immediately in the first few levels. |
| 4 | N/A | N/A | Reveal everything immediately |

### Notification

- "NotificationSound": If true, enables the notification sound when the reveal is triggered (default: true)

### Controls

At the time of writing this, the ingame mod menu [RepoConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) does not support keybinds, so only "EnableKeybinds" is visible. You can still edit the keybinds from the config file or from another config editor such as r2modman.

- "EnableKeybinds": If true, enables hotkeys (default: true)
- "RevealKeybind": Key to press to reveal all remaining valuables (default: F10)

### Debug

- "EnableLogging": If true, prints debug data to console (default: false)

## Developer Contact

Report bugs, suggest features, or provide feedback:

| Discord Server | Channel | Post |
|-|-|-|
| [R.E.P.O. Modding Server](https://discord.com/invite/vPJtKhYAFe) | #released-mods | [FindRemainingValuables](https://discord.com/channels/1344557689979670578/1375885607720718416/1375885607720718416) |