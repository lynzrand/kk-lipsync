# KK-Lipsync

An experimental lip sync project built for KoiKatsu and other Illusion games.

## Techniques and Explanation

This lip sync plugin will try to analyze the output audio for the mouth positions showed in the following table (primarily for Japanese usage). This plugin does not calculate or change tongue position. The positions are based on mouth position presets in Illusion games.

The plugin will then try to blend these motion presets onto _the current_ mouth shape, while trying to balance the mouth open-ness parameter.

| Position | Description                                                                  |
| -------- | ---------------------------------------------------------------------------- |
| Idle     | Idle position. Nothing special.                                              |
| `A`      | Open mouth. Used by /a/ and /ɑ/.                                             |
| `I`      | Lips open with teeth closed. Used by /i/ and /th/.                           |
| `U`      | Small mouth with lips drawn together. Used by /u/.                           |
| `E`      | Open mouth with smaller open-ness and wider corners. Used by /æ/.            |
| `O`      | Small round mouth. Used by /ɔ/ and /o/.                                      |
| `a`      | Smaller version of `A`. Used by /ε/.                                         |
| `i`      | Smaller version of `I`. Used by /ts/, /t/, /d/, /z/.                         |
| `u`      | Smaller version of `U`. Used by /w/.                                         |
| `e`      | Smaller version of `E`. Used by /e/, /k/, /g/, /r/, /h/ and breathing voice. |
| `o`      | Smaller version of `O`. Currently not used.                                  |
| `N`      | Tightly closed mouth. Used by /n/, /m/, /p/, /b/, /f/.                       |


## License

MIT.

---

## Notes

### Call Stack

It all comes from the act of setting an audio source to the character. `ChaControl.SetVoiceTransform()` does this job, setting `ChaControl.asVoice` to an existing Audio Source.

```
[something that sets voice on a character]
	ChaControl.SetVoiceTransform()
		ChaControl.asVoice::set()
```

Every time an update happens, the character re-calculates its mouth blend shape.

```
Manager.Character.Update()
    ChaControl.UpdateForce()
        ChaControl.UpdateBlendShapeVoice()      
            // This is the method we want to hack into. May need to bypass it in order to
            // use custom data structures for more data to work with.

            WavInfoControl.WavInfoData.GetValue(float)	
                // This method seems to be loading from pre-generated lip data
                // file. May need to bypass it if we want to generate our own lip data.

            FBSAssist.AudioAssist.GetAudioWaveValue(AudioSource, float)
                // Alright. This method calculates audio wave value and makes it lip data.
                // Its algorithm is simple: calculate the quadratic mean of the last 1024 data,
                // multiply with a correction value and call it a day.
                //
                // We need to hack into this method as well. Maybe via extending it and replace the 
                // original one. That way we also need to add a postfix function of 
                // `ChaControl.InitializeControlFaceAll()` which is pretty easy.
            
            FaceBlendShape.SetVoiceValue()
                // We need a new FaceBlendShape class as well to hold blend data. More hacks!

FaceBlendShape.LateUpdate()
    // Here's the blend part. Excited!
       
    FBSCtrlMouth.CalcBlend()
        // More methods to hack! This method just passes the call onto the next frame of 
        // the call stack. May need to bypass if we want our own `CalculateBlendShape()`
        //
        // p.s. bad naming practices on `FixedRate`! Surely they wanted to name it the "rate" of
        // changing, but it is used as the "value" here.
        
        FBSBase.CalculateBlendShape()
            // This method calculates the real blending parameters. Because we are not using a single
            // parameter (aka lip open-ness) for blending any more, we might need to bypass and rewrite
            // this method.

            SkinnedMeshRenderer.SetBlendShapeWeight(int, float)
                // This method does the real job of blending meshes. It is the final destination of our
                // modified data.
```

To finalize, the way to patch this thing is:

- Create a custom class (`LipsyncFaceBlender`) to replace `FaceBlendShape`.
- Create a custom class (`LipsyncMouthController`) to replace `FBSCtrlMouth`.
- Create a replacement function of `FBSCtrlMouth.CalcBlend()`.
- Create a replacement function of `ChaControl.UpdateBlendShapeVoice()`.
- Bypass `ChaControl.UpdateBlendShapeVoice()`. Call the custom function in `LipsyncFaceBlender` instead.
- Bypass `FBSCtrlMouth.CalcBlend()`.

---

Codename _`Seventeen`_