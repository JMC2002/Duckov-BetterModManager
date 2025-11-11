# Better MOD Manager

## 0. Installation
For the Steam version, simply subscribe on the Workshop 👉 [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3600174953)

For other versions, you can compile it yourself or download the BetterModManager.zip from the [📦 Releases](https://github.com/JMC2002/Duckov-BetterModManager/releases) page, and extract it into the "Mods" folder in the game installation directory (create the folder if it doesn’t exist). The file structure will look like this:

```sh
-- Escape from Duckov
    |-- Duckov.exe
    |-- Duckov_Data
         |-- Mods
              |-- BetterModManager
                   |-- 0Harmony.dll
                   |-- BetterModManager.dll
                   |-- info.ini
                   |-- preview.png
```

> ⚠️ The release page will update with Workshop changes. The latest version is based on the source code. The DLL in the project folder is the latest, but it may not be usable during development. 


## 🧠 1. Introduction
Tired of clicking endlessly to sort or enable/disable mods? Use this mod to pin to the top, pin to the bottom, enable, or disable with just one click! It also supports dragging and sorting of items. A restart is required the first time you enable it.

[GitHub Repository](https://github.com/JMC2002/Duckov-BetterModManager)

## ⚙️ 2. Features
- Added two buttons for each item in the MOD management page to move the mod to the top or bottom, effectively equivalent to pressing the up/down button multiple times.
- A checkbox is added on the left side of the MOD entry. Checking the box enables all mods, while unchecking it disables all mods.
- Hold the mouse button on a mod entry to drag and reorder it.
- This mod makes extensive use of reflection, and it may become outdated with future game updates
- Click a mod entry to select it, then press **W** or the up arrow key to move the mod up, **S** or the down arrow key to move it down, and press **Enter**, **ESC**, or click anywhere to deselect.
- You can now scroll the interface by dragging with the right mouse button (same behavior as left-click dragging without mods).

## 🔔 3. Notes
- This mod can be safely uninstalled and will not affect your save files.
- A restart is required the first time the mod is activated.
- This mod does not depend on any prerequisites, but if you're using a Mac and have subscribed to the Harmony.lib mod which prevents this mod from working, try moving this mod above it in the load order.
- The principle behind enabling/disabling all mods is to enable them sequentially from the first mod entry downwards. It is normal to experience a brief delay if there are too many mod entries.
- If you want to disable all mods but notice the checkbox is not checked, please check the box first.

## 🧩 4. Compatibility
- This mod modifies the MOD menu UI, which may conflict with other mods that modify the same UI.
- This mod uses the Harmony framework, so it may conflict with other mods that also use Harmony.
- This mod makes extensive use of reflection, and it may become outdated with future game updates.

## 🧭 5. TODO
- Add a one-click select/deselect all feature. ✔️

**If you like this mod, please consider leaving a star~**
