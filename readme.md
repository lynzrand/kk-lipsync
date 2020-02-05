# KK-Lipsync

An experimental lip sync project built for KoiKatsu and AI Shoujou.

This plugin requires BepInEx and Harmony installed.

This plugin uses Oculus's OVR Lip Sync.

## Usage

Grab the latest release from [the Releases tab][releases], and extract it into the folder Koikatsu is installed in. The folder structure should look like below after extraction:

```
koikatsu/
    BepInEx/
        core/
        ...
        KKLipsync.dll <-- 
        OVRLipSyncRef.dll <--
    KoiKatu_Data/
    ...
    Koikatu.exe
    KoikatuVR.exe
    OVRLipSync.dll <--
```

```
ai-shoujou/
    BepInEx/
        plugins/
            AILipsync.dll <-- 
            OVRLipSyncRef.dll <--
    ...
    AI-Syoujyo.exe
    OVRLipSync.dll <--
```

This plugin has **no** in-game UI. Simply install and see the difference.

[releases]: https://github.com/01010101lzy/kk-lipsync/releases

## Changelog

### 0.1.3

- [AILipsync] ADDED overdrive factor config to make mouth motion larger
- ADDED config option to toggle lipsync (plugin can be turned off when encountering issues)
- FIXED mouth openness can't be preserved when editing multiple characters in Studio

### 0.1.2

- [AILipsync] ADDED support for AI Shoujou
- Refactored folder structure

### 0.1.1

- Fixed an issue where morphing coefficients were wrong
- Fixed stuttering in some cases

### 0.1.0

Initiali release.

## License

MIT. The OVR Lip Sync code follow their own license (see `OVRLipsync/Readme.md`).

---

Codename _`Seventeen`_