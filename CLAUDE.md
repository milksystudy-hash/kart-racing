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
