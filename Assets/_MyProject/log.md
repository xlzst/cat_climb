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
