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
