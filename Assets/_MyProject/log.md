# Development Log

## [2026-07-08] Setup Prototype Scene and Sprites
- Created `Assets/_MyProject/Scripts/Editor/SceneSetup.cs` which:
  - Programmatically generates `CatCircle.png` and `WallSquare.png` sprite textures.
  - Generates the `PrototypeScene.unity` scene with URP 2D camera settings.
  - Dynamically configures the Game View resolution to 1080x1920 (9:16 portrait) via reflection.
  - Adds the walls, floor, and cat circle aligned to the 9:16 orthographic screen bounds.
  - Exposes `Tools -> MyProject -> Create Prototype Scene` menu option in the editor.
- Registered the scene in the project's build settings.

## [2026-07-08] Scale Cat and Apply Gravity
- Updated `PrototypeScene.unity` and `SceneSetup.cs` to set the Cat (Circle) scale to `0.5` (half size).
- Configured Rigidbody2D on the Cat to explicitly be Dynamic with gravity scale `1.0`.

## [2026-07-08] Add Jump Player Script
- Created `PlayerController.cs` runtime component to handle mouse clicks and tap inputs for upward jumps.
- Updated `SceneSetup.cs` to automatically attach the `PlayerController` component to the Cat GameObject during scene building.

## [2026-07-08] Fix Input Handling Package Error
- Patched `PlayerController.cs` to use the modern Unity Input System (`Pointer.current`) instead of the legacy `UnityEngine.Input` class, resolving the `InvalidOperationException` thrown due to project active input handling configuration.

## [2026-07-08] Add Grounded Jump Requirement
- Modified `PlayerController.cs` to set `allowInfiniteJumps` to `false` by default.
- Implemented a robust automated ground check using `Collider2D.Cast` directly from the Cat's `CircleCollider2D` downward by `0.05` units, filtering out the Cat's own collider.
- Modified `PrototypeScene.unity` directly to set the serialized `allowInfiniteJumps` value to `0` (false) on the Cat's `PlayerController` component, ensuring the grounded requirement works immediately in the editor.

- Added debug scene gizmos to visualize ground detection status (Green when grounded, Red when in air).

## [2026-07-10] Implement Horizontal Movement
- Added horizontal movement logic to `PlayerController.cs`.
- Listens to left/right arrow keys using the modern Unity Input System (`Keyboard.current.leftArrowKey` / `rightArrowKey`).
- Exposed a configurable `moveSpeed` field (default `5.0f`) in the Inspector to control speed.

## [2026-07-10] Add Wall Climbing and Remove Friction
- Programmatically created and assigned a frictionless `PhysicsMaterial2D` (friction = 0) to the Cat's `CircleCollider2D` to prevent sticky friction when sliding or moving against walls.
- Implemented left/right wall detection using `Collider2D.Cast`.
- Implemented steady vertical wall climbing at `climbSpeed` (default `3.0f`) when the screen pointer/mouse is held down (`Pointer.current.press.isPressed`) while touching a wall.

## [2026-07-10] Set up Cinemachine and Camera Target Follower
- Created `CameraFollowTarget.cs` component to track the Cat's highest Y position. X and Z are locked to 0, and the Y coordinate only increases (never goes back down).
- Updated `SceneSetup.cs` to integrate Cinemachine 3.1.7 using compilation-safe reflection.
- Automatically adds `CinemachineBrain` to the Main Camera.
- Spawns a `CinemachineCamera` set up to follow the `CameraTarget` GameObject with orthographic size 5, using a `CinemachinePositionComposer` component for smooth tracking.

## [2026-07-10] Fix Cinemachine Lens Reflection Zoom-out
- Corrected reflection lookup in `SceneSetup.cs` to check for public `Lens` fields first (which are used in Cinemachine v3) to properly assign the orthographic size of `5.0f`. This resolves the zoomed-out layout caused by defaulting to `10.0f` size.

## [2026-07-10] Implement Platform Manager and Object Pooling
- Created `PlatformManager.cs` utilizing an object pooling pattern to manage and recycle 20 platforms.
- Configured platforms programmatically as **One-Way Platforms** using `PlatformEffector2D` and `BoxCollider2D.usedByEffector` so the player can jump through them upwards and land on top.
- Implemented camera-height tracking to recycle platforms when they fall below the camera's bottom viewport limit, randomly repositioning them above the highest active platform and within the horizontal wall boundaries (X in range `[-2.0, 2.0]`).
- Updated `SceneSetup.cs` to automatically instantiate the `PlatformManager` in the prototype scene.

## [2026-07-10] Adjust Cat Starting Position
- Changed initial Y position of the Cat to `-4.55` in both `PrototypeScene.unity` and `SceneSetup.cs`, placing the cat exactly on the surface of the Floor at startup.

## [2026-07-10] Fix Camera Target Initial Position
- Updated `CameraFollowTarget.cs` to lock the initial camera target Y-coordinate to `0.0f` at startup, ensuring the Floor remains locked at the bottom edge of the screen. Camera scrolling now begins only when the cat climbs above `Y = 0.0f`.

## [2026-07-10] Implement Infinite Wall Scrolling
- Created `WallManager.cs` to manage infinite vertical walls via a segment-pooling loop.
- Automatically creates 3 segments (each `10` units tall, total `30` units) for both the Left and Right walls using clones of the editor-generated wall GameObjects.
- Implemented camera-height tracking to recycle lower wall segments when they fall below `Camera.main.y - 15.0f` (off-screen), repositioning them directly above the highest active segment.
- Updated `SceneSetup.cs` to automatically instantiate the `WallManager` in the scene.

## [2026-07-10] Change Platforms to Standard Solid Colliders
- Removed `PlatformEffector2D` and disabled `usedByEffector` on `BoxCollider2D` in `PlatformManager.cs` to turn platforms into solid physical blocks that prevent the cat from passing through from all directions.

## [2026-07-10] Implement Enemy Triangle Spawning
- Added programmatic generation of a white 2D triangle sprite asset (`EnemyTriangle.png`) in `SceneSetup.cs`.
- Pre-spawned exactly 1 inactive child `Enemy` GameObject (represented by a red triangle tinted `#FF5252` and sized to match the Cat at `(0.5f, 0.5f)`) on each of the 20 platforms.
- Corrected child enemy local scale relative to parent platform scale to prevent physical/visual stretching.
- Attached a `PolygonCollider2D` (configured with `isTrigger = true` for damage overlap checks) to the Enemy GameObject.
- Implemented a 10% (1/10) probability check on startup and platform recycle using `Random.value <= 0.1f` to dynamically enable or disable the pooled Enemy GameObject.

## [2026-07-10] Fix Enemy Placement & Randomize X
- Changed the local Y coordinate of the Enemy child on the platforms to `1.75f` (world Y = `0.35`), ensuring its bottom edge sits exactly on top of the platform's surface instead of clipping through.
- Randomized the Enemy's local X coordinate dynamically between `[-0.29f, 0.29f]` (world X range `[-0.35f, 0.35f]`) on initialization and recycle. This ensures the enemy spawns at different spots along the platform instead of sharing the exact same displacement as the platform center.

## [2026-07-10] Implement Melee Attack Mechanic
- Created [Enemy.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Enemy.cs) containing a public `TakeDamage()` method that logs a defeat message and disables the enemy (recycling it).
- Attached `Enemy` script component to the pooled platform enemies, and assigned their layer to `"Enemy"`.
- Programmatically added `"Enemy"` layer index creation in `SceneSetup.cs` so it is automatically configured in Unity project settings.
- Added horizontal facing direction tracking (`facingDirectionX`) in `PlayerController.cs`.
- Integrated `MeleeAttack()` in `PlayerController.cs` triggered when clicking/tapping in mid-air (`!isGrounded` and `!allowInfiniteJumps`).
- Used `Physics2D.OverlapCircleAll` within a `0.6` unit radius offset by `0.5` units in front of the cat, filtering with `LayerMask.GetMask("Enemy")`. When hit, it calls `enemy.TakeDamage()`.
- Added yellow wire sphere visualization in `OnDrawGizmos()` showing the active melee attack range in front of the cat.

## [2026-07-10] Implement Wall Spikes Spawning & Pooling
- Updated `WallManager.cs` to pool and manage Wall Spikes.
- Pre-spawned exactly 2 spikes per wall segment (children of `WallManager` to bypass non-uniform wall scaling distortion).
- Rotated spikes outward: Left wall spikes point right (`-90` deg), Right wall spikes point left (`90` deg).
- Spikes are represented by the `EnemyTriangle` sprite tinted hazard dark red (`#BF4040`), attached with a trigger `PolygonCollider2D` and the `Enemy` script component on the `"Enemy"` physics layer.
- Randomized Y-coordinates in separate halves of each recycled segment (`Random.Range(-4.0f, -1.0f)` and `Random.Range(1.0f, 4.0f)` relative to the segment center) and applied a 50% activation probability roll to generate organic random hazard intervals.
- Updated `SceneSetup.cs` to assign the triangle sprite reference to `WallManager.spikeSprite`.

## [2026-07-10] Decrease Platform Vertical Spacing
- Reduced default platform spacing settings in `PlatformManager.cs`: `minVerticalSpacing` to `1.2f` and `maxVerticalSpacing` to `1.9f`. This spans platforms closer together, smoothing out vertical jumping progression.

## [2026-07-10] Make Spikes Solid and Indestructible
- Modified `WallManager.cs` to remove the `Enemy` script component from the wall spikes, making them indestructible by the player's swipe attacks.
- Set `isTrigger` to `false` on the spikes' `PolygonCollider2D`, making them solid physics barriers that physically block the cat's movement.

## [2026-07-10] Implement Platform Size & Shape Randomization
- Added dynamic platform size/shape randomization in `PlatformManager.cs`. Each platform selects from: Square `(0.8f, 0.8f, 1f)`, Vertically Long Rectangle `(0.5f, 1.5f, 1f)`, or Horizontally Long Rectangle `(1.5f, 0.3f, 1f)`.
- Implemented dynamic boundary calculations based on platform scale width to prevent any platform from clipping or spawning inside the left/right walls.
- Formulated adaptive local scaling (`0.5f / parentScale`) and positioning (`0.5f + (0.25f / parentScale.y)` local Y, and dynamic local X bounds) for the child Enemy GameObject, ensuring it sits flush on top of any platform shape without physical stretching or mid-air floating.

## [2026-07-11] Implement Game Over on Off-Screen Cat, Hazard Damage, & Score UI
- Created [GameManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/GameManager.cs) script to track game state, check when the cat is below the visible screen viewport, freeze the player controller, and handle keyboard/pointer tap scene restarts.
- Implemented vertical progress height tracking to calculate a real-time player score (10 points per unit of height reached) that only increases.
- Added a translucent dark capsule Score UI bubble at the top-left of the screen (`OnGUI`) showing the score in theme-matching teal with an increased font size (`24`) and expanded dimensions.
- Implemented `OnCollisionEnter2D` and `OnTriggerEnter2D` on the player cat in [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) to detect contact with hazards (wall spikes and platform enemies) on the `"Enemy"` layer, immediately triggering game over.
- Implemented a clean, premium, responsive screen overlay via `OnGUI` that displays the final score alongside restart options.
- Modified [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to automatically instantiate the GameManager GameObject in the prototype scene on regeneration.

## [2026-07-15] Generate and Integrate Pixel Cat Sprite
- Generated a cute 16-bit style pixel art cat character sprite using the AI image generation tool.
- Wrote a custom Python script `make_transparent.py` utilizing Pillow's flood fill algorithm to remove the solid white background and export it as a clean transparent PNG (`pixel_cat.png`).
- Resized the sprite to 128x128 pixels using nearest-neighbor interpolation to match existing sprite sizes and preserve sharp pixel boundaries.
- Modified [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to auto-detect, import, and configure `pixel_cat.png` as a Point-filtered (crisp) Sprite asset.
- Integrated the new pixel cat sprite as the player character, adjusting its SpriteRenderer color to white to display its full color.
- Generated a second front-facing sitting cat sprite (`pixel_cat_idle.png`) for the staying-still idle state, processed it to have a transparent background, and resized it to 128x128 pixels.
- Updated [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) to expose `walkingSprite` and `idleSprite` fields, caching the `SpriteRenderer` component on startup.
- Implemented `LateUpdate()` visual logic in [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) that:
  - Swaps to `idleSprite` when the cat is grounded and staying still (no horizontal input).
  - Swaps back to `walkingSprite` when moving or in mid-air.
  - Automatically flips the sprite (`flipX = true`) when walking or facing right, and clears the flip (`flipX = false`) when going left.
- Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to import and configure the idle sprite, and assign both sprite references to the `PlayerController` component during scene generation.
- Restructured the `Cat` GameObject in [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to separate physics/logic (parent GameObject with scale `1.0` and `0.25` radius collider) from rendering (child `Visual` GameObject with base scale `0.5`). This prevents cosmetic visual scaling from distorting physics colliders.
- Implemented a clean, frame-by-frame visual state transition in [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) using coroutines when transitioning between states (`Idle` <-> `GoingLeft` / `GoingRight`):
  - Removed all squash and stretch scale manipulation ("popping effects") to keep the sprite size perfectly constant at local scale `0.5`.
  - Immediately switches to the diagonal `transitionSprite` (with correct horizontal flip) for a brief, cached delay of `0.08` seconds (`transitionDelay`) before swapping to the final target sprite (`walkingSprite` or `idleSprite`), creating a smooth and consistent 2D retro transition.
- Refactored [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to parameterize scene generation via a unified `CreateScene(string sceneName, bool usePixelCat)` method.
- Added two separate editor menu items:
  - **Create Prototype Scene**: Generates `PrototypeScene.unity` with the original pink circle cat and no visual sprite transitions (the circle is mapped to all state sprites for compatibility).
  - **Create Gameplay Scene**: Generates `GameplayScene.unity` with the current exact settings, using the new pixel cat sprites (walking, idle, transition) and visual state transitions.
- Dynamically registers both scenes in the project's Build Settings.
- Generated three new pixel art cat sprites to complete the movement states:
  - **pixel_cat_jump.png**: Leaping/jumping cat, side view (facing left). Automatically flipped horizontally for jumping right.
  - **pixel_cat_jump_up.png**: Cat jumping straight up (vertical jump, facing front/up).
  - **pixel_cat_climb.png**: Cat climbing/clinging to a vertical wall on the left (facing left). Processed using a custom Python filter to erase the guide wall line from the generated asset and keep the paws intact. Automatically flipped horizontally for climbing the right wall.
- Updated [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) fields and `LateUpdate()` visual state machine to support the new visual states: `VerticalJump`, `JumpingLeft`, `JumpingRight`, `ClimbingLeft`, and `ClimbingRight`.
- Added left/right wall checking variables (`isTouchingLeftWall`, `isTouchingRightWall`) to `FixedUpdate()` in [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) to correctly determine wall clinging directions.
- Modified [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to import, configure, and assign the new jump, vertical jump, and climbing sprites to the `PlayerController` component during `GameplayScene` generation.
- Generated second and third frames for walking and climbing to enable smooth looping movement cycles:
  - **pixel_cat_walk_2.png**: Second walk pose, side view (facing left).
  - **pixel_cat_walk_3.png**: Intermediate walk pose showing the front-left leg moving forward and the back-right leg moving backward (diagonal pairing).
  - **pixel_cat_climb_2.png**: Alternate vertical climbing pose (facing left). Guide wall line erased via Python script.
- Exposed `walkingSprite2`, `walkingSprite3`, `climbSprite2`, and `frameDuration` settings in [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs).
- Implemented a 4-step walk cycle mapping (Frame 1 Stride A -> Frame 3 Intermediate -> Frame 2 Stride B -> Frame 3 Intermediate -> Repeat) and a 2-frame climbing cycle toggler in [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs)'s `LateUpdate()` based on `frameDuration` (default `0.15s` per frame), ensuring both pairs of legs alternate correctly and look natural in each direction.

## [2026-07-18] Revise Cat Walk Cycle Animation
- Extracted four distinct walk cycle frames from the user-provided reference sheet.
- Programmatically processed each cropped frame: removed the white background using an edge-based flood-fill algorithm (preserving internal white body markings), resized to 84x84, and pasted onto a transparent 128x128 canvas with a vertical offset aligning the paws at baseline y = 103 to prevent animation jittering.
- Saved processed sprites as `pixel_cat.png`, `pixel_cat_walk_2.png`, `pixel_cat_walk_3.png`, and `pixel_cat_walk_4.png` in `Assets/_MyProject/Sprites/`.
- Updated [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) to expose `walkingSprite4` and revised the walk cycle animation loop in `GetWalkingSpriteForFrame` to map the 4 sequential frames directly: CONTACT -> PASSING -> CONTACT (opposite) -> PASSING (opposite).
- Modified [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to import, filter, and assign the new walk cycle sprite `pixel_cat_walk_4.png` dynamically during gameplay scene generation.
- Recompiled scripts and successfully regenerated `GameplayScene.unity` using the custom Editor script.
- Processed a new, clean version of the walk cycle reference image (without text labels at the bottom) to overwrite the walk sprites and ensure any non-cat artifacts are completely trimmed.
- Fixed a sprite cropping issue in `pixel_cat_walk_2.png` where a slight horizontal crop overlap captured the tail of the first frame's cat. Resolved by cropping exact, mathematically disjoint horizontal boundaries for all 4 frames and centering them on intermediate canvases before scaling, ensuring zero bleed between frames.
- Fixed a Unity texture importer issue where existing `.meta` files for character sprites had `spriteMode: 2` (Multiple) with custom old slice rectangles, causing new sprites to be cut and trimmed incorrectly in the editor. Enforced `spriteMode: 1` (Single) in the C# texture settings inside [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) and programmatically cleaned up all `.meta` files on disk to reset them to Single sprites, successfully restoring the full, uncut paws and bodies of all character walk frames.
- Fixed a temporary Unity import warning where the `.meta` files were marked as having invalid GUIDs due to Unity's file watcher trying to read them while they were half-written. Touched all `.meta` files to trigger a clean file watcher re-import and deleted the unused `new_raw_walk.png.meta` file.
- Corrected the horizontal crop range for Cat 2 from `[256, 498]` to `[303, 498]` to eliminate the tail of the first cat that bled into the front nose area of the second frame. Now all frames use exact, mathematically disjoint boundaries for zero bleed.

## [2026-07-19] Implement 4-Frame Cat Climbing Animation
- Extracted 4 climbing animation frames from the user-provided sprite sheet (`media__1784457115665.png`).
- Programmatically processed each frame using Pillow: performed background removal (edge-based flood fill with threshold 30), resized using Nearest-Neighbor to match the walking cat scale factor (~0.316), and placed them onto transparent `128x128` canvases.
- Aligned all 4 frames to prevent visual jitter: set the front claws/paws (clinging side) exactly at `x = 22` and aligned the bottom edge of the cat's bounding box exactly at `y = 117` (exclusive).
- Updated [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs):
  - Declared `climbSprite3` and `climbSprite4` fields.
  - Expanded the climbing animation index loop from 2 frames to 4 frames.
  - Integrated `GetClimbingSpriteForFrame` helper to cycle through the 4 frames.
- Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to configure `pixel_cat_climb_3.png` and `pixel_cat_climb_4.png` as point-filtered Single sprites, and automatically bind them to the player controller during scene generation.

## [2026-07-20] Bypass State Transitions for Air/Jump/Climb States
- Added `ShouldAnimateTransition` helper in [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) to restrict state transitions that use `transitionSprite` (which had a `0.08` seconds delay) only to grounded walking and idle states (`Idle`, `GoingLeft`, `GoingRight`).
- Modified `TriggerStateTransition` to immediately swap sprite states (`ApplySpriteState`) without delay and stop any running transition coroutines when entering jump or climb states. This ensures the cat immediately changes to the jump sprite when clicking to jump from the idle state.

## [2026-07-20] Implement Cat Jump and Landing Transitions
- Sliced and processed 6 animation frames from the user's `jumping_sheet.png` (using edge-based flood-fill to preserve internal white details, nearest-neighbor resizing, and placement on 128x128 canvases aligned at baseline y = 115).
- Created `pixel_cat_crouch.png`, `pixel_cat_crouch_deep.png`, `pixel_cat_launch.png`, `pixel_cat_rise_1.png`, and `pixel_cat_rise_2.png`.
- Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to import, configure, and assign the new transition sprites to the player controller.
- Refactored [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs):
  - Declared fields for the new transition sprites.
  - Implemented `JumpingTransition` state machine: plays grounded jump anticipation (`Crouch 1` -> `Crouch 2` -> physical force -> `Launch` -> `Rise 1` -> `Rise 2`) with a fast, responsive delay (0.05s per frame).
  - Implemented `LandingTransition` state machine: detects when the cat lands from mid-air and plays cushion animations (`Crouch 2` -> `Crouch 1`) before returning to idle/walk.
  - Ensured immediate cancellation of transitions in favor of immediate input actions (e.g. wall climbing, consecutive jumps).

## [2026-07-20] Implement Walk-to-Jump Transition Animation
- Extracted crouch (pre-jump squat), takeoff (launch), and mid-air jump (flying pose) frames from the user-provided sprite sheet (`media__1784477214107.png`).
- Programmatically processed each frame using Pillow (background removal, scaled by 0.582 to match baseline walking cat sizes, and centered on transparent 128x128 canvases with paw baseline aligned at y=104).
- Overwrote the horizontal `pixel_cat_jump.png` sprite and created `pixel_cat_crouch.png` and `pixel_cat_takeoff.png` in `Assets/_MyProject/Sprites/`.
- Updated [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs):
  - Declared `crouchSprite` and `takeoffSprite` fields.
  - Declared `walkToJumpDelay = new WaitForSeconds(0.07f)` variable.
  - Updated `ShouldAnimateTransition` to trigger state transitions when transitioning from `GoingLeft` -> `JumpingLeft` or `GoingRight` -> `JumpingRight`.
  - Updated `AnimateTransition` coroutine to sequence crouch (0.07s) and takeoff (0.07s) sprites before applying the final sustained jump state.
  - Added horizontal-flip helper methods `ApplyCrouchSpriteState` and `ApplyTakeoffSpriteState`.
- Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to automatically configure import settings and assign the new transition sprites to the `PlayerController` component during scene generation.

## [2026-07-20] Fix Vertical Jump/Landing Direction & Sprite Glitch
- Declared `launchSprite` field in [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) for vertical takeoff/launch, resolving the mismatch between horizontal takeoff and vertical launch.
- Updated `AnimateJumpTransition` to determine if a jump is vertical (`Mathf.Approximately(horizontalInput, 0f)`) and capture the jump direction on trigger. If vertical, it forces front-facing sprites (`crouchDeepSprite`, `launchSprite`, `rise1Sprite`, `rise2Sprite`) and disables sprite flipping (`flipX = false`); if horizontal, it correctly flips and plays side-facing sprites (`crouchSprite`, `takeoffSprite`, `jumpSprite`).
- Updated `AnimateLandingTransition` to accept a `bool isVertical` parameter to play front-facing cushioning (`crouchDeepSprite` -> `idleSprite`) for vertical jumps and side-facing recovery for horizontal jumps.
- Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to import and map `pixel_cat_takeoff.png` to `takeoffSprite` and `pixel_cat_launch.png` to `launchSprite`.

## [2026-07-20] Implement 4-Frame Cat Attacking Animation
- Extracted four attack animation frames from the user-provided reference sheet.
- Programmatically processed each frame using Pillow: removed white background (edge-based flood fill with threshold 40), flipped horizontally (to face left by default to match movement flip settings), scaled by 0.45 to match walking cat sizes, and positioned them on transparent 128x128 canvases.
- Saved processed sprites as `pixel_cat_attack_1.png` to `pixel_cat_attack_4.png` in `Assets/_MyProject/Sprites/`.
- Updated [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs):
  - Declared `attackSprite1` through `attackSprite4` fields.
  - Added `Attacking` state to the `CatState` enum.
  - Implemented `AnimateAttack` coroutine that cycles through the 4 frames (0.05s per frame) and applies correct horizontal flip state.
  - Updated `MeleeAttack` to trigger `AnimateAttack` coroutine in mid-air.
  - Updated `LateUpdate` to return early if `attackCoroutine` is active, and immediately stop `attackCoroutine` when wall climbing, jumping, or landing.
- Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to import, configure, and assign the attack sprites to `PlayerController` during scene generation.
- Modified [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to automatically check and regenerate `GameplayScene.unity` on compile/load when missing, and deleted the old scene file to force automatic regeneration of sprite references.
- Fixed a critical Unity TextureImporter issue where the newly created attack sprites were imported as Multiple sprites and automatically sliced into sub-sprites (e.g. `pixel_cat_attack_1_0`), causing only leg/claw segments to load. Solved by writing clean, single-sprite `.meta` templates for all 4 attack sprites while preserving their unique GUIDs to keep inspector links intact.
- Corrected cropping boundaries for the attack sprites to prevent visual artifacts: restricted Frame 1 and 2 right boundaries to `255` and `511` to cut off bleed-in from the subsequent frames, and extended the Frame 3 left boundary to `494` to preserve the cat's full tail. Overwrote the sprites and automatically re-imported them.
- Refined crop regions using a connected component seed-fill algorithm to extract the exact shape of each cat, eliminating horizontal overlap and segment bleed-in. Adjusted scale factor to `0.41` to prevent any side-clipping of the wide slash-swipe effect on the `128x128` canvas, aligning all bottoms to `y=104`.
## [2026-07-20] Implement Custom Wall Spike Sprite
- Generated a high-quality, pixel-art style, sharp metal wall spike sprite pointing straight up with a warning red tip accent.
- Programmatically processed the generated spike image to remove the checkered background (making it fully transparent) and cropped it tightly to its bounding box (286x837 pixels), saving it as `WallSpike.png`.
- Created a standard Unity single-sprite `.meta` file (`WallSpike.png.meta`) with a fixed GUID (`33b62c665e3d14abab4baf2ba9c794ce`).
- Updated `GameplayScene.unity` and `PrototypeScene.unity` scenes to assign the new `WallSpike` sprite to the `WallManager`'s `spikeSprite` property.
- Updated `SceneSetup.cs` to import the new `WallSpike.png` sprite and assign it to the `WallManager` during programmatic scene generation.

## [2026-07-20] Generate and Integrate Ground Sprite
- Generated a beautiful 16-bit pixel art ground tile sprite (`GroundSprite.png`) featuring green grass on top and brown dirt below using the AI image generator.
- Processed the sprite with Python (Pillow) to crop it exactly to its black borders (`902x262`) and remove the white background to make it transparent.
- Updated `SceneSetup.cs` to import the new `GroundSprite.png` asset, configure it as a point-filtered, horizontally tileable sprite, and assign it to the Floor GameObject's SpriteRenderer.
- Set the Floor's SpriteRenderer color to `Color.white` to draw the sprite with its full generated colors instead of dark grey-blue tinting.
- Updated `PlatformManager.cs` to load the new `GroundSprite.png` sprite dynamically for footholds (platforms) and set their SpriteRenderer color to `Color.white`.
- Deleted old scene files to trigger automatic regeneration of the prototype and gameplay scenes.


## [2026-07-20] Fix SceneSetup Compilation Errors
- Fixed compilation error CS0128 in [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) by removing duplicate local variable declarations (`string`) for `pixelWallPath` and `groundSpritePath`. Both variables are now correctly reused from their definitions in the outer scope of the method.

## [2026-07-20] Fix Ground Sprite Scale and Cat Burial
- Updated `process_ground.py` to resize the cropped ground sprite from `902x262` to a 1:1 `128x128` canvas using nearest-neighbor scaling. This ensures its aspect ratio and size match `WallSquare.png` exactly, correcting all platform scaling and foothold collider calculations.
- Modified floor creation in `SceneSetup.cs` to align with the new `128x128` sprite scale: adjusted floor position to `Y = -5.44f` and increased tiled size height to `1.28f` when the ground sprite is loaded, keeping the grass surface flush at the cat's spawn point (`Y = -4.8f`) while maintaining fallback logic for the prototype scene.

## [2026-07-20] Fix Player/Cat Visibility and Floor Collider Size Mismatch
- Fixed player visibility and falling-off-screen bug by configuring explicit Sprite sorting layers across the codebase: Cat/Player is set to `sortingOrder = 10` (always on top), Spikes and Enemies are set to `sortingOrder = 5`, Platforms are set to `sortingOrder = 2`, and Walls and Floor are set to `sortingOrder = 1`.
- Resolved collider size mismatches in [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) by explicitly setting the `size` properties of the `BoxCollider2D` components for LeftWall, RightWall, and Floor to match their respective `SpriteRenderer` tiled size properties. This prevents the Player from immediately falling through or walking off the narrow default collider bounds and triggering off-screen Game Over.
- Deleted the old scene files to trigger automatic, clean scene regeneration upon compile.

## [2026-07-20] Fix GameView NullReferenceException on Play Mode Entry
- Guarded `SetResolution` in [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) from running when Unity is compiling, updating the asset database, or preparing to enter Play Mode (`EditorApplication.isPlayingOrWillChangePlaymode`). This prevents GUI lifecycle conflicts and crashes (NullReferenceExceptions in `GameView.OnGUI` and `HostView`) that happen when code tries to open/manipulate EditorWindow layout states during Play Mode transitions.

## [2026-07-20] Adjust Wall Spike Dimensions
- Resized the custom wall spike sprite (`WallSpike.png`) to a 1:1 `128x128` ratio using nearest-neighbor scaling. This matches the standard 1:1 ratio used in the scene generation, making the spike shorter and protrude by exactly `0.5` units (one cat diameter) into the climbing lane instead of blocking 30% of the screen.
- Updated `WallSpike.png.meta` to reflect the new `128x128` texture dimensions.
- Deleted old scene files to trigger clean scene regeneration.

## [2026-07-20] Defer Scene Generation and Restore Active Scene
- Deferred automatic scene regeneration in `SceneSetup.cs` to the next editor frame using `EditorApplication.delayCall` instead of running directly during the domain reload event (`InitializeOnLoadMethod`). This ensures that scene creation runs only when the editor domain is fully stable, avoiding memory leaks and scene object warnings.
- Added logic in `SceneSetup.cs` to capture the path of the active scene before triggering auto-generation and re-open it afterwards. This keeps the user in their active scene and prevents newly generated scenes from leaking objects into global editor memory.

## [2026-07-20] Fix Sprite Tiling and GameView Reflection Warnings
- Updated `SetAsSprite` in [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to automatically configure `spriteMeshType` to `SpriteMeshType.FullRect` on all imported sprites using `TextureImporter.SetTextureSettings`. This resolves the Unity editor warning about tiled SpriteRenderers potentially not displaying correctly when using tight meshes.
- Silenced non-critical reflection warnings in `SetResolution` by changing `Debug.LogWarning` logs to `Debug.Log`. This prevents harmless GameViewSizes reflection failures from cluttering the Unity console in Unity 6+.

## [2026-07-20] Replace Enemy Sprite with Custom Mouse Sprite
- Generated a high-quality, pixel-art style mouse sprite (`EnemyMouse.png`) using the AI image generator.
- Processed the sprite with Python (Pillow) to remove the solid white background and make it transparent, and cropped it tightly to its bounding box.
- Renamed the existing `EnemyTriangle.png` and its `.meta` file to `EnemyMouse.png` and `EnemyMouse.png.meta` to match the new character design while maintaining its GUID.
- Modified [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to reference `EnemyMouse.png` (using `enemyPath` variable) instead of `EnemyTriangle.png`.
- Updated [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs) to set the enemy visual renderer's color to `Color.white` so it renders the mouse sprite's actual texture colors instead of tinting it red-coral.
- Deleted the old scene files to trigger automatic, clean scene regeneration upon the next compile or loading of the project.

## [2026-07-20] Resize Enemy Mouse Sprite and Adjust Scale
- Resized the raw cropped mouse sprite (`EnemyMouse.png`) to fit exactly on a `128x128` transparent canvas (matching the cat's sprite canvas size and resolution) using Nearest-Neighbor scaling.
- Set the default `enemyScale` configuration in [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs) to `0.5f` (matching the cat's visual scale). This ensures they render at the exact same screen size and pixel grid density.
- Modified [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to import `EnemyMouse.png` with `FilterMode.Point` so that the pixel art remains crisp in Unity.
- Deleted the old scene files to trigger automatic, clean scene regeneration upon the next compile or loading of the project.

## [2026-07-20] Implement Aligned Hand-Drawn Platform Sprites
- Wrote a Python script `process_uploaded_transparent.py` to extract high-quality platform sprites from the user's new transparent PNG sheet:
  - `GroundVertical.png` (Vertical column): Cropped from the left column `(94, 42, 418, 769)`, transparentized from checkered grids, and resized to `128x128`.
  - `GroundSquare.png` (Square platform): Cropped from the right block `(549, 213, 936, 605)`, transparentized from checkered grids, and resized to `128x128`.
- Re-imported assets as point-filtered sprites and regenerated both the Prototype and Gameplay scenes with the updated hand-drawn textures.

## [2026-07-20] Implement Gyro/Tilt Controls for Mobile Platforms
- Added serialized fields (`useTiltControl`, `tiltSensitivity`, `tiltDeadZone`, `invertTilt`) to [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) under a new "Tilt / Gyro Settings" header.
- Implemented `OnEnable` and `OnDisable` to dynamically enable and disable the New Input System `GravitySensor` and `Accelerometer` devices.
- Added `GetTiltHorizontalInput` helper to read from the modern `GravitySensor` (best for smooth gravity-based tilt) and automatically fall back to `Accelerometer` if `GravitySensor` is not available.
- Updated `Update()` to read tilt movement if no keyboard input is currently active, allowing seamless editor testing with keyboards and natural tilt controls on mobile devices.

## [2026-07-20] Fix PlatformManager UnityEditor Build Compilation Error
- Exposed platform sprite fields as `public` on [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs) to allow assignment from Inspector/Editor scripts.
- Wrapped `UnityEditor.AssetDatabase` asset loading calls in `PlatformManager.cs` inside `#if UNITY_EDITOR` preprocessor blocks, preventing Android/mobile build compilation errors.
- Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to pre-load and assign all ground and wall sprites directly to the `PlatformManager` during programmatic scene generation.
- Fixed duplicate local variable declarations (`groundSquarePath`, `groundVerticalPath`, `groundDirtOnlyPath`, and `squarePath`) in `SceneSetup.cs` to resolve compiler warning CS0128.

## [2026-07-23] Implement Main Screen, UI Architecture, and Game State System
- Refactored [GameManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/GameManager.cs) to introduce an explicit `GameState` machine (`MainMenu`, `Playing`, `Paused`, `GameOver`), persistent High Score saving (`PlayerPrefs`), state change events (`OnGameStateChanged`, `OnScoreChanged`), and completely removed legacy `OnGUI()` per-frame GC allocations.
- Created modular UI components in `Assets/_MyProject/Scripts/UI/`:
  - [UIManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/UIManager.cs): Listens to `GameManager` state transitions and controls UI panel visibility.
  - [MainMenuUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/MainMenuUI.cs): Displays game title, high score record, and pulsing play button ("TAP TO START").
  - [GameplayHUDUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/GameplayHUDUI.cs): Displays real-time current score and high score HUD.
  - [GameOverUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/GameOverUI.cs): Displays final score overlay and restart controls.
- Updated [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) with `IsPointerOverUI()` check to prevent character jumps or attacks when tapping UI buttons.
- Extended [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) with `SetupUICanvas()` helper to automatically construct the 9:16 mobile portrait Canvas and UI hierarchy in `PrototypeScene.unity` and `GameplayScene.unity`.

## [2026-07-24] Fix Input System StandaloneInputModule InvalidOperationException
- Resolved `InvalidOperationException` caused by `StandaloneInputModule` and legacy `UnityEngine.Input` calls when using Unity's modern Input System package:
  - Updated [UIManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/UIManager.cs) to dynamically replace `StandaloneInputModule` with `UnityEngine.InputSystem.UI.InputSystemUIInputModule` on `EventSystem` during `Awake()`.
  - Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to attach `InputSystemUIInputModule` when building new UI scene hierarchies.
  - Refactored `IsPointerOverUI()` in [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) to use `Touchscreen.current.touches` instead of legacy `UnityEngine.Input.GetTouch()` / `Input.touchCount`.

## [2026-07-24] Fix Touch/Mouse Cat Jump Input & UI Raycast Target Blocking
- Fixed issue where the cat did not jump on touch/mouse clicks during gameplay:
  - **Selective UI Raycast Filter**: Refactored `IsPointerOverUI()` in [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) using `EventSystem.RaycastAll` to check if pointer hits an interactive `Selectable`/`Button` component, preventing full-screen transparent UI panels (e.g. `GameplayHUDPanel`) from blocking gameplay jumps.
  - **Input System Device Support**: Expanded `PlayerController` jump input detection to support `Pointer.current`, `Mouse.current`, `Touchscreen.current`, and keyboard controls (`Space`, `UpArrow`, `W`).
  - **Wall Jump Condition**: Updated `Jump()` condition in `PlayerController.cs` to trigger when `isGrounded`, `isTouchingWall`, `isClimbing`, or `allowInfiniteJumps` is active.
  - **UI RaycastTarget Cleanup**: Disabled `raycastTarget` on full-screen transparent panels and static text elements in [GameplayScene.unity](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scenes/GameplayScene.unity), [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs), and [UIManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/UIManager.cs).

## [2026-07-24] Implement Skins & Shop UI Screen
- Created [ShopUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/ShopUI.cs) component to manage cat skin unlocks, selection, equipping, and persistent saving via `PlayerPrefs` (`CatClimb_SelectedSkin`).
- Added "SKINS & SHOP" button below the Play button on [MainMenuUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/MainMenuUI.cs).
- Connected [UIManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/UIManager.cs) to handle smooth transitions between Main Menu and Shop Screen (`ShowShop` / `HideShop`).
- Updated [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs) with `ApplySelectedSkin()` helper to dynamically apply chosen skin colors (Classic Orange, Sakura Pink, Shadow Ninja, Champion Gold, Cyber Neon) to the cat's `SpriteRenderer`.
- Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to programmatically construct the `ShopPanel` UI hierarchy with preview icons, descriptions, equip buttons, and "BACK TO MENU" navigation.

## [2026-07-27] Implement Main Menu Mission & Ranking Buttons and Connected Screens
- Created [MissionUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/MissionUI.cs) component to handle daily missions & challenges ("First Climb", "High Climber", "Sky Master", "Persistent Cat", "Mouse Hunter") with progress tracking, state updates, claim buttons, and persistent reward saving (`PlayerPrefs`).
- Created [RankingUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/RankingUI.cs) component to present a dynamic Leaderboard with rank badges (#1 🥇, #2 🥈, #3 🥉), real-time user rank calculation, and a highlighted player rank entry card ("YOU (Your Cat)").
- Updated [MainMenuUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/MainMenuUI.cs) with "DAILY MISSIONS" and "LEADERBOARD" buttons on the main menu layout.
- Updated [UIManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/UIManager.cs) to orchestrate smooth screen visibility transitions between Main Menu, Gameplay HUD, Game Over, Shop, Mission, and Ranking panels.
- Updated [Enemy.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Enemy.cs) and [GameManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/GameManager.cs) to persist metric counters (`CatClimb_DefeatedEnemies` and `CatClimb_TotalRuns`) for mission progression checks.
- Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to programmatically generate the `MissionPanel` and `RankingPanel` UI card hierarchies, button click listeners, and link all references to `UIManager`.

## [2026-07-27] Implement Fixed Width Camera Aspect Ratio Resolution Setter
- Created [CameraResolutionSetter.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/CameraResolutionSetter.cs) component to dynamically adapt the camera's orthographic size based on screen aspect ratio (`targetWidth = 1080`, `targetHeight = 1920`, base `orthographicSize = 5.0`).
- Ensures fixed horizontal world width across taller/narrower mobile aspect ratios (e.g. 9:19.5, 9:20), preventing left and right wall boundaries from being cropped on modern smartphones.
- Supports both standard `Camera.main` and Cinemachine Virtual Camera (`CinemachineCamera` / `CinemachineVirtualCamera`) via reflection.
- Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to automatically attach `CameraResolutionSetter` to the Main Camera during scene setup.

## [2026-07-27] Overlap Prevention for Platforms and Enemies
- Updated [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs) to eliminate platform-platform overlaps and enemy-platform overlaps:
  - **Enemy Surface Positioning**: Corrected local Y coordinate calculation for Enemy child objects so their bottom edge rests flush on top of the parent platform surface (`0.64f + (enemyScale / 2f) / py`), eliminating penetration into parent platforms.
  - **Edge Containment**: Constrained local X coordinates so enemies stay fully within their parent platform's top bounds without floating over edges.
  - **AABB Overlap Check**: Implemented `IsValidPlacement` using Axis-Aligned Bounding Box overlap detection with a safety margin (`0.05f`). Checks candidate platforms and active enemies against all existing active platforms and enemies in the pool.
  - **Dynamic Step Search**: Added multi-step X/Y candidate position search during pool initialization and platform recycling to guarantee non-overlapping placement.

## [2026-07-31] Fix Platform Spawning Distribution & Gap Clearance Validation
- Fixed issue where platforms were only spawning against side walls and restored balanced platform distribution in the air/middle:
  - **Air/Middle Candidate Range Sampling**: Updated [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs) to sample candidate positions across `[-airMaxX, airMaxX]` for floating air platforms (70% of spawns) while reserving 15% for Left Wall flush and 15% for Right Wall flush.
  - **Corrected Clearance Parameters**: Set `minClearance = 0.55f` (Cat diameter `0.50f` + `0.05f` buffer). Any floating platform spawned within `airMaxX` is guaranteed to maintain `>= 0.55f` units of open clearance to both side walls, allowing the cat to jump or fall through either side without touching walls or being trapped.
  - **Solid Block Collision**: Maintained standard solid `BoxCollider2D` physics so platforms act as solid obstacles that cannot be passed through from below.

## [2026-08-05] Implement Gameplay Pause Button and Pop-up Menu
- Created [PauseUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/PauseUI.cs) component to manage the pause popup modal interface with `Continue` and `Home` buttons.
- Updated [GameManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/GameManager.cs) to support `GameState.Paused`, manage `Time.timeScale` freeze (`0f` when paused, `1f` when active/main menu/game over), and provide `PauseGame()`, `ResumeGame()`, `ReturnToMainMenu()`, alongside `Escape`/`P` key toggle bindings.
- Updated [GameplayHUDUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/GameplayHUDUI.cs) with a pure icon image Pause Button (text label removed).
- Generated 2D pause icon asset (`PauseIcon`) and updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs) to programmatically generate the icon texture and render the pure icon sprite on the HUD canvas (fixed `spritesDir` scope in `SetupUICanvas`).

## [2026-08-06] Prevent Impassable Dual Wall Spikes Without Middle Platforms
- Updated [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs):
  - Added static `Instance` singleton property to allow global queries from wall hazard systems.
  - Added `HasMiddlePlatformNearY(float minY, float maxY)` helper method to query active floating middle platforms within a given vertical range (`|px| < 1.8f`).
- Updated [WallManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/WallManager.cs):
  - Added `sameYSpikeThreshold` (default `2.2f`) to define vertical proximity between opposing wall spikes.
  - Updated `PositionSingleSpike` to sample candidate Y positions and check if opposite wall spikes exist within `sameYSpikeThreshold`. If an opposite spike exists and no middle platform is present to provide a safe landing/jumping path, the system resamples or deactivates the candidate spike.
  - Added `ValidateSpikePositions()` called during initialization and every frame in `Update()`. If active left and right wall spikes exist at the same Y level without a middle platform (e.g. after platform recycling), one spike is automatically deactivated to guarantee that climbing is always possible.

## [2026-08-06] Moving Platforms at Score 3000 with Overlap Prevention
- Created [MovingPlatform.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/MovingPlatform.cs):
  - Handles vertical smooth movement for Square (`shapeChoice == 0`) and Horizontally Long (`shapeChoice == 2`) platforms when the player reaches score 3000.
  - Excludes Vertical Rectangle platforms (`shapeChoice == 1`) from vertical movement.
  - Implemented `WillOverlapOtherPlatform` using directional `Physics2D.OverlapBoxAll` checks to ensure moving platforms reverse direction before colliding with adjacent platforms above or below, preventing platforms from passing through each other.
  - Added player transportation logic to carry the player smoothly when standing on top of a moving platform.
- Updated [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs):
  - Automatically attaches and configures `MovingPlatform` components on all pooled platform instances during shape configuration and recycling.

## [2026-08-06] Update Moving Platform Score Threshold to 100 for Testing
- Updated [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs):
  - Added configurable `movingPlatformScoreThreshold = 100` property for easy testing (can be set back to 3000 for production).
- Updated [MovingPlatform.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/MovingPlatform.cs):
  - Evaluates score threshold against `PlatformManager.Instance.movingPlatformScoreThreshold` (100 for testing, equivalent to Y >= 10f).


## [2026-10-12] Implement Phase 1: Core Loop & Locomotion Overhaul (One-Way Platforms, Touch/Drag, Wall Climb & Wall-Kicks)
- Updated [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs):
  - Converted platform pooling setup and runtime recycling to configure `PlatformEffector2D` (`useOneWay = true`, `surfaceArc = 160f`, `rotationalOffset = 0f`) on all platforms.
  - Enabled `collider.usedByEffector = true` on `BoxCollider2D`, allowing the cat to jump upward cleanly through platforms from below and land securely on top (Doodle Jump mechanic).
  - Eliminated runtime `Destroy(effector)` calls during object recycling, adhering to zero-allocation gameplay performance guidelines.
- Updated [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs):
  - **Modern Hybrid-Casual Touch/Drag Steering**: Implemented direct horizontal tracking where the cat smoothly steers toward the user's touch/finger world position (`deltaX * dragResponsiveness`) with fallback to keyboard and tilt sensors.
  - **Wall Climb & Wall Sliding Synergy**: When contacting a wall, holding touch or pressing Up/W activates smooth upward climbing at `climbSpeed` with animated climbing frames. Releasing touch while falling against a wall engages controlled `wallSlideSpeed` friction.
  - **Swipe Wall-Kicks & Tap Kicks**: Implemented gesture detection (`ProcessSwipe` and tap release). Swiping away from a wall or upward launches an acrobatic diagonal impulse (`Vector2(-wallNormalX * wallKickForceX, wallKickForceY)`) for rapid zigzag climbing.
  - **One-Way Ground Safety**: Added vertical velocity threshold check (`rb.linearVelocity.y > 0.15f`) to prevent premature grounding or crouch interruptions while leaping upward through one-way platforms.
  - Updated visual state transitions and animation timers in `LateUpdate` to support wall sliding alongside wall climbing.

## [2026-10-12] Revert to Solid Physical Platforms (No Pass-Through From Below)
- Updated [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs):
  - Disabled `PlatformEffector2D` and set `collider.usedByEffector = false` across all pooled platforms and default templates.
  - Platforms function strictly as solid physical obstacles that block the cat from passing through from below, requiring tactical wall-jumping and navigating around them.
  - Preserves zero-allocation pooling performance by disabling effector components without calling `Destroy()`.

## [2026-10-12] Implement Phase 2: In-Run Economy & Revives (Fish Treats, Revive Modal, Mission Rewards & Shop Purchases)
- Created [FishTreat.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FishTreat.cs) collectible item component with gentle hover bobbing animation, trigger collection by the player cat, and zero-allocation pool deactivation.
- Generated and imported 128x128 golden fish snack/treat sprite asset (`FishTreat.png` and `FishTreat.png.meta`) in `Assets/_MyProject/Sprites/`.
- Updated [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs):
  - Added collectible pooling: attached a pooled `FishTreat` child GameObject to each platform during `InitializePool()`.
  - Recycles and repositions `FishTreat` during `RecyclePlatform()` with a 35% spawn chance when no enemy hazard is present.
  - Zero runtime allocations (`Instantiate` / `Destroy`) during active gameplay.
- Updated [GameManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/GameManager.cs):
  - Added `GameState.Revive` state machine transition.
  - Added lifetime and in-run economy persistence: `TotalCoins`, `SessionCoins`, `AddCoins(int)`, and `TrySpendCoins(int)`.
  - Added `HandlePlayerDefeat()`, `RevivePlayer()` (1 revive max per run, timeScale restoration, center safe respawn), and `ConfirmGameOver()`.
- Created [ReviveUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/ReviveUI.cs) modal dialog featuring a 5-second countdown timer, 20 Fish Coins revive button, simulated Rewarded Ad revive hook, and skip button.
- Updated [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs):
  - Added `RespawnAt(Vector3)` and invulnerability blinking shield routine (`TriggerInvulnerability(2.5f)`) to grant safe recovery upon reviving.
- Updated [GameplayHUDUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/GameplayHUDUI.cs) and [UIManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/UIManager.cs):
  - Added `coinText` counter display on the gameplay HUD, reflecting real-time collected fish treats during each run.
  - Orchestrated smooth modal transitions for `GameState.Revive`.
- Updated [ShopUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/ShopUI.cs) to display current Fish Coins balance and allow purchasing skins with coins.
- Updated [MissionUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/MissionUI.cs) to deposit coin rewards (`rewardCoins`) into `GameManager` upon claiming completed daily missions.
- Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs):
  - Added HUD Fish Coin counter card container and wired `hudUI.coinText`.
  - Constructed the full `RevivePanel` modal hierarchy with countdown and action buttons, linking references to `ReviveUI`.
  - Hooked `reviveUI` into `UIManager.SetReferences()`.
- Verified compilation with 0 errors and 0 warnings on both `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj`.

## [2026-10-12] Fix Damage Freeze: Dynamic Runtime ReviveUI Generation & GameOver Fallback
- Resolved issue where taking damage caused the game to halt with `Time.timeScale = 0f` due to uninstantiated `ReviveUI` in existing scenes:
  - Added `EnsureReviveUI()` in [UIManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/UIManager.cs) to dynamically construct the `RevivePanel` (popup card, 5s countdown text, 20-coin revive button, rewarded ad revive button, skip button) on the canvas at runtime if missing from the scene.
  - Added on-demand safety check in `HandleGameStateChanged(GameState.Revive)`: if `reviveUI` is missing, it dynamically creates it on the fly, and if still null, immediately falls back to `GameManager.Instance.ConfirmGameOver()`, guaranteeing the game never stays frozen.
  - Added `Init(...)` and `BindButtonListeners()` to [ReviveUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/ReviveUI.cs) to wire interactive button clicks and countdown logic when initialized dynamically.
  - Added `EnsureCoinCard()` in [GameplayHUDUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/GameplayHUDUI.cs) to dynamically instantiate the HUD coin counter if missing from the active scene.
  - Added player and camera null-safety fallbacks in `RevivePlayer()` in [GameManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/GameManager.cs).
  - Verified compilation with 0 errors and 0 warnings on both `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj`.

## [2026-10-13] Implement Phase 3: Power-ups & Dynamic Platform Mechanics (Spring Pads, Coin Magnet & Fragile Platforms)
- Created [SpringPad.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/SpringPad.cs) bouncy launch pad item:
  - Detects landing cat collisions when falling (`linearVelocity.y <= 1.0f`).
  - Calls `player.SuperBounce(bounceForce = 18f)` for an acrobatic super-jump launch.
  - Plays a 3-stage squash & stretch compression rebound animation routine.
- Created [MagnetItem.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/MagnetItem.cs) coin magnet power-up:
  - Features gentle floating hover bobbing animation.
  - On pickup, activates the magnet on the cat via `player.ActivateMagnet(duration = 8f)`.
  - Recycles cleanly back into the object pool with zero GC allocations.
- Created [FragilePlatform.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FragilePlatform.cs) crumbling platform mechanic:
  - Distinct amber visual tint alerting players to fragile status.
  - Detects when the cat lands on the top surface, initiates a warning wobble shake and flash for 0.55s before crumbling away (disabling collider and renderer).
  - Resets state cleanly when recycled back into the pool.
- Generated and imported 128x128 pixel art sprites in `Assets/_MyProject/Sprites/`:
  - `SpringPad.png`: Metallic coiled spring with red top cushion.
  - `MagnetItem.png`: Classic red horseshoe magnet with silver tips and golden sparkles.
- Updated [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs):
  - Added `SuperBounce(float bounceForce)` to launch the cat upward while setting aerial poses.
  - Added `ActivateMagnet(float duration)`, `IsMagnetActive`, and `MagnetRemainingTime`.
  - Implemented `AttractNearbyFish()` in `Update()`: smoothly pulls nearby pooled `FishTreat` items within a 5-unit radius directly to the cat.
- Updated [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs):
  - Attached pooled `SpringPad` and `MagnetItem` child GameObjects, plus `FragilePlatform` components, to all pooled platforms during `InitializePool()`.
  - Implemented mutually exclusive spawning during platform recycling (10% Enemy, 15% Spring Pad, 8% Magnet Power-up, 35% Fish Treat).
  - Configures fragile platforms (20% chance on non-vertical platforms without enemy or spring pad).
  - Strictly follows zero runtime `Instantiate` or `Destroy` gameplay constraints.
- Updated [GameplayHUDUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/GameplayHUDUI.cs):
  - Added dynamic `MagnetBadge` indicator showing real-time magnet countdown timer (e.g., `MAGNET: 7.8s`) with `EnsureMagnetBadge()` runtime self-healing.
- Updated [SceneSetup.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/SceneSetup.cs):
  - Configured Point filtering on `SpringPad.png` and `MagnetItem.png`.
  - Assigned sprite references to `PlatformManager`.
  - Integrated `MagnetBadge` into the HUD UI hierarchy.
  - Successfully regenerated `GameplayScene.unity` and `PrototypeScene.unity` in batchmode.
- Verified compilation with 0 errors and 0 warnings on both `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj`.

## [2026-10-13] Fix Platform Pooling Runaway Bug (Platforms Disappearing After Climb)
- Resolved critical bug where platforms stopped appearing after the player climbed past the initial platform set:
  - **Root Cause**: In [FragilePlatform.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FragilePlatform.cs), `ResetFragile()` contained `transform.localPosition = initialLocalPos;`, which was captured in `OnEnable()` at scene start (Y = -3f). Whenever a platform below the camera was recycled and positioned at the top of the tower, `ResetFragile()` instantly teleported the platform back to Y = -3f. This triggered repeated immediate recycling every frame, causing `highestPlatformY` to skyrocket into thousands of meters above the camera and leaving no platforms visible on screen.
  - **FragilePlatform Position Decoupling**: Removed all position overrides from [FragilePlatform.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FragilePlatform.cs). Platform positioning is now strictly managed by `PlatformManager`. `FragilePlatform` only manages horizontal wobble relative to its active position during crumble and resets its collider/renderer state.
  - **Dynamic Top-Platform Query**: Added `GetHighestPlatformY(GameObject excludePlatform)` in [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs) to dynamically compute the actual highest platform in the scene, preventing drift. Added camera viewport anchor safeguard (`baseTopY < cameraY + 5.5f`) so recycled platforms are always placed within reachable distance above the player.
  - **Collider State Restoration**: Explicitly guaranteed `collider.enabled = true` in `ConfigurePlatformShape()` in [PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs) so crumbled platforms reliably regain physical collision when recycled.
- Verified compilation with 0 errors and 0 warnings on both `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj`.

## [2026-10-13] Decommission Bouncy Spring Pad Mechanic & Codebase Cleanup
- Completely removed the bouncy spring pad mechanic following design evaluation (platforms in Cat Climb are solid obstacles rather than one-way pass-throughs, which made high vertical spring trajectories launch into the underside of platforms):
  - **PlatformManager.cs**: Removed `springPadSprite`, `springPadSpawnChance`, and `springPadScale` serialized fields; removed spring pad instantiation and attachment from `InitializePool()`; removed spring pad rolls and position adjustments from `RecyclePlatform()`; eliminated runtime legacy `Destroy()` checks to preserve zero-allocation gameplay performance.
  - **SceneSetup.cs**: Removed `SpringPad` sprite import reference, assignment to `PlatformManager`, and texture Point filter mode inclusion.
  - **Assembly-CSharp.csproj**: Removed `SpringPad.cs` from project compilation list.
  - **Asset & Script Cleanup**: Deleted `Assets/_MyProject/Scripts/SpringPad.cs`, `SpringPad.cs.meta`, `Assets/_MyProject/Sprites/SpringPad.png`, and `SpringPad.png.meta`.
  - **Build Verification**: Verified 0 errors and 0 warnings on both runtime and editor assemblies with `dotnet build`.

## [2026-10-14] Fix Magnet Power-Up: Dynamic Attraction & Position Override Decoupling
- Resolved issue where picking up the coin magnet power-up failed to pull Fish Treats to the player:
  - **Root Cause**: In [FishTreat.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FishTreat.cs), `Update()` was unconditionally executing `transform.localPosition = new Vector3(initialLocalPos.x, newY, initialLocalPos.z)` every frame to perform the idle hover bob. This immediately canceled out and snapped back any world-position displacement applied by the magnet in [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs), causing treats to remain frozen in place on their platforms.
  - **Dynamic Attraction Architecture**:
    - Added `isAttracted` state and `StartAttraction()` in [FishTreat.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FishTreat.cs). When magnetized, the idle local position override is bypassed, and the treat smoothly flies in world coordinates directly towards the cat with dynamic velocity (`Mathf.Max(magnetPullSpeed, dist * 4.0f)`).
    - Added self-detection in `FishTreat.Update()` for active magnets within range, providing dual-layer reliability alongside `PlayerController.AttractNearbyFish()`.
    - Added visual magnetic sparkle tint (`new Color(1f, 0.95f, 0.65f, 1f)`) while flying.
    - Guaranteed collection upon reaching proximity (`dist <= 0.65f`) as well as on physics trigger collision.
  - **PlayerController Enhancements**:
    - Exposed static `Instance` singleton for zero-cost runtime access by items.
    - Expanded `magnetRadius` from 5.0 to 7.5 units to comfortably pull coins across adjacent platform tiers.
    - Increased `magnetPullSpeed` from 12 to 18 units/s to ensure fast, satisfying suction even during high-velocity vertical cat climbs and jumps.
    - Tagged player GameObject with `"Player"` in `Awake()`.
  - **MagnetItem Fallback**: Added `PlayerController.Instance` fallback in [MagnetItem.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/MagnetItem.cs) `OnTriggerEnter2D` to ensure 100% reliable pickup trigger detection.
  - **Build Verification**: Verified 0 errors and 0 warnings on both runtime and editor assemblies with `dotnet build`.

## [2026-10-14] Implement Phase 4: Game Feel & Sensory Juice
- Fully implemented **Phase 4: Game Feel & Sensory Juice** to deliver satisfying hybrid-casual mobile game tactile feedback:
  - **Camera Trauma Screen Shake ([CameraFollowTarget.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/CameraFollowTarget.cs))**:
    - Implemented decaying Perlin noise trauma shake engine with quadratic response curve (`trauma * trauma`) for punchy visceral impacts.
    - Integrated with `AddTrauma(0.6f)` on hazard damage, `AddTrauma(0.22f)` on fragile platform collapse, and `AddTrauma(0.18f)` on altitude milestone achievements.
  - **Procedural Squash & Stretch ([PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs))**:
    - Dynamic landing squash proportional to downward impact velocity (`1.15x - 1.35x` horizontal squash, `0.65x - 0.85x` vertical compression).
    - Vertical jump launch stretch (`0.8x` horizontal, `1.25x` vertical).
    - Lateral wall-kick stretch (`1.22x` horizontal, `0.8x` vertical) into the kick trajectory.
    - Smooth zero-allocation spring recovery via Framerate-independent `Lerp` in `Update()` (`squashRecoverySpeed = 14f`).
  - **Zero-Allocation Particle Systems ([FXManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FXManager.cs))**:
    - Created `FXManager` singleton with pre-allocated pooled particle systems:
      - `JumpDust`: Emits directional soft dust puffs at the cat's feet on jump takeoff, platform landing, and wall kicks.
      - `CoinSparkle`: Emits radiant golden starburst particles when collecting [FishTreat](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FishTreat.cs).
      - `PlatformDebris`: Emits crumbling amber rock/debris fragments when a [FragilePlatform](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FragilePlatform.cs) collapses.
  - **Dynamic Streak & Altitude Toast System ([GameplayHUDUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/GameplayHUDUI.cs), [UIManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/UIManager.cs))**:
    - Created `EnsureToastBanner()` self-healing floating card with pop-in scale bounce (`1.35x -> 1.0x`), 1.0s hold, and smooth fade-out.
    - Integrated combo streak milestones on consecutive platform landings ("3x COMBO!", "5x STREAK!", "ON FIRE!", "UNSTOPPABLE!").
    - Integrated altitude milestone celebrations every 25 meters in [GameManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/GameManager.cs) ("25m REACHED!", "50m SKY HIGH!", "100m STRATOSPHERE!", "200m SPACE CAT!").
  - **SceneSetup & Build Verification**:
    - Registered `FXManager` in `SceneSetup.cs` and project assembly definitions.
    - Verified compilation with 0 errors and 0 warnings on both `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj`.
## [2026-10-14] Fix Fish Treat & Wall Spike Overlap with Multi-Layer Spatial Clearance
- Resolved issue where collectible fish treats and lethal wall spikes spawned at identical or intersecting coordinates on the side walls:
  - **Root Cause**: Platforms generated flush against the left (`X = -maxX`) or right (`X = maxX`) walls spawned child [FishTreat.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FishTreat.cs) collectibles at the platform center directly inside the lethal polygon bounds of wall spikes (`WallSpike`). Furthermore, [WallManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/WallManager.cs) randomized spike heights without checking if an active platform or fish treat occupied that vertical height on the wall.
  - **Zero-Allocation Active Item Tracking**:
    - Added static tracking lists `ActiveFishTreats` in [FishTreat.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FishTreat.cs) and `ActiveMagnetItems` in [MagnetItem.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/MagnetItem.cs).
    - Automatically registered on `OnEnable()` and unregistered on `OnDisable()` / `OnDestroy()` without runtime garbage collection.
  - **Platform & Item Spatial Clearance Exclusion ([PlatformManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlatformManager.cs))**:
    - Updated `IsValidPlacement`: checks `WallManager.Instance.IsSpikeNear(...)` so candidate platforms near the wall never physically clip through active wall spikes.
    - Updated `InitializePool()` and `RecyclePlatform()`: pre-checks candidate item world coordinates against `WallManager.Instance.IsSpikeNear(...)` with a 1.35-unit clearance threshold, suppressing item spawning whenever a wall spike is present on that side.
  - **Spike Spatial Exclusion & Real-Time Validation ([WallManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/WallManager.cs))**:
    - Added public singleton accessor `WallManager.Instance` and deterministic `EnsureInitialized()` helper.
    - Added `IsSpikeNear(worldX, worldY, verticalClearance)` query API.
    - Added `IsConflictWithItemOrPlatform(candidateY, isLeft)`: checks candidate spike heights against active fish treats, magnet items, and platforms flush to that wall before activating a spike.
    - Added real-time per-frame safety cleanup in `ValidateSpikePositions()` via `ValidateSpikesAgainstItems(...)`: immediately deactivates any active spike that overlaps an active collectible item, preventing unfair player deaths.
  - **Editor Clearance Validator ([FishSpikeClearanceValidator.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/Editor/FishSpikeClearanceValidator.cs))**:
    - Added validation menu utility under `Tools -> MyProject -> Validate Fish-Spike Spatial Clearance`.
  - **Build Verification**:
    - Verified compilation with 0 errors and 0 warnings on both `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj`.
## [2026-10-14] Implement Phase 5: Retro Audio System, Biome Atmospheres & Movement Polish
- Completed **Phase 5: Biomes, Audio & Polish** delivering full sensory immersion and progression:
  - **Zero-Allocation Retro Audio Engine ([AudioManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/AudioManager.cs))**:
    - Created `AudioManager` singleton managing an 8-channel pre-allocated `AudioSource` pool with round-robin channel allocation.
    - Synthesized 8 crisp, custom 16-bit 44.1kHz retro sound effects in `Assets/_MyProject/Audio/` using mathematical waveform synthesis:
      - `sfx_jump.wav`: cute bouncy upward chirp.
      - `sfx_wallkick.wav`: punchy acrobatic wall kick whoosh.
      - `sfx_coin.wav`: two-tone sparkling chime for golden fish treats.
      - `sfx_magnet.wav`: uplifting 4-note ascending powerup arpeggio with shimmer.
      - `sfx_crumble.wav`: crunchy stone rumble on fragile platform collapse.
      - `sfx_hit.wav`: blunt hazard impact on damage.
      - `sfx_streak.wav`: celebratory fanfare for combos, milestones, and achievements.
      - `sfx_click.wav`: snappy UI button press.
    - Integrated organic micro-pitch variation (±4-8%) on repetitive actions to eliminate ear fatigue.
    - Integrated persistent audio settings (`CatClimb_AudioEnabled`, `CatClimb_SFXVolume`) with `PlayerPrefs`.
    - Wired audio triggers across gameplay: [PlayerController.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/PlayerController.cs), [FishTreat.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FishTreat.cs), [MagnetItem.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/MagnetItem.cs), [FragilePlatform.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/FragilePlatform.cs), [GameManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/GameManager.cs), and all UI buttons.
  - **Audio Settings Toggle ([MainMenuUI.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/UI/MainMenuUI.cs))**:
    - Added self-healing sound toggle button (`🔊` / `🔇`) on Main Menu with immediate visual state synchronization and persistent storage.
  - **Dynamic Height Biomes & Altitude Atmospheres ([BiomeManager.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/BiomeManager.cs))**:
    - Created `BiomeManager` to dynamically transition the world atmosphere across 4 distinct altitude tiers:
      - **Alley Dawn** (0m - 50m): Deep Slate Twilight (`#1A202C`).
      - **Rooftops Sunset** (50m - 150m): Amber Plum Sunset (`#3D2138`).
      - **Stratosphere** (150m - 300m): Cosmic Deep Indigo (`#121631`).
      - **Deep Space** (300m+): Starlight Velvet Orbit (`#0A0A14`).
    - Smoothly interpolates the camera background color and triggers celebratory toast announcements ("BIOME: ROOFTOPS SUNSET!", etc.) with fanfare upon discovering new biomes.
  - **MovingPlatform Hazard Safety ([MovingPlatform.cs](file:///Users/minwookang/cat_climb/Assets/_MyProject/Scripts/MovingPlatform.cs))**:
    - Added dynamic safety check against `WallManager.Instance.IsSpikeNear(...)` in `WillOverlapOtherPlatform`, preventing moving platforms from oscillating into active wall spikes.
  - **Build Verification**:
    - Verified compilation with 0 errors and 0 warnings on both `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj`.
