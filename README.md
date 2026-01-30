# Synty Animation Poser

An editor tool for Unity that allows you to quickly place Synty Studios POLYGON characters with random animation poses from the ANIMATION series, directly in the Scene View.

## Disclaimer

This is an unofficial community tool created by [@digitalbreed](https://bsky.app/profile/digitalbreed.bsky.social). The author is not affiliated with, sponsored by, endorsed by, or otherwise connected to Synty Studios Limited. This tool is provided as-is for the community's convenience.

## Features

- **Pack Selection**: Select from preconfigured animation packs and POLYGON art packs
- **Smart Filtering**: Filter animations and characters by name (supports comma-separated inclusions and exclusions)
- **Quick Placement**: Click anywhere in the Scene View to place characters with random poses
- **Flexible Alignment**:
  - Align characters to collision normals or a custom vector
  - Random Y-axis rotation for variety
- **Material Randomization**: Attempts to find similar material definitions and apply a random one for greater variety
- **Head Rotation**: Optional random head rotation with configurable horizontal (yaw) and vertical (pitch) ranges

## Limitations

- **No gender specific animations**: This tool doesn't know whether a character is male, female, or anything else. Therefore, animations are picked and assigned randomly. This shouldn't matter for 99.9% of the static poses, though.

## Installation

- **Via Package Manager (recommended):**

  1. Open the Package Manager in Unity
  2. Click the "+" icon in the top left of the window
  3. Select "Install package from git URL..."
  4. Enter `https://github.com/digitalbreed/SyntyAnimationPoser.git`

- **Manual copying**

  1. Copy the `Editor` folder (containing `SyntyAnimationPoserWindow.cs`) to your project

The tool will appear in the Unity menu bar: **Tools > digitalbreed > Synty Animation Poser**

## Usage

### Initial Setup

1. Open the window: **Tools > digitalbreed > Synty Animation Poser**
2. Configure your animation packs and art packs:
   - Edit the `DEFAULT_ANIMATION_PACKS` and `DEFAULT_ART_PACKS` arrays in the script with your Unity GUIDs
   - Or use the checkboxes in the window to enable/disable packs
3. Click **Rescan** (or **Start** will auto-rescan the first time) to scan for assets

### Placing Characters

1. Click **Start** to enter placement mode
2. Click anywhere in the Scene View to place a random character with a random animation pose
3. Characters will be placed at the click location with proper alignment

### Filter Options

- **Animation name contains**: Filter animations by name (e.g., "idle, walk, !run")
- **Character name contains**: Filter characters by name (e.g., "male, female, !zombie")
- Use the quick-fill buttons to add common filters
- Click **X** to clear filters
- Use "!" as a prefix to exclude certain words
- Filters are applied in real-time without requiring a rescan

### Placement Options

- **Parent Transform**: Optional parent transform for placed characters
- **Use Collision Normal**: Align characters to surface normals (unchecked = use Alignment Vector)
- **Alignment Vector**: Custom alignment direction (default: world up)
- **Random Y Rotation**: Random rotation around the character's up axis
- **Rotate head**: Enable random head rotation
  - **Head horizontal range (yaw)**: Total range for left/right head movement
  - **Head vertical range (pitch)**: Total range for up/down head movement

## Requirements

- Unity 6 or later (may work with earlier versions; not tested!)
- Synty Studios POLYGON art packs
- Synty Studios ANIMATION packs

## Acknowledgements

This tool was inspired by [a post by Mike W on Bluesky](https://bsky.app/profile/gekido.bsky.social/post/3mbnjblkj6s2a) who did something similar.

## License

MIT licensed, see [LICENSE.md](LICENSE.md)
