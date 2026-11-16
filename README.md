# YmapDedupe

A small Windows tool that finds and removes **duplicate / overlapping props** inside large collections of GTA V / FiveM `.ymap` files.

When you work with big map packs, it is common to end up with many props placed at exactly the same position (or almost the same position) in different `.ymap` files, or even inside the same file. This causes z-fighting, wasted streaming and a heavier map. YmapDedupe scans whole map folders, tells you which ymaps overlap, and writes cleaned copies of them. Your original files are never modified.

## What it does

1. You select **one or two map folders**. The tool finds every `.ymap` file in them, including all subfolders.
2. Each `.ymap` is read directly in its binary form (no XML conversion needed) using `CodeWalker.Core.dll` from your CodeWalker installation.
3. Props are compared **across all ymaps of both folders at once**. Two props are considered duplicates when all of these match:
   - same model (archetype hash)
   - position within a tolerance (default 0.02 m, adjustable in the window)
   - same rotation
   - same scale
4. The first prop found is kept; later copies are marked for removal. Folder order and then file name order decide which one is "first", so the first map you select has priority.
5. Results are shown in a table: for every ymap you see its prop count, how many props overlap, how many would be removed, a status and the files it overlaps with the most.
6. Ymaps whose **every prop already exists in other ymaps** are reported as `FULL DUPLICATE`. These are not deleted; they are copied into a separate `_deletable` folder so you can decide.
7. When you click save, cleaned copies are written to a new output folder, keeping the **same ymap names and the same subfolder structure** as your originals.

## Output layout

If you select the folders `mapA` and `mapB` and the output folder `C:\clean`, you get:

```
C:\clean\
  mapA\   (same subfolders and ymap names as the original mapA)
  mapB\   (same subfolders and ymap names as the original mapB)
  _deletable\   (fully duplicate ymaps, same structure; optional)
  ymap_summary.csv
  ymap_overlap_details.csv
```

Ymaps without overlaps are copied byte for byte. Only ymaps that actually lose props are re-saved.

## Safety

- Original files are never written to. The output folder must be separate from the map folders; the tool refuses to continue otherwise.
- Only `.ymap` files are read. Other files (`.ydr`, `.ydd`, `.ytyp`, `.ybn`, scripts, ...) are not touched.
- Every cleaned ymap is re-opened after saving and its prop count is verified. If it does not match, the original file is copied instead and the error is shown.
- Props that belong to a LOD hierarchy (parent or child) are never removed; they are reported as skipped.
- MLO instances are left alone.

## Requirements

- Windows 10 / 11 (uses the .NET Framework that ships with Windows).
- A CodeWalker installation. In the window, select the folder that contains `CodeWalker.exe` and `CodeWalker.Core.dll`. No CodeWalker files are included in this repository.

## Usage

1. Run `YmapDedupe.exe`.
2. Select map folder 1 (and optionally map folder 2), the output folder and the CodeWalker folder.
3. Click **Scan and Find Overlaps** and review the table and the log.
4. Click **Save Cleaned Copies**.
5. Check the results in your output folder before replacing your real map files.

The interface is available in **English** and **Turkish** (selector at the top of the window).

## Building from source

No Visual Studio is required.

1. Put `YmapDedupe.cs` and `build.bat` in the same folder.
2. Double-click `build.bat`. It uses the C# compiler that ships with Windows and creates `YmapDedupe.exe` next to it.

The program has no build-time dependency on CodeWalker; `CodeWalker.Core.dll` is loaded at run time.

## Limitations

- When two props match, only the first one is kept. Other differences between them (flags, LOD distance, tint, extensions attached to the removed prop) are lost.
- Different models that overlap each other (for example two different decals on the same wall) are not detected, only copies of the same model.
- Only the folders you select are compared. Overlaps with vanilla GTA V props or with other resources are not detected.
- Cleaned ymaps are written with CodeWalker's own ymap writer, the same as opening and saving them in CodeWalker.
- The tool depends on CodeWalker internals. A very different CodeWalker version may not work.

## Disclaimer

This is an early release. Always keep a backup of your maps and test the cleaned files in-game before using them. The software is provided "as is", without warranty of any kind.
