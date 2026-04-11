# Audio Manager

🎵 This README explains how to set up and use the `BattleTurn Audio Manager` package in a Unity project.

## ✨ What This Package Does

This package organizes audio with the following structure:

- `AudioAlbumManagerSO`: the root asset that contains all audio albums.
- `AudioAlbumSO`: a top-level album such as `SFX` or `MFX`.
- `AudioCategorySO`: a category inside an album, such as `UI`, `Weapon`, or `BGM`.
- `AudioContentSO`: a single audio clip entry.
- `SoundManager` and `MusicManager`: runtime managers for SFX and music playback.
- `AudioPlayer`: a component with Inspector dropdowns for choosing audio directly in the scene.

The package also generates these files automatically:

- `GameMixer.mixer`
- `AudioType.cs`
- `AudioNames.cs`
- `AudioMixerExposedParameter.cs`
- `AudioMixerGroupName.cs`

Generated files are stored in:

```text
Assets/Plugins/BattleTurn/Generated/AudioManager
```

## 📦 Prerequisite

Before installing `AudioManager`, you should install `dependency_downloader` first if you want package dependencies to be installed automatically.

Install it from:

```text
https://github.com/BattleTurn/dependency_downloader.git
```

This lets `AudioManager` auto-install its package dependencies from `Dependency.json`.

If you do not want to use `dependency_downloader`, install these dependencies manually before adding `AudioManager`:

- `UniRx`
- `NaughtyAttributes`
- `UniTask`

## 🚀 Initial Setup

After adding the package to your project:

1. Make sure `dependency_downloader` is already installed, or manually install `UniRx`, `NaughtyAttributes`, and `UniTask` first.
2. Add `AudioManager` to your project.
3. Open Unity and wait for compilation to finish.
4. If the `AudioManager Extra` popup appears, import the requested `.unitypackage`.
5. If the popup does not appear but you still need to import it manually, use:

```text
Audio/Import Extra Package
```

On first load, the package will try to create `GameMixer` automatically and auto-wire the mixer into any existing `AudioAlbumManagerSO` assets.

## 🛠 Recommended Setup Flow

Follow this order to avoid missing references or stale generated code.

### 1. Create AudioContent assets

Create assets from:

```text
BattleTurn/Audio/AudioContent
```

Each `AudioContentSO` contains:

- `_name`: the key used to play the audio in code.
- `_clip`: the actual `AudioClip`.

Recommended naming rules:

- No spaces.
- Unique within the same category.
- Use stable names, because generated code depends on them.

### 2. Create Category assets

Create assets from:

```text
BattleTurn/Audio/AudioCategorySO
```

Inside each category:

- Set `_name`.
- Assign `_audioContents`.

Example categories:

- `UI`
- `Footstep`
- `Weapon`
- `BGM`

### 3. Create Album assets

Create assets from:

```text
BattleTurn/Audio/AudioAlbumSO
```

In most projects you should create at least two albums:

- `SFX`
- `MFX`

Inside each album:

- Set `_name` to the album name.
- Assign `_audioCategories`.

⚠ Important:

- `SoundManager` reads the album named `SFX`.
- `MusicManager` reads the album named `MFX`.

If you rename these albums to something else, the default managers will no longer resolve the data correctly.

### 4. Create AudioAlbumManagerSO

Create the asset from:

```text
BattleTurn/Audio/AudioAlbumManagerSO
```

Inside this asset:

- Assign `_audioAlbums`, usually `SFX` and `MFX`.

### 5. Create or update the mixer

Use:

```text
Tools/Audio/Create Game Mixer
```

This menu will:

- create `GameMixer.mixer`
- auto-wire the mixer into `AudioAlbumManagerSO` assets
- generate `AudioMixerExposedParameter.cs`
- generate `AudioMixerGroupName.cs`

If the mixer already exists and you only want to refresh generated files, use:

```text
Tools/Audio/Update Mixer Exposed Parameter
Tools/Audio/Update Mixer Group Names
Tools/Audio/Auto Wire AudioAlbumManager AudioMixer
```

### 6. Build generated audio code

Select `AudioAlbumManagerSO` in the Inspector and click:

```text
Build AudioAlbumManagerSO
```

This rebuilds:

- `AudioType.cs`
- `AudioNames.cs`

Rebuild whenever you:

- add a new album
- rename an album
- add a category
- rename a category
- add audio content
- rename audio content

## 🎬 Scene Setup

This project currently uses two runtime managers:

- `SoundManager`
- `MusicManager`

You should place both of them in your bootstrap scene or first game scene, then assign the same `AudioAlbumManagerSO` asset to their `audioManager` field.

Why this matters:

- If no manager exists in the scene, the singleton creates a new `GameObject` automatically.
- That runtime-created object does not automatically receive an `AudioAlbumManagerSO` reference.
- The result can be silent playback or null/missing-data issues.

Recommended setup:

1. Create a `SoundManager` GameObject and add the `SoundManager` component.
2. Create a `MusicManager` GameObject and add the `MusicManager` component.
3. Assign the same `AudioAlbumManagerSO` asset to both.
4. Keep them in the first scene of the game.

These managers already call `DontDestroyOnLoad`, so they only need to be created once.

## 💻 Playing Audio in Code

Main namespaces:

```csharp
using BattleTurn.AudioManager.Runtime;
using BattleTurn.AudioManager.Runtime.Implemented;
```

### Play basic SFX

```csharp
SoundManager.Instance.Play("Click");
```

### Play basic music

```csharp
MusicManager.Instance.Play("MainTheme");
```

### Play by category + audio name

```csharp
SoundManager.Instance.Play("UI", "Click", null);
MusicManager.Instance.Play("BGM", "MainTheme", null);
```

### Play one-shot audio

```csharp
SoundManager.Instance.PlayOneShot("Explosion");
```

### Play at a world position

```csharp
SoundManager.Instance.PlayAt("Explosion", hitPoint);
```

### Play while following a transform

```csharp
SoundManager.Instance.PlayFollow("EngineLoop", targetTransform);
```

### Use generated names instead of hard-coded strings

```csharp
SoundManager.Instance.Play(
    AudioNames.SFX.Categories.UI,
    AudioNames.SFX.UI.CLICK,
    null);
```

If the generated names are missing or outdated, select `AudioAlbumManagerSO` and click `Build AudioAlbumManagerSO` again.

## 🔁 Playback Parameters

The managers support playback parameters through `IParameterizable`.

Example:

```csharp
var parameters = new IParameterizable[]
{
    new DelayParameter(0.25f),
    new LoopParameter(1),
};

SoundManager.Instance.Play("UI", "Click", parameters);
```

Available parameters:

- `OneShotParameter`: plays audio using one-shot behavior.
- `WorldPositionParameter`: places the `AudioSource` in world space.
- `FollowParameter`: attaches the `AudioSource` to a `Transform`.
- `DelayParameter`: delays playback.
- `LoopParameter`: loops a fixed number of times or forever.

`LoopParameter` notes:

- `0`: play once.
- `1`: play twice in total.
- `-1`: infinite loop.

`DelayParameter` notes:

- `DelayType.OnStart`: delay only on the first playback.
- `DelayType.EveryLoop`: delay on every loop.

## 🎚 Mixer Parameters During Playback

You can pass `AudioMixParameter` values to update exposed mixer parameters while playing:

```csharp
SoundManager.Instance.Play(
    "Explosion",
    new AudioMixParameter(AudioMixerExposedParameter.SFX_LOWPASS_CUTOFF_FREQUENCY, 1200f));
```

Another example:

```csharp
MusicManager.Instance.Play(
    "MainTheme",
    new AudioMixParameter(AudioMixerExposedParameter.MUSIC_VOLUME, -10f));
```

✅ Use names from the generated `AudioMixerExposedParameter` class instead of typing them manually.

## 🎮 Using AudioPlayer in the Inspector

The `AudioPlayer` component lets you select and play audio directly from the Inspector.

Main fields:

- `audioType`: choose `SFX` or `MFX`.
- `categoryName`: category dropdown filtered by `audioType`.
- `sfxName` or `mfxName`: audio name dropdown filtered by category.
- `mixerGroups`: a list of `AudioMixParameter` values applied on play.
- `fadeOnChange`: stops the previous audio with a short fade.

Available methods on the component:

- `Play()`
- `PlayOneShot()`
- `PlayAt(Vector3 position)`
- `PlayFollow(Transform follow)`
- `Pause()`
- `Stop()`

Good use cases:

- UI buttons
- simple triggers
- scene objects that need self-contained audio playback

## 🔊 Volume and Stop Control

Each manager has its own `Volume` property:

```csharp
SoundManager.Instance.Volume = 0.8f;
MusicManager.Instance.Volume = 0.5f;
```

The value is stored in `PlayerPrefs` using the manager key.

Stop everything:

```csharp
SoundManager.Instance.StopAll();
MusicManager.Instance.StopAll();
```

Stop everything with fade:

```csharp
MusicManager.Instance.StopAll(0.25f);
```

Stop a specific `AudioSource` returned from playback:

```csharp
var source = SoundManager.Instance.Play("Explosion");
SoundManager.Instance.Stop(source, 0.15f);
```

## 📦 Recommended Workflow

When adding new audio, the safest workflow is:

1. Create `AudioContentSO`.
2. Assign it to an `AudioCategorySO`.
3. Make sure the category belongs to the correct album, `SFX` or `MFX`.
4. Make sure the album is assigned to `AudioAlbumManagerSO`.
5. Select `AudioAlbumManagerSO` and click `Build AudioAlbumManagerSO`.
6. If mixer groups or mixer parameters changed, run the relevant `Tools/Audio` menu again.

## 🧩 Common Issues

### `Play` is called but nothing is heard

Check these first:

- `SoundManager` or `MusicManager` exists in the scene.
- The `audioManager` field on the manager is assigned.
- `AudioAlbumManagerSO` contains the correct albums.
- The audio name used in code is valid.
- The album is assigned to a valid `MixerGroup`.
- Mixer volume or manager volume is not too low.

### Category or audio dropdowns are empty

This is usually caused by stale generated data or an invalid `AudioAlbumManagerSO` setup.

Check:

- the `AudioAlbumManagerSO` asset contains both `SFX` and `MFX`
- categories and audio contents are assigned correctly
- `Build AudioAlbumManagerSO` has been run

### Asset names changed but generated code did not update

Select `AudioAlbumManagerSO` and click:

```text
Build AudioAlbumManagerSO
```

## 🧪 Minimal Example

```csharp
using UnityEngine;
using BattleTurn.AudioManager.Runtime.Implemented;

public sealed class DemoPlayAudio : MonoBehaviour
{
    public void PlayClick()
    {
        SoundManager.Instance.Play("Click");
    }

    public void PlayBgm()
    {
        MusicManager.Instance.Play("MainTheme");
    }
}
```

## 🏷 Naming Recommendations

To keep generated code readable, use a simple naming convention:

- Album: `SFX`, `MFX`
- Category: `UI`, `Weapon`, `BGM`, `Ambient`
- Audio name: `Click`, `Explosion`, `MainTheme`, `RainLoop`

Avoid:

- names with spaces
- names that change frequently
- duplicate audio names inside the same category

---

If needed, a shorter quick-start README for the team or a full first-scene setup example can be added next.