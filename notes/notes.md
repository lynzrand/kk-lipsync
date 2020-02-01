
# Notes

## KoiKatsu's provided mouth shapes

| ID |  Name                | Shape Description            |
|----| -------------------- | ---------------------------- |
| 00 | Default              |                             |
| 01 | Smiling              |
| 02 | Happy (broad)        |
| 03 | Happy (moderate)     |
| 04 | Happy (slight)       |
| 05 | Excited (broad)      |
| 06 | Excited (moderate)   |
| 07 | Excited (slight)     |
| 08 | Angry #1             | small /e/ mouth
| 09 | Angry #2             | /ae/ mouth
| 10 | Serious #1           | /a/ mouth
| 11 | Serious #2           | /ah/ mouth
| 12 | Hate                 | looks like /i/ sound, with teeth exposed
| 13 | Lonely               | small mouth
| 14 | Impatient            | /ah/
| 15 | Dissatisfied         | large /u/ mouth
| 16 | Amazed               | very small mouth
| 17 | Suprized             | large /o/ mouth
| 18 | Suprized (moderate)  | smaller than the last one
| 19 | Smug                 | /eh/ mouth
| 20 | Playful              | closed mouth with tongue sticked out
| 21 | Eating               | 
| 22 | Hold in mouth        | ~~blow___~~
| 23 | Kiss                 | small kissing mouth
| 24 | Tongue out           | tongue always stick out. ~~ahegao~~
| 25 | Small /a/            | <-
| 26 | Big /a/              | <-
| 27 | Small /i/            | <-
| 28 | Big /i/              | <-
| 29 | Small /u/            | <-
| 30 | Big /u/              | <-
| 31 | Small /e/            | <-
| 32 | Big /e/              | <-
| 33 | Small /o/            | <-
| 34 | Big /o/              | <-
| 35 | Small /n/            | <-
| 36 | Big /n/              | <- 
| 37 | Catlike              | 0w0
| 38 | Triangle             | 0ʌ0
| 39 | Cartoony smile       | 0u0

## AI Shoujou's provided mouth shapes
0 default
1 smile slight
2 smile teeth out
3 unhappy slight
4 unhappy moderate
5 unhappy large (lip pursed)
6 hate (teeth)
7 right corner up
8 left corner up
9 lick left corner
10 open mouth (slight), /a/
11 open mouth (big), large /o/
12 kiss
13 blowjob
14 lick
15 lick small
16 lick moderate
17 lick open mouth
18 lick large open mouth
19 /a/
20 /i/
21 /u/
22 /e/
23 /o/
14 /n/

| ID |  Name                | Shape Description            |
|----| -------------------- | ---------------------------- |
| 00 | Default              |                             |
| 01 | Smiling              |
| 02 | Smiling (with teeth) |
| 03 | Happy (moderate)     |
| 04 | Happy (slight)       |
| 05 | Excited (broad)      |
| 06 | Excited (moderate)   |
| 07 | Excited (slight)     |
| 08 | Angry #1             | small /e/ mouth
| 09 | Angry #2             | /ae/ mouth
| 10 | Serious #1           | /a/ mouth
| 11 | Serious #2           | /ah/ mouth
| 12 | Hate                 | looks like /i/ sound, with teeth exposed
| 13 | Lonely               | small mouth
| 14 | Impatient            | /ah/
| 15 | Dissatisfied         | large /u/ mouth
| 16 | Amazed               | very small mouth
| 17 | Suprized             | large /o/ mouth
| 18 | Suprized (moderate)  | smaller than the last one
| 19 | Smug                 | /eh/ mouth
| 20 | Playful              | closed mouth with tongue sticked out
| 21 | Eating               | 
| 22 | Hold in mouth        | ~~blow___~~
| 23 | Kiss                 | small kissing mouth
| 24 | Tongue out           | tongue always stick out. ~~ahegao~~

## Call Stack

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

