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

This plugin has **no** in-game UI.

[releases]: https://github.com/01010101lzy/kk-lipsync/releases

## License

MIT. The OVR Lip Sync code follow their own license (see `OVRLipsync/Readme.md`).

---

Codename _`Seventeen`_