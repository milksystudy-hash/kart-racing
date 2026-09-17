# CLAUDE.md

## Language

**Always reply to the user in Korean.** This file stays in English; the conversation does not.

## Project

`Desktop\Racing` — Unity 6000.3.18f1 / URP 17.0.1. **환웅박물관 최후의 그랑프리** — a
3D 캐주얼 미션 레이싱 + 스토리 어드벤처 demo about saving a teddy-bear museum from demolition.

**The design doc is the authority**, not this file and not anything remembered from an earlier
session: `Downloads\환웅박물관_최후의_그랑프리_게임기획서.pdf` (18 pp, 2026-09-14). Re-read it
before planning. **A 디저트 랜드 theme was scrapped on 2026-09-14 — never propose it again.**

- **Deadline 2026-10-16.** Single-player Windows build, 15–25 min play time.
- MVP scope: **museum circuit track ×1** reused across chapters by moving doors/lights/props,
  **6 character cards (4 playable: 정이감·한시우·한세운·한세진, slots 5·6 locked)**, shared kart
  body recoloured per character, 4 missions (코인 수집 / 지정 충돌 / 문서 수집 / 제한 시간),
  prologue + 4 story chapters + epilogue, 2D portrait dialogue.
- **Excluded this deadline:** online multiplayer, 6 unique karts, multiple standalone tracks,
  shop economy. 드리프트 and 수동 부스터 are listed as 보류 (already built — leave them, but
  don't extend them).
- Races are **3인칭 백뷰**, 60–120 s each. Story scenes are 3D museum backdrop + 2D portraits.
- Visual direction: 크림색 / 짙은 남색 / 나무색 base, saturated colour only on characters and
  karts; the villain's ads deliberately clash in 금색 + 자홍색.
- Perf target: 1280×720 @ 30 fps on a weak laptop, 150k–250k tris on screen, baked lighting.
- **Emergency cut-down** if core systems aren't stable by 2026-10-01: 1 playable character,
  no AI karts, 2 mission types, 3 story scenes.

### Division of labour — confirm before assuming

The doc's §9.3 lists work "Claude 가 대신할 수 없는" — attaching scripts, Inspector wiring,
collider/trigger/tag/layer setup, build settings, play-feel tuning, build verification.
In practice this session **has** been doing scene construction through editor scripts
(`TestSceneBuilder`, `LobbySceneBuilder`). That is faster, but it is not what the doc assumes.
Ask which they want before generating more scenes.

The doc also asks for **one feature per request** (§9.1) and one script per responsibility —
that is their defence against the "Claude 코드 충돌" risk in §10.1. Respect it: don't ship
seven managers at once. Script order from §9.2: `KartController` → `Checkpoint`/`RaceManager`
→ `CoinPickup` → `BumpTarget` → `MissionManager` → `StoryUnlockManager` → `SaveProgress`.

The user cannot write C#, so never leave a step that requires them to.

## Off-limits — hard rule, no exceptions

**Never read, write, or run any command against 온실 / Greenhouse.** Reading counts as
touching. Teammates are actively committing there and the user is blamed if anything breaks.

| Path | What it is |
|---|---|
| `Documents\GitHub\Greenhouse` | team repo, remote `github.com/kkng932/Greenhouse` |
| `Desktop\Greenhouse-main` | plain folder copy |
| `Desktop\My project` | 온실 personal Unity project |
| `D:\` | the user's USB stick |

The user repeats this at the start of requests on purpose. That repetition is welcome —
never treat it as redundant, and never push back on it. If a task seems to need information
from those folders, say what you need and let the user supply it.

To prove non-interference, list what **you** wrote. That needs no access to their repo.

## Unity gotchas in this project

- **Input System package only** (`activeInputHandler: 1`). Legacy `Input.GetAxis` /
  `Input.GetKey` compile fine but **throw at runtime**; IMGUI `Event.current` key presses are
  unreliable. Read `Keyboard.current` / `Gamepad.current` — go through `KartInput.cs`.
- The kart sits on layer 2 (**Ignore Raycast**) with `groundMask = ~(1 << 2)`, or its
  suspension raycast hits its own collider.
- `CreatePrimitive(Cylinder)` ships a **CapsuleCollider**. Flatten it (scale y ≪ x/z) and the
  capsule degenerates into a **sphere of radius x/2** — so a disc-shaped "floor" becomes a
  huge dome you stand inside and fall through. Use `LobbySceneBuilder.Disc()`: destroy the
  capsule, add a `BoxCollider` with `size = (1, 2, 1)`, because the cylinder mesh is 2 units
  tall while a box is 1 — with the default size the walkable surface sits below the visible
  top. Cubes are unaffected (`BoxCollider`, both 1 unit).
- Compiling proves nothing about scene builders. **Run `-executeMethod` end-to-end in a
  throwaway project** and check the generated scene — that is how both the missing
  `Assets/Materials` folder and the capsule-floor bug were caught.
- `Editor/TestSceneBuilder.cs` **overwrites** `Testbed.unity` and `Track.unity`, and
  `LobbySceneBuilder.cs` overwrites `Lobby.unity` (`GallerySceneBuilder` and
  `MyTrackSceneBuilder` ask first; `StoryRigBuilder` and `MuseumLook` never overwrite anything).
  Warn before suggesting either once anything has been hand-placed.
- Batchmode refuses to open a project the Editor already has open. Don't close their editor —
  mirror the scripts into a throwaway project and batchmode that instead.

## FBX conventions

- Hierarchy: `<Name>` (root, **rot 0 / scale 1**) > `<Name>_Geo`
  (**270.02° X axis fix — never zero it**) > the meshes.
- Pivot at footprint XY centre, lowest point `y = 0`. Kart: 전장 1.5 / 전폭 1.1 / 바퀴 ø0.4 /
  축거 ±0.55 / 윤거 ±0.50.
- **Characters are 3등신** (revised 2026-09-14 from 2.5등신): 서 있는 키 **1.25 m**,
  머리 **0.42 m**, 어깨 폭 **0.34 m**, origin at the **feet (y = 0)** so it drops onto a
  lobby pedestal as-is. Seated height stays **0.95 m**.
- **One model per character, not two.** Per the design doc §6.1 the kart hides the lower
  body, so the standing model goes straight into the seat — never ask them to model a
  separate seated version.
- Lobby stands expose a `ModelAnchor` at the pedestal top; a feet-at-origin FBX lands
  correctly with no repositioning.

### Swapping grey boxes for real models — the four rules

The user has heard that grey-boxing first causes "콜라이더와 스케일의 배신, 애니메이션과 계층
구조 꼬임". Those failures are real but every one of them is avoidable, and this project is
already built around avoiding them. Keep it that way, and reassure with specifics, not vibes.

1. **Put the FBX *inside* the shell, never replace it.** `KartVisual` (kart) and `ModelAnchor`
   (lobby stands) exist so that scripts keep pointing at a stable object. Deleting them is the
   one move that breaks the wiring.
2. **No colliders on the art.** Collision lives on the root `BoxCollider`; the FBX goes in with
   none. Swapping a model must not change physics. **Never add a `MeshCollider` to a kart or
   character** — verify with `grep -c "^MeshCollider:"` on the scene, it should stay 0.
3. **Root rot 0 / scale 1**, scale applied in Blender, `globalScale: 1` in the importer.
   The Testbed scale references (F3) exist to catch this by eye before it reaches the track.
4. **Animate only the final model.** Clips store transform paths, so a clip authored against a
   grey box breaks on swap. There are currently zero AnimationClips — the kart's lean is code
   rotating a Transform. Keep it that way until real rigs arrive.

Scripts reference objects through **Inspector fields, never `transform.Find` by name** — that
is what makes renaming safe. Don't introduce name-based lookups.
- Blender **+Y is forward** so it lands as Unity's +Z.
- **Prefer no textures**: geometry with flat colour materials. A texture-free FBX survives
  being renamed; a textured one does not. The one exception is the character's face plane.
- Tell the user to drag the **FBX icon itself**, not a mesh sub-asset — dragging a sub-mesh
  attaches `Default-Material`, which renders **magenta** under URP.
- Report polygon counts in **quads**, not tris.

## Making it not look "made in Unity"

Added 2026-09-15 after the user said the scenes looked too Unity-default. The cause was almost
never the models — it was three things, all fixable without touching geometry:

1. **Post-processing was off.** `Editor/MuseumLook.cs` creates `Assets/Settings/MuseumLook.asset`
   (Tonemapping Neutral, ColorAdjustments, WhiteBalance, ShadowsMidtonesHighlights, Bloom,
   Vignette), drops a global `Volume` in the scene, and sets `renderPostProcessing` + **SMAA** on
   every camera. Menu: **`Racing → 박물관 느낌 입히기 (지금 열린 씬)`** — non-destructive and
   idempotent, so it is safe on a scene the user has decorated. Builders call
   `MuseumLook.ApplyToOpenScene()` themselves.
2. **Contact shadows were almost invisible.** SSAO was already a renderer feature but at
   Intensity 0.4 / Radius 0.3. `TuneAmbientOcclusion()` raises it to 0.85 / 0.45 at half
   resolution. This is what makes objects sit on the floor; it is far cheaper than shadow-casting
   spot lights, and §7.6's "실시간 그림자는 주요 조명 하나만" stays intact.
3. **Every material had one finish.** `TestSceneBuilder.Finish` — 무광 / 나무 / 석재 / 광택 /
   금속 / 유리 / 발광. `MaterialAsset(color, finish)` writes `Flat_{hex}_{finish}.mat`;
   **무광 keeps the old `Flat_{hex}.mat` name** so already-saved scenes don't lose their
   materials. 유리 does the full URP transparent setup; 발광 sets `_EMISSION` so Bloom catches it.

Grading numbers are measured, not guessed — render a frame headlessly and read pixels back
(`-batchmode` without `-nographics`, `cam.Render()` to a RenderTexture; call
`DynamicGI.UpdateEnvironment()` and `probe.RenderProbe()` first or it comes out black).

- **Do not grade hard.** The first pass used contrast 14 + vignette 0.26 and dropped gallery floor
  brightness from 0.231 to **0.047** — a five-fold crush. Now contrast 6, vignette 0.13, shadows
  lifted +0.02. The user has complained about darkness before; check a measured pixel, not a vibe.
- **Do not use 광택 on large floors.** At grazing angles reflection beats diffuse, and in a dim
  room that reflection is the dark ceiling — the floor goes black at eye level. Gallery floor is
  석재 (0.18). Reflection probe intensity 0.32.
- Judge from the scene's **actual camera pose** (gallery orbit: pivot (0,1.4,0), distance 11,
  pitch 20°), not an arbitrary angle. An eye-level shot at the wall made the room look broken
  when it was fine.
- Geometry that pays for itself: floor seams (gives the eye a scale reference), skirting and
  picture rail (walls that meet the floor with a trim read as rooms), column capitals, and an
  emissive lens inside every light fixture.

## Story scenes and dialogue

Built 2026-09-15. Per §3.6 a story scene is a **still 3D backdrop + 2D portraits + nameplate +
dialogue box** — never 3D character acting. That is why adding a scene costs writing time and
almost nothing else, and why the character side of this project needs **drawing, not modeling**.

| File | Role |
|---|---|
| `Scripts/StoryScript.cs` | **the only file the user edits.** All dialogue, as a table |
| `Scripts/Cast.cs` | who exists — id, display name, nameplate colour, portrait lookup |
| `Scripts/Dialogue.cs` | `DialogueLine` + `When.*` conditions + `Talk.Say/Narrate` |
| `Scripts/DialogueRunner.cs` | playback: typewriter, advance, replay. Knows no dialogue |
| `Scripts/DialogueHUD.cs` | OnGUI drawing. Throwaway when real UI arrives; Runner survives |
| `Scripts/StoryStage.cs` | swaps the Lobby into story mode and back |
| `Editor/StoryRigBuilder.cs` | attaches the rig to `Lobby.unity`, `Racing` menu |

**There is no Story scene, and never build one.** Story scenes play *inside* `Lobby.unity` —
`StoryStage` swaps the camera to `StoryRig > StoryCamera` and pauses `LobbyOrbitCamera` /
`LobbySelector` / `LobbyHUD`, then restores them. The reason is the user's: they are filling the
lobby with their own FBX, and a separate story scene would be a **second copy of 중앙홀** that
drifts from the one they decorated. One room, one file. A first attempt on 2026-09-15 did build a
duplicate hall — the user caught it. Story scenes set elsewhere later get the same treatment
(a rig in that scene), never a hand-built copy of a room that already exists.

- `StoryRigBuilder.EnsureRig()` is **idempotent and non-destructive** — it never rebuilds the
  lobby, adds only what is missing, and leaves an existing `StoryCamera` transform alone so the
  user's framing survives. `LobbySceneBuilder.BuildLobby` calls it too, so both paths agree.
  Verified by calling it three times in one editor session: `StoryStage` stayed at 1.
- Story camera framing is the user's to set — move `StoryRig > StoryCamera` in the scene.
  Nothing in code depends on where it points.
- `StoryCamera` carries **no AudioListener** (the lobby camera has one; two is a warning), and
  mode switching toggles `Camera.enabled`, never the GameObject.

- **Variant lines are one call, not a branch**: `Talk.Say(...).OnlyIf(When.AllCollected)`.
  Conditions are evaluated **once, when the scene starts** (`StoryScript.Playable`), not per line.
  Available: `AllCollected` / `NotAllCollected` / `Has(id)` / `Missing(id)` /
  `ChapterAtLeast(n)` / `ChapterBelow(n)`. `id` is an `ExhibitCatalogue` id.
- Portraits are dropped into `Assets/Resources/Portraits/` as `{id}_{표정}.png` —
  `기본` / `기쁨` / `당황`, and a missing mood falls back to `기본`. No code change on arrival.
  Grey placeholder boxes stand in until then, so dialogue flow is fully testable with zero art.
- The typewriter paints unrevealed characters **transparent** rather than trimming the string —
  trimming makes word-wrap jump on every character.
- Scene order in Build Settings is **append-only** (`LobbySceneBuilder.RegisterScenes`):
  F1 로비 · F2 트랙 · F3 전시실 · F4 테스트베드 · F6 내 맵.
  Inserting in the middle shifts every F-key the user already memorised.
- `StoryProgress.HasSeen / MarkSeen` keeps a chapter's story from replaying on every lobby visit.
- Debug keys in the Lobby: **T** replays this chapter's story; while a story is playing,
  **1~5** pick a scene and **F9/F10** fill/clear the collection so the before/after variants can
  be compared instantly. Turn `debugKeys` off on `StoryStage` and `DialogueHUD` before submitting.
- **The dialogue is the user's to write.** They asked for the system only (2026-09-15) —
  their register is dark comedy, blunt and argumentative. `StoryScript.cs` holds a skeleton with
  the 기획서 §2.5 beats as comments per scene; don't pad it with prose in a different voice.

## Delivery rules

1. Deliver to `C:\Users\MBC-501-17\Downloads` unless told otherwise.
2. **One flat file with a self-describing name.** No folder trees, no 최종/구버전 folders.
3. Never overwrite, rename, or reorganize the user's existing files.
4. Encode variants in the filename; if several combinations are wanted, generate them all.

## Local toolchain

No `node`/`npm`, no real `python`, no Office. Use Blender's bundled Python:

```
"C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe"
```

- Python 3.13 with working `pip`. Install with `--target <scratch>/pylibs`, never into the
  Blender install.
- Set `PYTHONUTF8=1` — the console default is cp949 and UTF-8 files fail without it.
- **Long files fail through Bash heredocs here.** Use the Write tool for anything longer than
  a few lines.
- Never drive edits with a `grep`→`sed` line-number loop; an empty variable becomes a global
  substitution and eats the file.

## Track

- **Road and walls are continuous meshes, not rows of boxes** (`TrackBuilder.Ribbon`). The old
  box-per-segment approach left **wedge-shaped gaps on the outside of corners** — boxes overlap at
  the centreline but not at the edge, and a 10 m-wide road turning 15° per segment opens a >1 m
  hole. That was the "트랙이 중간에 끊겨서 떨어진다" bug, fixed 2026-09-15. It also cut the track
  from ~500 GameObjects to ~105. `MeshCollider` on static road/wall geometry is correct; the
  no-MeshCollider rule is about karts and characters.
- Verify holes by **raycasting down along the path and along both edges**, not by driving:
  1200 rays, 0 misses. A visual check misses the one corner that is broken.
- `BoostPad` (마리오카트 대시 패널) is placed from the `BoostPads` table of `(t, lane)` — `t` is
  position around the lap, `lane` is −1..+1 across the width. Pads sit **off-centre on purpose**:
  a centred pad is free, an off-centre one costs you the racing line. Moving the `Path` table
  moves the pads with it.
- `KartController.ApplyBoost(amount, duration)` is the public way to push a kart; it keeps the
  stronger and longer of the current and new boost so consecutive pads don't cut each other off.
- The camera is **3인칭 백뷰** and always was (기획서 §1.3, `KartCamera`). If the track *looks*
  narrow that is a width/dressing question, not a camera-mode question — widths live in the
  `Path` table (currently 10–18 m).
- `TrackBuilder.Discard()` picks `Destroy`/`DestroyImmediate` so calling `Build()` from the editor
  doesn't flood the console.

## Scenes — there are three, and that is the number

`Lobby` · `Track` · `Gallery`. Registered in that order, so **F1 로비 · F2 트랙 · F3 전시실**.
Trimmed to three on 2026-09-15 at the user's request ("씬이 여러개 있어서 헷갈려요"); `Testbed`,
`MyTrack` and `SampleScene` were deleted and `MyTrackSceneBuilder.cs` with them.

- A scene is a **loading boundary, not a folder**. Things that appear together belong in one scene.
  That is why story scenes run inside `Lobby` and why the 전시실 is its own scene (you travel to it).
- `Racing → 트랙 씬 다시 만들기` now rebuilds **Track only**. Testbed moved to its own on-demand
  menu item and is not registered in Build Settings — create it to eyeball a model's scale, then
  delete the file.
- Expect a **4th scene eventually**: a title/main menu (기획서 §5). Add it at the *end* of
  `RegisterScenes` so the memorised F-keys don't shift.

## Karts

- `TestSceneBuilder.KartModels` maps castId → FBX path. **Adding a kart is one line there.**
  Every kart that exists is instantiated inside `KartVisual`; `KartSkin` enables the one matching
  `GameSelection.SelectedCastId` at Awake and rebinds `KartWheels` to that model's wheels.
  A kart is ~3.1k tris, so carrying all four costs nothing and makes mid-race swaps free.
- Before this, the model was hard-coded to 진's FBX, so **picking 세운 in the lobby still raced
  in 세진's kart**. If a kart looks wrong, check `KartSkin`, not the lobby.
- Characters without a kart fall back and log which kart they borrowed.
- **The FBX origin is at the wheel bottoms; the kart root floats at `rideHeight` (0.38 m).**
  The model must be offset down by that or the whole kart hovers. The builder reads the value off
  `KartController`, so tuning the suspension keeps the wheels on the ground.
- Unity's default for these FBXs is `materialLocation: External`, which silently renders the kart
  **all white** when no matching `.mat` assets exist. `EnsureModelImportSettings` flips it to
  `InPrefab`. The colour was never missing from the FBX.
- The user's karts already match spec exactly (전장 1.500 / 전폭 1.09 / 바닥 y=0, all quads, wheels
  and steering wheel as separate objects named `_Wheel_FL/FR/RL/RR` and `_Steering`). Keep those
  names — the builder looks them up once at build time and wires Inspector fields.

## 무인 모형 카트 — 2026-09-16 결정

The user changed the premise: **karts are unmanned toy/RC karts**, remote-driven by the four
characters from the sidelines. Reasons, in their words: a real grand prix can't plausibly be
arranged in a month after 세진's outburst, and a toy race can.

**The point of this decision is that 3D characters are no longer needed at all.**

| | before | now |
|---|---|---|
| race | 3D character sits in the kart | kart only |
| story scene | 2D portrait (§3.6) | 2D portrait — unchanged |
| lobby stand | 3D character on a pedestal | **the same 2D portrait, as a standee** |
| 3D character models needed | 4 | **0** |

The 2D portraits were already required by §3.6, so they now do double duty. With 30 days left and
zero character models started, this removes the largest remaining unknown. `DriverAnchor` stays in
the kart — if a seat ever looks empty, a head-and-shoulders silhouette can go there in code.

- §6.1 ("카트가 하반신을 가린다") no longer applies; don't cite it to ask for a seated model.
- UI direction: **no joystick** — the theme is 한옥 + 곰인형 박물관. The player's HUD reads as a
  **wooden toy remote** from the museum gift shop: 나무 판에 종이 라벨, 태엽 게이지, 안테나,
  나무 구슬 랩 카운터. This also explains the empty seat without a line of dialogue.
- The karts' bear-eared seat backs already read as "toy built by the bears" — lean on that
  rather than adding new props.

## HUD — 2026-09-16 재정비

The user's complaints were all one root cause each, and all were **measured**, not guessed.

- **`Scripts/Hud.cs` is the shared HUD layer.** Palette, fonts, 나무 판 + 종이 라벨, and the
  screen-scale transform live there; `TestHUD` and `LobbyHUD` only lay things out. Adding a
  third HUD means using `Hud`, not copying a palette.
- **All HUD coordinates are in a virtual 1280×720.** `Hud.Begin(font)` sets `GUI.matrix` to
  `Screen.height / 720` (clamped 0.8–1.8) and returns the virtual screen rect; `Hud.End()`
  restores it. Before this, OnGUI drew in raw pixels, so a panel tuned at one resolution ate
  the screen at another — that was "패널이 너무 크다".
- **Text contrast is a hard rule.** `Ink #4A3A2C` (7.7:1 on paper) and `InkSoft #7B6752`
  (5.0:1). The old `#A2907C` was **2.9:1** — under the 4.5:1 accessibility floor, which is
  why "글씨가 너무 연하다". Don't introduce a third brown for text.
- **Never draw outside `Hud.Inner(panel)`.** Text that crosses the wood border looks cut off;
  that is exactly what happened to the 태엽 label.
- **Verify overlap by measuring, not by looking.** `GUIStyle.CalcSize` on the real strings in
  the real styles, checked against each slot and against `Hud.Inner`. That check found the
  최고-시간 row sitting 3 px below the paper and the speed number 1 px above it — neither was
  visible in a screenshot. Build styles with `new GUIStyle()`, **not** `new GUIStyle(GUI.skin.label)`:
  `GUI.skin` throws outside OnGUI and the check can't run.
- Two columns beat two stacked rows. 드라이버/이름 and 현재/최고 overlapped because a 12 px
  font in a 14 px-tall rect bleeds into the row below.
- **No permanent control bar.** `H` opens a controls card in both scenes; a 쪽지 in the corner
  says so. Dev info (scene name, chapter, F7/F8) moved to the bottom-left corner — at
  top-centre it collided with the lap panel.
- `LobbyOrbitCamera.hoverLook` (7°) tilts the camera toward the cursor without any click, so the
  player discovers "이건 돌아가는구나" without being told. It is an **offset added at Apply time**,
  never added into `yaw` — accumulating it makes the lobby spin on its own.

## Materials outside the gallery — 2026-09-16

The track and campus looked "made in Unity" because **every surface was the same matte**
(`FlatMaterial.Get` hard-coded smoothness 0.08) while the gallery used seven finishes. Same
models, different lighting response, completely different read.

- **`Scripts/Surfaces.cs` holds the one `Finish` table.** Both the editor
  (`TestSceneBuilder.MaterialAsset`) and the runtime (`FlatMaterial.Get`) read it. It used to
  live nested inside `TestSceneBuilder`, which runtime code can't reach.
- `FlatMaterial.ByColor` maps palette colour → finish, so `CampusBuilder`'s hundreds of
  `Block()` calls didn't have to change. **A new material is one line in that table.**
  Current result: 석재 205 · 나무 113 · 발광 59 · 광택 1 · 무광 242.
- Runtime emission is **×1.25**, not the gallery's ×2.2 — outdoors in daylight the higher value
  blows windows to white. Lower that one number if the lanterns glare.
- The flat one-colour grass plane was the other big tell. `ScatterGroundPatches()` lays 26 wide,
  collider-free discs in two barely-different greens. **Keep the difference small** — a strong
  contrast reads as stains, a weak one reads as a field.

## Lap time

One lap is **464 m** and runs ~29 s using the boost pads. The race is **3 laps
(`RaceProgress.totalLaps`)**, so a full race is ≈ **1:27** — which is already the user's
1:29 target and inside 기획서 §1.3's 60–120 s. To change the *lap* length rather than the race
length, edit `TrackBuilder.Path`; the boost pads, kerbs and checkpoints all follow it.

### 이미 만들어진 씬에 마감 입히기

`Racing → 재질 다듬기 (지금 열린 씬)` (`MuseumLook.RefineMaterials`) swaps each `Flat_*`
material for the finished version of the same colour. **It rebuilds nothing** — hand-placed
objects survive and Ctrl+Z undoes it, which is why the Lobby got its finishes this way instead
of through `LobbySceneBuilder` (rebuilding the lobby would wipe the user's own FBX props).
It only touches materials whose name starts with `Flat_`, so an imported model's own materials
are never reassigned even when a colour happens to match. Measured on a fresh lobby:
131 무광 → 석재 53 · 나무 42 · 발광 4 · 광택 1 · 무광 31.

**Menu is now four items**, not three: 트랙/로비/전시실 씬 만들기 + 재질 다듬기. The 2026-09-15
complaint was about six confusing entries with submenus, not about the count itself.

## 지붕 — 2026-09-16, 세 씬 모두

The user asked for no sky in any scene, roofed like the track's buildings.

- **`Scripts/HanokRoof.cs` builds one roof, used by all three scenes.** It only models the
  *underside* — the player never sees the tiled outside. Layers bottom-up: 처마 띠(청기와) →
  공포 → 대들보 + 단청 → **서까래** → 반자널. The rafters are what makes it read as a hanok;
  a single ceiling slab is the most "made in Unity" thing you can build.
- Materials come in as a `Func<Color, Material>`: the Lobby/Gallery pass
  `c => TestSceneBuilder.MaterialAsset(c, FlatMaterial.FinishFor(c))` because a **saved scene
  needs real .mat assets**, while the track passes `FlatMaterial.Get` (runtime).
- **No roof piece casts a shadow.** The point is hiding the sky, not switching the lights off.
  Measured A/B at the start line: road brightness **0.295 → 0.362 with the roof** (it went *up*;
  the warm ambient helps) while the top of the frame went 0.487 → 0.260, which is the sky
  becoming a ceiling. Gallery §7.6's one shadow-casting skylight still works.
- **`Camera.clearFlags` was never set, so it defaulted to Skybox** — the Unity default sky was
  visible in all three scenes. `SetUpCamera` now uses SolidColor. Track background and
  `fogColor` are the **same value** (0.55, 0.51, 0.45); if they differ, the far wall reads as a
  band of sky.
- `CampusBuilder.BuildUpperWalls` closes the gap between the 4.2 m perimeter wall and the 26 m
  roof. Without it a strip of background shows at the horizon and the roof is pointless.
  Measured: sky-coloured pixels in the upper third of the frame = **0.0%** in all three views.
- **Roofing forced the orbit cameras down.** `maxPitch` 55° → 20° in Lobby and Gallery: at
  55° the camera climbed above the walls and would now look at the roof's back.
  The rule is `pivotY + maxDistance * sin(maxPitch) < ceiling - 0.4`. Change the ceiling and
  you must change this too.

## 로비 손보기 — 2026-09-16

The user said the lobby looked lazy, and confirmed **they have no FBX in the lobby** — the only
models they have made are the karts. So `LobbySceneBuilder` can be re-run freely; the earlier
caution about overwriting hand-placed props does not apply to Lobby.unity today. Re-check before
assuming it still holds.

`MakeCraft()` adds three things, all thin boxes, all chosen because they give the eye a ruler:
바닥 줄눈 (3 m grid), 살창 12장 (frame + 5 verticals + 3 horizontals + an **emissive 한지** behind),
주련 10개 on the columns. Blank cream walls were the biggest remaining tell.

- **`TestSceneBuilder.Cube/Capsule/Primitive` now default their finish to `FlatMaterial.FinishFor(color)`**
  instead of 무광. That was why the new 살창 came out dead (발광 0) — the generic helper flattened
  everything. An explicit `finish:` argument still wins, so the gallery's 유리/금속 are untouched.
  Result: 로비 석재 64 · 나무 220 · 발광 16, 전시실 유리 32 · 금속 56 still intact.

## 잠긴 자리는 ??? — 2026-09-16

`CharacterStand.Label` returns **"???"** when `locked` is true, so 개발업자(5번)와 시의원(6번)
are not named in the lobby. `displayName` and `castId` stay in the scene, so the nameplate colour
still identifies them and flipping `locked` reveals the name with no other change.

## 임무 — 2026-09-16

`Scripts/MissionManager.cs`. 기획서 §9.2's next script. **The point is that a race can now be
lost.** Attached by `TestSceneBuilder` to the track's `GameRig`, wired to `LapTracker` + kart.

- Three goals: **완주 / 발판전부 / 무충돌**, picked from `StoryProgress.CurrentChapter`
  (`GoalForChapter`: 1장 완주 · 2장 발판전부 · 3장~ 무충돌). One track, three ways to drive it —
  that is §4.1's "reuse the track per chapter" without new geometry.
- **Judged over the whole 3-lap race, not per lap.** Per-lap judging would force the player to
  drive two pointless laps after failing the first. 발판전부 is the exception: the pad flags reset
  at each lap crossing, so all 5 pads must be taken on **every** lap — that is what makes lap 2
  and 3 different from lap 1 instead of repetition.
- `allowedHits = 2` on 무충돌. Zero tolerance across 1:27 is brutal for a casual game; the field
  is there to tune, not a hard rule.
- `KartController.WallHits` counts **only impacts that actually cost speed** — the grazing filter
  (`into < 1.5f`) already existed, so brushing a wall is not a failure.
- `BoostPad` static helpers `CountInScene / TakenCount / ClearTaken` — the mission never hard-codes
  how many pads exist, so moving the `BoostPads` table keeps working. Only a kart with
  `PlayerKart` marks a pad taken; counting AI would let the player pass by standing still.
- Pads and the roof are **built at Awake**, so a freshly saved `Track.unity` shows 0 of them in the
  editor. That is correct. `MissionManager.Start` runs after every `Awake`, and `Update` re-counts
  if it ever sees 0.

### 발판 무한 부스트 — 고침

`BoostPad.OnTriggerStay` re-fired every `retriggerDelay` while a kart sat on a pad against a wall:
"풀린다!" never ended, the gauge sawtoothed, and the constant forward force killed steering.
Now it skips when `kart.IsBoosting` or when the kart is under 5 km/h.

### 태엽이 화면 밖으로 날아가던 것 — 고침

`GUIUtility.RotateAroundPivot` takes a pivot in **screen** coordinates, but the HUD draws in
virtual 1280×720. At 1080p (scale 1.5) the rotation centre was off by half the panel's distance
from the origin and the key swung off-screen. Multiply by `Hud.ScaleFactor`. **Any other GUI call
that takes screen coordinates needs the same multiply.**

### 메뉴는 다시 셋

`재질 다듬기` lost its `[MenuItem]`; all three builders call `MuseumLook.RefineMaterials()`
themselves before saving, so a freshly built scene is already roofed and already finished.

## 카트 제원 — 2026-09-16

`Scripts/KartSpec.cs`. Four karts, four different sets of numbers, **and no text saying which is
better** — the user's instruction: write it like a real racing game so the player infers it.

- Every kart trades something. Verified in batchmode that **no kart leads 3 of the 4 columns**;
  세진 leads 최고+가속, 시우 leads 중량+접지. If a future edit makes one kart lead three, it
  becomes the answer and the choice stops mattering — re-run that check.
- Differences are held inside **±10%** of the base (중량 13.0 · 최고 17.0 · 가속 22.0 · 접지 16.0).
  Wider means rebalancing four karts, and that time comes out of writing dialogue.
- `KartSpec.ApplyTo` is called from `KartSkin.Apply` — "this kart becomes X's kart" includes its
  numbers, not just its model.
- **Mass only changes who gets shoved in a collision.** The drive force is
  `ForceMode.Acceleration`, which ignores mass. Don't claim it does more.
- `Spec.tagline` is empty on purpose. 차 이름·소개글 wait until the character models are done
  (user, 2026-09-16) — the field is the placeholder for them.
- Shown in the lobby by `LobbyHUD.DrawSpecSheet`, for the **hovered** stand or the chosen one.
  Numbers plus a bar: numbers alone force the player to compare four karts from memory.

## 수집품 체크리스트 — 2026-09-16

The user judged that the chapter title ("제1장 사라진 관람객") doesn't tell you what to do, and
that **획득 n/8 does**. `TestHUD.DrawCollectionPanel` replaced the mission-only panel:
`수집품 n / 8`, eight tick boxes in **the same order as the gallery's eight cases**, then a thin
mission line under a rule. A ticked box is filled *and* marked, not just recoloured.

If the panel doesn't appear at all, the Track scene predates `MissionManager` — rebuild it.

## 전시실에 불이 들어온다 — 2026-09-16

`Scripts/GalleryLights.cs`. Collect all 8 and the gallery goes from dark museum to lit museum.
**This is the only place the collection visibly pays off**, which is worth more than the counter.

- Six `CeilingLights` sit at intensity 0 until then; the skylight brightens 0.85 → 1.25 and the
  Trilight ambient lifts. Fades over 3 s — a one-frame switch reads as a settings change, a slow
  rise reads as lights coming on.
- It starts dark **even when you enter already complete**, so the reveal always plays.
- `Update` tracks the count both ways, so the lobby's F9/F10 debug keys change it live.
- **It never touches materials.** The 발광 surfaces are `.mat` assets — writing to them at runtime
  edits the file on disk and leaks into every other scene.
- Ceiling lights cast no shadows; §7.6's one shadow-caster is still the skylight.

## 수집품은 주워서가 아니라 임무를 깨서 — 2026-09-16

The user's call, and it is right: driving backwards to fetch a coin fights the racing. It also
made the demo short — two items were reachable on the first run, so 8 items was four races.

**한 판 = 임무 하나 = 수집품 하나. 여덟 개니까 여덟 판.** At ~1:30 a race that is ~12 minutes of
racing, which lands inside 기획서 §1.2's 15–25 minutes *without* padding.

- `MissionManager.NextReward()` is the first uncollected entry in `ExhibitCatalogue.All` order,
  so **the catalogue order is the progression order** and nothing extra has to be saved.
- `GoalForReward` is `(Goal)(index % 4)` over 완주 / 발판전부 / 무충돌 / 제한시간, so the eight
  races are eight different races on one track — §4.1's whole idea. Item 0 lands on 완주 by
  construction; don't reorder the catalogue without checking that.
- Clearing calls `CollectionState.Collect` **at the finish line**, then advances the chapter when
  that chapter's items are all in. That is what retires the F7 manual chapter key.
- 무충돌 and 제한시간 fail **the moment they are blown**, not at the finish — driving two more laps
  knowing you failed is the worst version of this.
- `TestSceneBuilder.MakeExhibitPickups` is **commented out, not deleted** — the coin-style pickup
  still works if a scoring mode ever wants it.
- `Scripts/Toast.cs` holds the on-screen notice now. It used to live on `ExhibitPickup`, but two
  different things raise notices since the change, and the HUD should not know which.

### 발판 — 방향과 벽

- A pad now only fires when the kart is **going forwards** (`SpeedKph >= 5`) **and roughly aligned
  with the pad** (`Dot(kart.forward, pad.forward) >= 0.3`). Boost pushes along the kart's own
  forward, so taking one in reverse flung the kart backwards — a trap, not a boost.
- `KartController.CancelBoost()` fires when wall scrub hits its floor speed. Pinned against a wall
  with a boost still running, the forward force beat both reverse and steering, so the gauge
  drained and nothing moved. Cancelling the boost is what actually frees the kart; the
  `rollingFactor` floor alone was not enough.

## 제원표 문구 — 2026-09-16

"제원 / 중량 / 최고 / 가속 / 접지" read as if the character runs, and the terms meant nothing to
the user. Now the panel is titled **전용 장난감 카트** (which also settles the 무인 카트 premise on
sight) and the rows are **무게 · 빠르기 · 출발 · 코너**. What each does is spelled out in the `H`
card under 카트 항목 — still without saying which kart is better.

## 전시실 점등 — 방 전체로

The first pass lit a few lamps. The user wanted the room to read as **lights switched on**:
9 ceiling lamps (3×3, range 22) at intensity 2.6 and the Trilight ambient going to
(1.00, 0.97, 0.90) / (0.86, 0.83, 0.77) / (0.55, 0.51, 0.46). Dark state is unchanged.

## 조작 키 — SHIFT 가 태엽, SPACE 가 호핑 (2026-09-16)

They were the **same key**, and that was a real bug, not a preference: tapping it hopped, hopping
left the ground, and `IsDrifting` requires `IsGrounded` — so holding it **never wound the spring**.
`KartInput.DriftPressed` is gone; `HopPressed` (SPACE / gamepad A) and `Drift`
(SHIFT / shoulder buttons) are separate now. The drift gate also dropped from 5 m/s to 3.5 m/s —
slowing for a corner used to cancel the wind-up at exactly the moment you wanted it.

## 임무 여덟 개, 전부 다르게 — 2026-09-16

`GoalForReward` is now `(Goal)Mathf.Min(index, 7)` — **catalogue order is mission order**, one
distinct goal per exhibit:

| 판 | 임무 | 무엇을 바꾸는가 |
|---|---|---|
| 1 | 3바퀴 완주 | 아무 조건 없음 — 뭘 하는 게임인지 배우는 판 |
| 2 | 발판 전부 밟기 | 매 바퀴 초기화. 레이싱 라인을 포기하게 만든다 |
| 3 | 벽에 안 부딪히기 | 벽이 처음으로 무서워진다 |
| 4 | 105초 안에 완주 | 안전하게 도는 걸 못 하게 한다 |
| 5 | 태엽 6번 터뜨리기 | 드리프트를 처음으로 강제한다 |
| 6 | 발판 밟지 않고 완주 | 2판의 정반대 — 같은 코스가 다시 새로워진다 |
| 7 | 한 바퀴 33초 끊기 | 세 바퀴 중 한 바퀴만 잘하면 된다 |
| 8 | 무충돌 + 시간 | 마지막 판. 앞의 조건 둘을 동시에 |

- Reordering `ExhibitCatalogue.All` reorders the missions. Item 0 must stay the one that should be
  taught first.
- The **"AI 카트보다 먼저 들어오기"** mission is deliberately absent — there is no AI driver script
  in the project yet (`grep` for one before assuming). It becomes the ninth goal when AI lands.
- `KartController.DriftBoosts` counts spring releases, reset with `ResetWallHits`.

### 실패했으면 그 자리에서 다시

`MissionManager.FailReason` carries *why*, and `TestHUD.DrawFailed` puts it in the middle of the
screen with `ENTER 이 판 다시 하기`. A corner panel reading "실패" was invisible — the user reported
the instant-fail "doesn't show", and that was the reason.

**Retry replays only the current race. Everything already collected stays.** Resetting the whole
collection on a failed mission would make failure cost twenty minutes, and the user would stop
taking risks — which is the exact opposite of what missions are for.

## 곰인형 NPC — 2026-09-16

The user generated a teddy bear with an AI 3D tool and wants it walking the museum complaining
that the toilet is blocked. Right now it is **placed but stationary** — movement only.

### 모델 손보기 (Blender headless)

The raw FBX was one mesh with **1764 duplicate vertices (30%)** and **3254 open edges**. Merging
at 0.0005 fixed the cracks (3254 → 28) and collapsed what looked like 186 shells into 4 — those
"shells" were the duplicate seams, not body parts. So splitting into parts was never possible;
bones were the only route.

- **Bone positions come from a measured width profile, not by eye.** Half-width drops
  0.377 → 0.297 at z 0.60–0.65 — that is the neck (59% of height). It widens again at
  z 0.50–0.55, which is where the arms stick out. Five bones: Root · Body · Head · Arm_L · Arm_R.
- Blender's automatic (bone-heat) weights worked. 63 leftover unweighted vertices were assigned
  to the nearest bone by region — unweighted vertices stay put while the bone moves and spike.
- **Arm weights needed sharpening and the centre-line needed clearing.** Bone heat smeared arm
  influence into the belly, so waving dragged the torso.
- **"각져 보인다" was polygon count, not shading.** Rendering at 35° and at 180° auto-smooth looked
  identical; the eyes and nose are simply low-poly. One Catmull-Clark level
  (**4,507 → 17,345 quads**) rounds them out. Two levels is ~70k and too much for an NPC.
  **Cost: ~34.7k tris each, so keep to about four on screen** against §7.6's 150–250k budget.
- Still wrong in the source and *not* fixable by cleanup: the muzzle sits left of the eye
  midline, and a faint facet remains in the eye highlights.
- **Verify a rig by posing it and measuring**, not by looking: rotate each bone and check that the
  intended region moves while the rest does not. Head 20° → head 0.145 m / feet 0.000 m.
  Two harness bugs bit here — `to_mesh()` must be cleared between evaluations or the second call
  returns `inf`, and `v.co` is a **live reference**, so the rest pose must be captured with
  `.copy()` or the comparison baseline moves with the mesh.

### 유니티 쪽

- `Scripts/BearNpc.cs` — 숨쉬기 · 고개 갸웃 · 팔 흔들기 · 가까이 오면 쳐다보기.
  **No AnimationClip**; it rotates bone transforms, same as the kart's lean (CLAUDE.md rule 4).
- **Axes were measured in Unity**, not guessed: head local **X = 끄덕 · Y = 좌우 · Z = 갸웃**;
  arm local **X = 위아래**, and local Y does nothing because it runs along the bone.
  Blender's `Arm_L` lands on Unity's **−X** side after the axis conversion — names only.
- Inspector fields win; if they are empty `AutoBind()` fills them by name once and says so. That
  is a deliberate exception to the no-`Find`-by-name rule, because the point is that a freshly
  dragged FBX moves immediately.
- `LobbySceneBuilder.MakeBears()` places three along the north wall (3.9 m clear of the character
  stands) and **finds the FBX by looking for a skinned model in `Assets/NPC_bear`** rather than by
  filename — the user renames it between passes. Newest file wins. `EnsureBearImport` flips
  `materialLocation` to InPrefab (External renders it pure white, same trap as the karts) and
  turns animation import off.
- `noticeRange` is **13 m** in the lobby, not the 5 m default: the orbit camera passes at 5–25 m,
  so 5 m would never trigger and the look-at would appear broken.

### 곰이 하얗게 나오던 것 — 고침 (2026-09-16)

`materialLocation = InPrefab` 만으로는 부족했다. **FBX 안에 박힌(embedded) 텍스처는 저절로
에셋이 되지 않는다** — `_BaseMap` 이 비고 `_BaseColor` 가 순백색이라 곰이 하얗게 나왔다.
`LobbySceneBuilder.ExtractBearTextures` 가 `ModelImporter.ExtractTextures` 로
`Assets/NPC_bear/Textures` 에 꺼내고, 꺼낸 뒤 **normal 이 이름에 든 텍스처는 타입을 NormalMap 으로**
바꾼다(안 그러면 파랗게 칠해진다). 이미 붙어 있으면 아무 것도 안 한다.

크기는 모델이 1.0 m 라 7 m 홀에서 인형처럼 작았다 → `BearScale = 1.7`.

**순찰 반경은 받침대 간격에서 나온다.** 곰~받침대 최소 거리가 3.93 m 라 기본 4.5 m 로 두면
캐릭터 고르는 자리를 침범한다. 3 m 로 잡아 0.93 m 를 남겼다. 곰 위치나 받침대를 옮기면
이 숫자를 다시 재라.

## ★ 임시 — 정비 곰 시나리오 (2026-09-16)

유저가 **"임시로"** 넣어 달라고 한 것이고, **지워 달라고 하면 같이 걷어내기로 약속했다.
유저가 "까먹을 수 있으니 나중에 물어봐 달라"고 했으니 먼저 물을 것.**

걷어낼 때 같이 지울 것:

| 파일 / 위치 | 무엇 |
|---|---|
| `Scripts/BearLines.cs` | 대사 표 (화장실 농담 10 + 5줄) |
| `Scripts/BearNpc.cs` 의 "순찰 + 혼잣말" 블록 | 순찰·말하기. 숨쉬기/갸웃/팔/쳐다보기는 남긴다 |
| `Scripts/LobbyHUD.DrawToast` | 로비 알림 줄 |
| `LobbySceneBuilder.MakeBears` 호출 | 로비 배치 |

등급 주의: 화장실 농담이라 전체 이용가 기준에서 애매할 수 있다고 유저도 말했다.
대사를 `BearLines.cs` 한 군데에 모아둔 이유가 그것 — 심의 때 이 파일만 갈아끼우면 된다.

## 직선을 만든 방법 — 2026-09-16

전에는 **제일 긴 직선이 10 m** 였고 25 m 넘는 직선이 하나도 없었다. 464 m 내내 꺾이기만 하니
최고 속도를 쓸 일도, 추월할 자리도, 리듬도 없었다.

**캣멀-롬 곡선은 점 두 개를 나란히 놔서는 직선이 안 나온다.** 한 점의 접선이 *양옆 점*으로
정해지기 때문이다. 네 점(13·0·1·2)을 같은 z 에 놓아야 가운데 구간이 곧게 뻗는다. 그래서
조절점을 12개 → **14개**로 늘리고 남쪽 변을 z = −78 로 맞췄다.

측정: **제일 긴 직선 57 m · 25 m 넘는 직선 2개** (전에는 10 m / 0개).
한 바퀴는 464 → **511 m**. 랩타임이 바뀌므로 `MissionManager` 의 시간 기준을 다시 재야 한다.

- 코스를 바꾸면 **건물과 겹치는지 반드시 재라.** 코스 중앙과 양 끝 240 × 3 지점이 다른 물건의
  바운즈 안에 드는지 검사했고 0개였다. 서편 조절점을 x −80 에서 −68 로 당긴 게 그 검사 때문이야.
- 발판 자리(t)도 같이 다시 골랐다. **직선 한가운데 발판은 그냥 지나가다 먹으니 재미가 없다** —
  직선 끝 브레이킹 존 직전에 두면 "밟고 들어갈래 말래" 가 생긴다.

### 코스 장식 — 기획서 §4.4 를 이제야 지켰다

§4.4 는 **악당 광고를 금색 + 자홍색으로 일부러 부딪히게** 하라고 적어뒀는데 트랙에 하나도
없었다. `BuildAdBoards` 가 길 바깥에 6개를 세운다. 색은 `Cast` 의 개발업자(#C9A227)·
시의원(#B0407F) 색을 그대로 쓴다 — 악당이 입은 색을 광고판이 똑같이 입고 있으면 나중에
이야기에서 둘을 연결할 때 설명이 필요 없다.

`BuildFinishArch` 는 결승선 위 한옥 아치(청기와는 로비·전시실 지붕과 같은 재료)와 직선 양옆
현수막 16장. **결승선이 바닥 무늬뿐이면 지나갔는지도 모른다** — 머리 위로 뭔가 지나가야
한 바퀴가 끝난 게 느껴진다.

## 결승 직선을 늘리려면 점이 다섯 개 (2026-09-16)

네 점(13·0·1·2)을 나란히 놓으면 **가운데 한 구간만** 곧다 — 32 m. 다섯 점(12·13·0·1·2)이어야
두 구간이 이어져 결승선 양쪽이 다 직선이 된다. 측정: **제일 긴 직선 63 m, 그게 결승선을 품은
직선**이다 (첫 시도에서는 57 m 짜리가 동쪽에 생기고 결승 쪽은 25 m 대였다).
한 바퀴 464 → 511 → **535 m**.

**코스 길이가 바뀌면 `MissionManager` 의 시간 기준을 같이 옮겨라.** 464 → 535 m 는 15% 이고,
난이도를 유지하려고 비례로 옮겼다: 제한시간 105 → **121**, 빠른랩 33 → **38**, 완벽 115 → **133**.
이건 계산값이고 실제 랩타임을 재면 다시 조여야 한다.

## 뭐가 벽이고 뭐가 장식인지 — 경고 띠 (2026-09-16)

유저: *"부딪힐 때마다 랜덤으로 부딪힘 갯수가 깎여나가니까"*. 원인은 물리가 아니라 **읽기**였다.
벽 색을 구간마다 나무·돌담·이끼로 다르게 칠해놔서 **벽이 풍경처럼 보였다.**

`BuildWarningStripe` 가 벽 꼭대기 0.2 m 에 빨강·크림 띠를 두른다. 규칙은 하나:
**띠가 있으면 부딪히는 것, 없으면 장식.** 광고판·나무·석등·가드레일 기둥은 전부
`noCollider: true` 라 띠도 없다. 새 물건을 놓을 때 이 규칙을 깨지 마 — 콜라이더가 있는데
띠가 없으면 플레이어는 또 "랜덤으로 깎인다" 고 느낀다.

## 화면에 뜨는 말은 RaceVoice 에 (2026-09-16)

판정 코드가 문장을 만들면 **"벽에 3번 부딪혔다 / 무충돌로 시간 안에"** 같은 로봇 말투가 된다.
상태를 그대로 읽어주는 건 정보지 말이 아니야. `Scripts/RaceVoice.cs` 로 전부 옮겼고,
말투는 이 게임 것 — 다크코미디, 약간 시비 거는 투. 실패 사유는 여러 줄 중 하나가 무작위로 뜬다.

- **임무 이름은 여덟 글자를 넘기지 마.** HUD 칸이 좁아서 "무충돌로 시간 안에" 가 라벨을 파고들어
  **"무충룰"** 로 보였다. 지금은 "무사고 + 시간".
- 값을 라벨 <b>옆</b>에 두지 말고 <b>아래 줄 통째로</b> 놓는다. 상품·임무·진행 셋 다 그렇게 바꿨고,
  그래야 이름이 길어져도 절대 안 부딪힌다. 패널도 188 → 208 로 넓혔다.
- 대사는 원래 유저가 쓴다. RaceVoice 에 있는 건 말투 견본이야.

### 문구는 담백하게 (2026-09-16, 되돌림)

시비 거는 말투로 한 번 써봤다가 유저가 **담백한 쪽으로 되돌렸다**. HUD 는 레이스 중에 힐끗
보는 거라 농담이 끼면 읽는 데 시간이 걸린다 — 맞는 판단이야. 이야기의 말투(다크코미디)는
`StoryScript` 쪽에서 살린다. **HUD 는 계기판이지 대사가 아니다.**

`망했다` → `임무 실패` · `벽이 3번 이겼다` → `벽 3번 부딪힘` · `ENTER — 다시. 모은 건 그대로 둔다`
→ `ENTER`. 그래도 문장은 `RaceVoice` 에만 둔다 — 판정 코드가 문장을 만들기 시작하면 다시
로봇 말투가 된다.

**임무 이름 길이 제한은 없어졌다.** 임무 칸에 `wordWrap` 을 켜서 두 줄까지 접힌다
(`부딪힘 없이 시간제한에 맞춰 도착하기` = 2줄, 측정 33px). 세 줄을 넘기면 잘리니 거기까지.
값은 라벨 옆이 아니라 **아래 줄 통째로** — 상품·임무·진행 셋 다.

### ESC — 게임을 끄지 않고 로비로

빌드한 게임에서 ESC 가 곧장 종료되면 잘못 눌렀을 때 되돌릴 수 없다. **로비로 보낸다.**
한 번 누르면 물어보고, 한 번 더 눌러야 나간다 — 잘 달리던 판을 실수로 날리지 않게.
다른 키를 누르면 취소된다. `SceneNavigator` 의 F1~F6 은 에디터 전용이라 이건 따로 만들었다.

모은 수집품은 PlayerPrefs 라 나가도 안 날아가고, **진행 중이던 판만 처음부터**다
(상품은 결승선을 넘어야 준다).

## AI 카트 — 2026-09-16, 만들어만 둔 상태

**유저 요청: 초반부터 AI 와 겨루게 하지 말 것. 지금은 달리기만 하고 임무에는 안 걸려 있다.
나중에 "AI 보다 먼저 들어오기" 를 붙일 때 유저에게 먼저 알릴 것.**

- `Scripts/KartAi.cs` — 길찾기도 NavMesh 도 없다. `TrackBuilder.PointOnPath` 가 이미 있어서
  **"내가 코스 어디쯤인지 찾고, 조금 앞을 보고 꺾는다"** 가 전부야. 코스 모양을 바꿔도 안 고친다.
- **플레이어와 같은 `KartController` 를 쓴다.** `acceptPlayerInput = false` 로 키보드만 끊고
  `Drive(throttle, steer, drift)` 로 몬다. 물리·드리프트·서스펜션이 같은 코드라 AI 가 사람이
  못 하는 움직임을 하지 않고, 카트를 손보면 AI 도 따라온다.
- `acceptPlayerInput` 은 **빌드할 때도 꺼서 저장**한다. 에디터에서는 Awake 가 안 도니까,
  안 그러면 씬만 봐서는 AI 인지 사람 카트인지 알 수가 없다.
- 실력을 0.80 / 0.845 / 0.89 로 조금씩 다르게 준다. 셋이 같으면 한 덩어리로 붙어다녀서
  레이스로 안 보인다.
- 출발선에 **뒤로 물려서** 세운다(3.2 m + 3.4 m 씩). 나란히 세우면 출발하자마자 서로 밀친다.
  측정: 제일 가까운 두 대 3.99 m, 카트 전장 1.9 m.
- AI 카트에서는 `PlayerKart` 를 떼어낸다 — 이야기 수집품을 AI 가 주워가면 진행이 막힌다.
- 벽에 붙어 1.2 초 못 움직이면 후진해서 뺀다. 없으면 한 대가 영영 거기 있는다.
- 끄려면 `TestSceneBuilder.AiRacers = 0`.

이제 순위판이 의미가 생겼다 — 전에는 "1위 / 1대 중" 이었다.

## 벽 부딪힘이 랜덤으로 세지던 것 — 2026-09-16

두 가지가 겹쳐 있었다.

1. **내가 만든 경고 띠에 콜라이더가 붙어 있었다.** `TrackBuilder.Ribbon` 의 `collider` 기본값이
   `true` 라, 띠 76조각이 전부 벽 면에 붙은 콜라이더가 됐다. 벽을 한 번 긁으면 벽 조각과 띠 조각이
   **각각** 세져서 "벽 2번" 이어야 할 게 **"벽 4번"** 으로 떴다. → `collider: false`.
   **장식용 리본을 추가할 때마다 이걸 확인해.**
2. **벽은 한 덩어리가 아니라 구간별 리본 여러 조각이다.** 벽을 따라 쭉 긁으면 조각을 넘을 때마다
   `OnCollisionEnter` 가 또 온다. `KartController.WallHitCooldown`(0.7초) 안에 들어온 건 같은
   접촉으로 친다.

## 로비 곰 자리 — 좌표로 찍지 말 것 (2026-09-16)

손으로 좌표를 적었다가 **두 번 다 틀렸다.** 처음엔 곰 조각상 (0, −11.5) 안에, 고친 뒤엔
석등 (±13, −8) 위에 올라갔다. 홀에 뭐가 있는지 외워서 피하는 건 안 되는 방법이야.

`FindOpenSpots` 가 바닥에 1.5 m 격자를 깔고 **제일 널널한 칸**을 고른다. 나중에 홀에 뭘 더 놓아도
곰이 알아서 비켜선다. 측정: 세 마리 모두 가장 가까운 물건까지 5 m 이상, 곰끼리 6.7 m.

- **콜라이더가 아니라 Renderer 바운즈로 잰다.** 석등 같은 장식은 콜라이더가 없어서 콜라이더만
  보면 "비어 있다" 고 나오고, 곰이 그 위에 선다. 막히는 것만 피하면 되는 게 아니라 **겹쳐 보이지도
  않아야** 한다.
- `MakeBears()` 는 **빌드 순서 맨 뒤**다. 앞에서 부르면 받침대·출발문이 아직 없어서 그 자리를
  비었다고 본다.
- 런타임에도 `BearNpc.PickSpot` 이 갈 자리를 미리 확인하고, 0.8 초 못 움직이면 처음 자리로 돌아간다.

## 말 걸기는 버튼으로 (2026-09-16)

가까이 갔다고 저절로 떠들면 지나갈 때마다 말이 튀어나와 금방 시끄러워진다. 동물의 숲처럼
**표시만 띄우고 `E` 를 눌러야** 말한다. 표시는 `BearNpc.Nearest` — **제일 가까운 한 마리에게만**
뜬다. 셋이 몰려 있을 때 누구한테 거는지 헷갈리면 안 되니까.

## 빨간 띠가 회색으로 번쩍거리던 것 — z-파이팅 (2026-09-17)

유저: *"빨간줄이 회색처럼 삐까번쩍 색이 바뀌어서 깨진 것처럼 연출되고 있어."*

**원인은 재질도 조명도 아니라 깊이값이다.** 두 면이 같은 평면에 있으면 GPU 는 어느 쪽이 앞인지
정할 방법이 없고, 카메라가 1cm만 움직여도 반올림 결과가 뒤집혀 픽셀마다 두 색이 번갈아 찍힌다.

경고 띠를 `outer[i]` 에서 위로 세웠는데 벽도 **같은 `outer[i]`** 에서 위로 세운 거였다. 간격 0.

- 고친 방법: 띠를 **코스 쪽으로 30mm** 밀어낸다. 미는 방향이 점마다 다르니(코너에서 벽이 기울어
  있다) `Ribbon` 의 고정 오프셋으로는 안 되고 **점을 새로 구해야** 한다.
- 30mm 로 잡은 근거: 24비트 깊이 버퍼에서 거리 z 의 분해능은 대략 `z² × 2e-7` 이다.
  이 트랙에서 제일 먼 벽이 ~130m 라 분해능이 ~3.4mm — 30mm 면 열 배 여유가 있다.
- 카트를 뚫지 않는다: 카트 콜라이더 윗면이 **0.545m**, 띠는 **0.65~0.86m** 라 아예 위에 있다.
  (카트 그림은 전폭 1.09 인데 콜라이더가 1.0 이라 원래도 벽에 4.5cm 파묻힌다.)

### 같은 병이 두 군데 더 있었다

`Editor/_ZFight.cs`(미러 프로젝트) 로 트랙 356조각을 훑어서 **같은 평면 + 자리 겹침**을 찾았다.

| 자리 | 무엇이 겹쳤나 | 고친 방법 |
|---|---|---|
| 골든베어 광고판 | 자홍색 띠와 금색 판의 **밑면**이 같은 높이(2.6m) | 띠를 5cm 위로 |
| 결승선 체커 | 바깥 칸의 **옆면**이 바탕의 옆면과 같은 평면 | 칸을 3% 줄임 — 줄눈도 생겼다 |

**바탕을 넓히는 쪽으로 고치면 안 된다.** 한 번 그렇게 했더니 바탕 끝면이 이번엔 **벽 면**과
0.2mm 로 붙었다. 겹침은 밀어내는 게 아니라 **작게 만드는** 쪽으로 푼다.

가짜 양성이 많으니 스캔 결과를 그대로 믿지 마라. 맞닿아 있을 뿐 넓이로 겹치지 않거나
(체커 칸끼리), 같은 색이거나(발판 화살표 두 개), 아래를 향해 안 보이는 면(기둥 밑면)은 괜찮다.
**서로 다른 색 + 보이는 방향 + 넓이로 겹침**, 셋이 다 맞을 때만 진짜다.

리본이나 판을 새로 얹을 때는 **밑에 있는 것과 좌표를 똑같이 주지 마라.**

## AI 카트가 전부 플레이어 카트로 나오던 것 — 2026-09-17

유저: *"정이감을 선택하면 진·운·시우 카트가 나와야 하는데 정이감 카트만 4개 나와."*

두 군데가 겹쳐 있었다.

1. **`KartSkin.Awake` 가 AI 카트에서도 `GameSelection.SelectedCastId` 를 읽었다.** 빌더가
   `fallbackCastId` 를 넣어줬지만 fallback 은 *못 찾았을 때만* 쓰인다 — 플레이어 캐릭터는
   멀쩡히 찾아지니까 넷 다 그 카트로 갈아입었다.
2. **AI 배정이 씬 구울 때 정해져 있었다.** `MakeAiKarts(track, GameSelection.SelectedCastId)`.
   **캐릭터는 씬을 구운 뒤에 로비에서 고른다** — 구울 때 정하면 박제된다.

`KartSkin.aiSlot` 으로 바꿨다. -1 이면 플레이어(고른 캐릭터를 따라감), 0 이상이면
**고른 캐릭터를 뺀 나머지 중 그 순번**을 런타임에 고른다. 빌더는 순번만 심는다.

**씬을 구울 때 런타임 선택에 의존하는 값을 읽지 마라.** 이 프로젝트에서 그런 값은
`GameSelection`(고른 캐릭터)과 `CollectionState`(모은 것) 둘이다.

검증: 네 캐릭터를 각각 골랐을 때 출발선에 **서로 다른 카트 4종**이 서는지 전부 확인
(`Editor/_AiCast.cs`, 미러 프로젝트).

## 캐릭터는 노마드 스컬프로 — 2026-09-17

앞서 "블렌더 박스 모델링" 을 권했는데 **뒤집는다.** 새로 안 사실 둘 때문이야:

- 유저는 **블렌더로 모델링할 줄 모른다.** 노마드도 잘 모르지만 그쪽이 빠르다고 판단했다.
- 유저가 **리토폴로지·UV·리깅을 나한테 맡기겠다**고 했다.

그러면 곰 때 나온 "34.7k tris" 는 이 루트의 운명이 아니다. 곰은 각져 보여서 **섭디비전으로
면을 4배 늘린** 결과였고, 이번엔 **Quadriflow 리토폴로지로 면 수를 내가 정한다**(4~6k quad 목표).
블렌더 헤드리스로 다 된다: `quadriflow_remesh` · `smart_project` · 곰 때 쓴 중복정점 정리.

- **모든 캐릭터가 걸어 다닌다**(유저 요청) → 다리 애니메이션이 필요하고, 그건 코드로 못 한다
  → **믹사모를 쓴다.** 갈래 A(서 있는 NPC, 믹사모 없음)는 폐기.
- 믹사모는 메시를 합치고 셰이프키를 버리니 **표정은 셰이프키로 못 한다.** 대신 눈·눈썹·입을
  **따로 만들어** 두고, 믹사모가 리깅한 몸에 내가 머리 본으로 다시 붙인다.
- **동작 클립은 캐릭터마다 받을 필요가 없다.** 유니티 Humanoid 리타게팅이라 한 번 받은 클립이
  넷에 다 돌아간다. 비용은 클립이 아니라 Animator 배선이고 그건 내 몫.

유저가 노마드에서 지켜야 할 것은 **T자 자세**와 **팔다리를 몸에서 떼는 것** 둘뿐이다.
자세는 리그가 생긴 뒤에야 바꿀 수 있어서 자동으로 못 고쳐준다.

## 부술 수 있는 광고판 — 2026-09-17

강사님 피드백: **게임이 재미없다, 정치인을 때리는 것 같은 기믹이 있어야 한다.** 맞는 지적이다.
원인은 레이싱이 아니라 **임무가 이야기와 아무 상관이 없었던 것**이다. 여덟 개가 전부
발판 밟기·안 긁기·시간 안에였고, 그 중 어느 것도 박물관이나 철거와 연결되지 않았다.
그래서 진지하지도 병맛도 아니었다.

`Scripts/AdBoard.cs` + `TrackBuilder.BuildAdSigns`. 골든베어 입간판 8개를 코스 **안쪽 갓길**에
세우고 들이받으면 부서진다. 임무 `Goal.광고판` 이 **`빠른랩` 을 대체**했다 — 제한시간과
빠른랩이 사실상 같은 요구라 하나를 이야기 쪽으로 돌린 것.

- **큰 광고판(`BuildAdBoards`)은 길 바깥이라 닿지 않는다.** 벽 너머 3.2m 라 부술 수가 없어서
  작은 입간판을 안쪽에 따로 세웠다. 큰 것은 그대로 배경.
- **트리거지 콜라이더가 아니다.** 콜라이더면 벽 부딪힘으로 세지고 카트가 튕긴다 —
  판자를 뚫고 가는 느낌이어야 한다. 그래서 경고 띠도 없다. 규칙이 셋이 됐다:
  **띠 있으면 부딪히는 것 · 띠 없고 트리거면 부수는 것 · 나머지는 장식.**
- 자리는 발판과 같은 사고방식 — **갓길이라 레이싱 라인을 포기해야 닿는다.** 좌우로 번갈아 둬서
  다 부수려면 지그재그로 돈다. 측정: 벽까지 0.64~1.11m, 가장 가까운 발판까지 20m 이상.
- **한 번 부수면 그 판 내내 부서져 있다.** 바퀴마다 되살리면 발판전부와 똑같아진다.
- **AI 는 못 부순다.** AI 가 부수면 플레이어의 표적이 사라진다.
- 조각 다섯 개가 튄다. 그냥 사라지면 "내가 부쉈다" 가 아니라 "못 보고 지나쳤다" 로 읽힌다.
- 정적 목록은 **`BoostPad` 와 같은 방식(`FindObjectsByType` 캐시)**. `OnEnable` 로 모으면
  에디터에서 Awake 가 안 돌아 검사가 0개로 나온다.

## 속도감 — 2026-09-17

유저: *"레이스가 너무 재미없어."* 시야각(60→76)은 **이미 있었다** — 없다고 말했던 건 내 착각.
빠진 건 그 외 전부였다. 화면이 가만히 있으면 22m/s 도 8m/s 처럼 보인다.

- `KartCamera` — 노면 진동(펄린 노이즈, **속도의 제곱**에 비례해 느릴 땐 거의 안 떤다),
  부스트 떨림, 벽 충돌 충격. **흔들림은 목표 위치가 아니라 맨 마지막에 더한다** —
  목표에 섞으면 부드럽게 만드는 lerp 가 떨림을 먹어버린다.
- 빠를수록 카메라가 **0.85m 뒤로, 0.22m 아래로.** 카트가 작아지면서 달아나 보이고
  시점이 낮아 지면이 빨리 흐른다.
- `Scripts/SpeedRush.cs` — 최고속 **55% 부터** 렌즈 왜곡 + 색수차. 천천히 갈 때도 화면이 휘면
  멀미가 나고 박물관 구경이 망가진다. **런타임 프로필**이라 `MuseumLook.asset` 을 안 건드린다
  (디스크 파일을 고치면 다른 씬까지 따라간다). 세기는 `volume.weight` 로만 —
  0 이면 효과가 아예 안 돌아 공짜다.
- `KartCamera` 가 없으면 스스로 붙인다. **씬을 다시 굽지 않아도 들어오게** 하려고.

## 문 — 세 씬 모두, 2026-09-17

유저: *"다들 문이 없는데 어떻게 들어가고 나간 거야."* 맞는 지적이다. 박물관인데 벽이 통짜라
방이 서로 이어져 보이지 않았다. 별관에는 갈색 판자 한 장이 있었는데 그건 문으로 안 읽힌다.

`Scripts/HanokDoor.cs` — `HanokRoof` 와 같은 방식으로 세 씬이 같이 쓴다.
**문을 문으로 보이게 하는 건 판이 아니라 틀이다.** 문틀·상인방·문지방이 있어야 "저기가
뚫려 있다" 가 되고, 판자 한 장은 벽에 칠한 자국으로 보인다. 한지 패널은 안에 불이 켜져 있다는
신호라 그것만으로 "들어갈 수 있는 곳" 이 된다.

- **좌표는 부모 기준**(`localPosition`). 건물이 yaw 로 돌아가 있어도 "정면 한가운데" 를 그대로 적는다.
- **콜라이더 없음.** 벽과 건물 몸통이 이미 막고 있다.
- 상인방 위 **현판**은 건물 이름을 달 자리 — 아직 비어 있다.
- 로비 2개(남쪽=밖, 동쪽=전시실. 서쪽은 방송 화면이 있어 비움), 전시실 1개, 트랙 6개
  (별관 5 + 본관). **본관 문은 6.4×6.2 로 크게** — 별관과 같으면 큰 건물이 작아 보인다.
- `BuildMainHall` 은 `Hanok()` 을 부른다. 본관에 문을 따로 더하면 **두 겹이 된다.**

## 진동은 사건일 때만 — 2026-09-17

유저: *"달릴 때마다 지진 온 것 같다. 뭐 부딪히거나 해야 진동이 실감 난다."* 맞다.
늘 떨고 있으면 그건 진동이 아니라 **화면 상태**가 되고, 정작 부딪혔을 때 아무 차이가 없다.

`KartCamera.shakeAtTopSpeed` 기본값을 **0** 으로 내렸다(주행 진동 없음). 남은 건 둘뿐:
**부스트가 터지는 순간(0.035)** 과 **벽에 부딪힌 순간(0.30)**. 속도감은 시야각 · 물러나기 ·
`SpeedRush` 가 맡는다 — 그것들은 화면을 흔들지 않는다.

## 캠퍼스 건물 목록 — 이름 붙이기 전 상태

지도와 미니게임 이야기를 하려면 지금 뭐가 있는지부터. 전부 `CampusBuilder.Build()` 안에 있다.

| 이름(임시) | 자리 (x, z) | 크기 (폭×깊이×높이) | 지금 성격 |
|---|---|---|---|
| MainHall | (0, −100) | 36 × 22 × 13 | 곰 본관. 박공에 곰 얼굴 + 빨간 리본, 계단 |
| Gate | (0, 100) | — | 한옥 정문 |
| TicketBooth | (−26, −88) | — | 매표소 |
| Annex_W1 | (−94, −22) | 22 × 14 × 9 | 이름 없음 |
| Annex_W2 | (−90, 42) | 18 × 12 × 8 | 이름 없음 |
| Annex_NE | (74, 80) | 20 × 13 × 9 | 이름 없음 |
| Annex_E1 | (96, −12) | 21 × 13 × 9 | 이름 없음 |
| Annex_SE | (82, −78) | 17 × 12 × 8 | 이름 없음 |

트랙 구간(`TrackBuilder.Zone`)은 여섯: 본관앞 · 서편전시동 · 북서담장 · 정문앞 · 동편연못 · 매표소굽이.
**구간 이름과 건물 이름이 지금 따로 논다** — 별관에 이름을 붙일 때 같이 맞출 것.

## 현판 글씨와 이야기 연출 — 2026-09-17

### 글씨는 TextMesh 로. TMP 는 쓰지 마라

`TMP_FontAsset.CreateFontAsset` 은 **TMP 기본 리소스(TMP Settings)가 임포트돼 있어야** 돌고,
이 프로젝트에는 없다. 배치모드에서 `TMP_Settings.get_clearDynamicDataOnBuild` 가 NullReference 로
터진다. 현판 글씨 하나 때문에 패키지 설정을 건드릴 일이 아니다.

`BuildingSign` 은 옛날 **`TextMesh`** 를 쓰고 폰트는 이미 있는 **`Resources/HudFont.ttf`** 다.
새로 넣는 에셋이 0개. 가까이서 조금 흐릿한데 달리면서 스쳐 보는 간판이라 문제가 안 된다.

- 글자는 현판의 **자식이 아니라 형제**로 단다. 현판은 납작하게 눌린 상자라 자식은 같이 눌린다.
- `fontSize 64` 로 크게 굽고 `characterSize` 로 줄인다. 작게 구우면 계단이 보인다.

### 캠퍼스가 이야기에 반응한다

`Scripts/CampusMood.cs`. 유저 요청: *"다 깨기 전과 다 깨고 미니게임 들어갈 때 연출이 달라져야 한다."*

**수집품 하나마다 폐과 딱지가 하나씩 떨어진다.** 마지막에 한 번 확 바뀌는 것보다 낫다 —
여덟 판을 도는 동안 화면이 안 변하면 이긴다는 게 뭔지 알 수가 없고, 다음 판을 돌 이유가 없다.

| 수집품 | 폐과 딱지 | 빛 |
|---|---|---|
| 0 / 8 | 12동 전부 | 서늘하고 탁함 (0.52, 0.52, 0.52) |
| 4 / 8 | 6동 | 중간 |
| 8 / 8 | 0동 | 따뜻함 (0.60, 0.55, 0.46) |

- **웅지관(행정)에는 딱지가 절대 안 붙는다.** 행정동은 없어지지 않는다 — 그게 농담이야.
- **기하를 하나도 안 만들고 안 부순다.** 이미 선 현판의 딱지와 조명 색만. 그래서 공짜고
  씬을 다시 구울 필요도 없다.
- 딱지는 글씨를 **가리지 않고 위에 덧붙인다.** 원래 뭐였는지가 보여야 없어지는 게 아프다.
- 건물 목록은 **이름 순으로 정렬**해서 딱지가 매번 같은 순서로 떨어진다. 안 그러면 다시 켤 때마다
  다른 건물이 살아나서 "내가 저길 살렸다" 가 안 남는다.
- **어둡게 하지 않는다.** 어두우면 안 보일 뿐 슬프지 않다. 차가운 쪽으로만 민다
  (유저가 어둡다고 한 적이 있다 — CLAUDE.md 의 "Do not grade hard" 참고).
- 배경색은 **안개와 같은 값**으로 같이 옮긴다. 다르면 먼 벽이 하늘 띠처럼 보인다.

### 미러 프로젝트가 PlayerPrefs 를 공유하고 있었다

`Verify` 의 companyName/productName 이 실제 프로젝트와 **같아서 레지스트리 키가 같았다.**
검사에서 `CollectionState.ClearAll()` 을 부르면 **유저의 실제 수집 기록이 날아간다.**
`milksystudy-verify / Racing Verify` 로 바꿔 놨다. **미러를 새로 만들면 이것부터 바꿀 것.**

## 카트끼리 부딪히기 — 2026-09-17

유저: *"부딪혔을 때 못 빠져나오고 서로 밀어주는 형태가 된다. 탑블레이드 팽이처럼 부딪혀야 재밌잖아."*

원인은 **카트끼리의 충돌이 `ScrubOnWall` 을 그대로 탔던 것**이다. 벽 취급이라
매 프레임 속도가 지워지는데 양쪽이 서로를 향해 구동력을 넣고 있으니 밀기 싸움이 됐고,
**벽 부딪힘 횟수에도 잘못 세졌다.**

- `OnCollisionEnter/Stay` 가 상대 리지드바디에 `KartController` 가 있는지 먼저 본다.
  있으면 `Bump()`, 벽 처리는 아예 안 탄다.
- 물리 엔진에 맡기면 안 튕긴다 — 서스펜션이 매 프레임 속도를 다시 쓴다. 그래서 충돌 순간에
  **`ForceMode.VelocityChange` 로 속도를 직접** 바꾼다.
- **무게가 여기서 처음으로 의미를 가진다.** 구동력이 `ForceMode.Acceleration` 이라 질량을
  무시해서 제원표의 중량이 사실상 장식이었다. 이제 `2 × 상대질량 / (내질량 + 상대질량)` 배로 튄다 —
  세진(11.7)이 시우(14.2)를 받으면 **세진 ×1.10 · 시우 ×0.90**.
- 조향이 `rb.MoveRotation` 이라 **토크가 안 먹는다.** 팽이처럼 도는 건 `spinRate` 를 두고
  조향 계산의 yaw 에 더한다.
- 양쪽이 각자 `Bump()` 를 돌려 반대로 튄다. 한쪽만 계산하면 누가 먼저 충돌을 받았느냐로 결과가 달라진다.
- 받히면 `CancelBoost()`. 안 그러면 밀려나면서도 앞으로 간다.

## AI 카트가 또 겹쳤다 — fallback 이 범인 (2026-09-17)

세진을 고르면 **세진 카트가 두 대**, 이감은 없었다. `AiCastId` 가 skins 를 훑으며 순번을 세다가
**모자라면 `fallbackCastId`("세진")로 떨어졌기** 때문이다. 이감 카트를 못 찾는 씬(카트 FBX 보다
오래된 씬)에서는 쓸 수 있는 카트가 둘뿐이라 세 번째 AI 가 fallback 으로 갔다.

이제 **먼저 모아 놓고 고른다.** 모자라면 `pool[slot % pool.Count]` 로 AI 끼리 겹치게 하고
경고를 띄운다 — **플레이어 카트로는 절대 안 떨어진다.** 씬이 오래됐다는 것도 경고가 알려준다.

## 카트 김 (배기) — 2026-09-17

`Scripts/KartExhaust.cs`. 유저 아이디어. 레이싱에서 이게 하는 일은 둘이다:
**속도가 눈에 보이고**(자국이 길수록 빠르다), **순위가 눈에 보인다**(앞차 색만 봐도 누군지 안다).
카트 넷이 뒤에서 보면 거의 똑같이 생겼거든.

- 무인 모형 카트라 배기가스는 설정에 안 맞는다 — **태엽 장난감에서 나는 김**으로 친다.
  그래서 색이 캐릭터 색이어도 이상하지 않고 부스트 때 확 뿜는 것도 설명이 된다.
- `simulationSpace = World` — 카트를 따라다니면 자국이 안 남는다.
- **알파는 0.55 까지.** 진하면 뒷차 시야를 가려서 불친절해진다.
- 프리팹 없이 코드로. `KartSkin.Apply` 가 색을 넣어주고, 씬에 저장될 게 없다.

## 임무 이름을 이야기로 — 2026-09-17 (임시)

조건만 적으면("발판 다 밟기") 조작 설명이 되고 박물관과 아무 상관이 없어진다.
`RaceVoice.Title` 을 실사(철거 심사) 쪽으로 바꾸고 **`RaceVoice.Why`** 를 새로 뒀다 —
HUD 임무 칸 아래 11px 로 *왜* 하는지가 한 줄 붙는다.

| 임무 | 이름 | 왜 |
|---|---|---|
| 완주 | 캠퍼스 세 바퀴 돌기 | 아직 운영 중이라는 걸 보여야 한다 |
| 발판전부 | 순찰 등 전부 켜기 | 밤에 불 꺼진 캠퍼스는 폐가로 찍힌다 |
| 무충돌 | 담장 긁지 않기 | 담장 흠집도 철거 사유로 적힌다 |
| 제한시간 | n초 안에 실사 끝내기 | 실사단이 오래 머물수록 트집이 늘어난다 |
| 태엽 | 태엽 n번 감기 | 태엽 소리가 나야 아직 돌아가는 곳이다 |
| 무발판 | 발판 밟지 않고 조용히 | 전기 쓴 기록이 남으면 예산 낭비로 잡힌다 |
| 광고판 | 골든베어 광고 철거 | 리조트 광고가 먼저 와서 서 있다 |
| 완벽 | 담장 안 긁고 시간 안에 실사 끝내기 | 마지막 실사다 |

**임시 문구다.** 이야기 말투는 유저 것이니 `StoryScript` 쪽과 같이 다듬을 것.
칸 높이는 `CalcHeight` 로 쟀다 — 임무 이름 40px · 왜 34px 안에 여덟 개 전부 들어간다
(완벽이 38px 로 제일 길다. 더 늘리면 넘친다).
