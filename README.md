# Tenali Raman Storybook 2

Unity **6000.3.10f1** AR storybook (Vuforia + URP + Addressables/CCD).

## Setup after cloning

Two package payloads are deliberately **not** in git — the Vuforia tarball is 138 MB, past
GitHub's 100 MB file limit, and the embedded Input System is a full package copy. Git tracks
`Packages/manifest.json` and `Packages/packages-lock.json`, which expect both at these exact
paths, so the project will not compile until you restore them:

| Path | Where to get it |
| --- | --- |
| `Packages/com.ptc.vuforia.engine-11.4.4.tgz` | Vuforia developer portal (Engine 11.4.4, Unity tarball), or copy from another storybook project |
| `Packages/com.unity.inputsystem/` | Copy the folder from another storybook project — an embedded copy of Input System 1.18.0 |

Drop both in place **before** opening the project. Unity resolves the rest from the registry
on first open.

### If packages went missing

Symptom: dozens of `CS0234`/`CS0246` errors (`TMPro`, `UnityEngine.UI`, `InputSystem`,
`AddressableAssets`, `Splines`, `Vuforia`) plus
`Couldn't open include file '.../ShaderLibrary/Core.hlsl'` in the shader compiler logs.

Cause: `Packages/manifest.json` was reset to Unity's bare default. Check with
`grep -c '"com\.' Packages/manifest.json` — a healthy manifest has 53 dependencies, and the
editor log should report `[Package Manager] Registered ~70 packages`, not 37.

Fix: quit Unity, `git checkout Packages/manifest.json`, delete `Packages/packages-lock.json`,
restore the two payloads above, and reopen.
