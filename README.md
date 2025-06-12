# Summary
Selection Tracker window helps navigate back to previously selected assets in the Project window by providing a history sortable clickable history.

## Description

This little utility is useful for those coming in to preexisting large projects where there may be massive amounts of game assets to sift through.

This utilty was written specifically for the case where, navigating a new project, you might find the correct asset referenced in a prefab, then changed scenes or gone to a new prefab, and now you don't recall exactly where the prior asset was located due to the deep or messy project structure.

Pin frequently used assets for quick selection. Sortable by column type.

## Detail

- Provides a history of selected assets in the project folders
- Provides a pinned section for frequently used assets
- Ping assets in project window from Selection Tracker
- Set length of history to record
- Saves history to PersistentDataFolder/ProjectName/SelectionTracker/history.dat
- Async save keeps data up to date with minimal file writes
- Uses UIToolkit
- Works in Unity 2022.3+ (may work in earlier versions that use UIToolkit but is untested)
