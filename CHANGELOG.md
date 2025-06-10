# Changelog

## Initial Release - 1.0.0

- Add logic for revealing valuables based on a threshold
- Add basic configuration for the threshold

## Minor Update - 1.1.0

- Add toggle for threshold to be based on one extraction point vs the whole level (default: Level)
- Add toggle for debug logging (default: false)
- Add keybind to instantly trigger the reveal. Incompatible with the RepoConfig, but can be edited from the file or r2modman etc (default: F10)

## Minor Update - 1.2.0

- No longer keeps checking after revealing valuables
- Displays a notification in Haul UI when revealing valuables
- Plays the "found valuable" sound even if no valuables are nearby
- Cleaned up logger spam

## Patch - 1.2.1

- Fix Level goal setting to properly count finished extractions as progress
- Add toggle to enable hotkeys (default: true)

## Major Update - 2.0.0

- Rework GoalTypes options to ExtractionGoal, LevelGoal, LevelLoot, Extractions (default: LevelLoot)
- Add new TrackingMethod options Haul or Discovery (default: Haul)
- Trigger the reveal notification if somebody else has revealed everything
- Update default threshold to 0.1
- With the new default configuration, valuables will be revealed when 90% of everything on the level is collected, independent of any extraction goals for a more consistent experience.
- Fix reveal logic to show correct value of revealed valuables
- Add some example configurations to the readme

## Patch - 2.0.1

- Only the host will reveal valuables
- Other players with the mod installed will still get the notification

## Patch - 2.0.2

- Performance improvements

## Minor Update - 2.1.0

- Add setting to disable notification sound effect (default: enabled)

## Minor Update - 2.2.0

- Add setting to let enemies respond to the notification (default: false)
- Add difficulty setting for the enemy response:
  - Investigate: Enemies within about 2-3 rooms will investigate the active extraction point (default)
  - Sweep: Enemies within about 4-5 rooms will walk to the active extraction point
  - Purge: All enemies on the map will respawn and converge on the active extraction point
- Note that no new behaviour is added if the valuables are revealed before activating the first extraction, or after completing the last extraction

## Minor Update - 2.3.0

- Add difficulty setting Annihilation: All enemies on the map will respawn and converge directly upon the active extraction point. Hide or defend your loot!
- If valuables are revealed while there is no active extraction point, the enemy alert will trigger when the next extraction point on the same level is activated.
