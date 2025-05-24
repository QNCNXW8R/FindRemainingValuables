# FindRemainingValuables

Tired of running in circles wondering if there are any more items left? No more! This mod adds a configurable threshold for leftover items, after which they will become visible on the map.

## Features
- When the remaining items outside the extraction point are under half the value of the goal, reveal every item on the map.
- Configurable threshold

## Configuration

### General

- "RevealThreshold": Proportion of goal remaining before revealing all valuables. 0 is never, 4 is always (default: 0.5)
- "GoalType": Which goal to use when calculating reveal threshold: Extraction or Level (default: Level)

### Controls

At the time of writing this, the ingame mod menu [RepoConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) does not support keybinds, so this section is hidden. You can still edit it from the config file or from another config editor such as r2modman.

- "RevealKeybind": Key to press to reveal all remaining valuables (default: F10)

### Debug

- "EnableLogging": If true, prints debug data to console (default: false)
