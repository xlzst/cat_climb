using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyProject;


namespace MyProject.Editor
{
    public static class SceneSetup
    {
        [InitializeOnLoadMethod]
        private static void OnEditorLoaded()
        {
            EditorApplication.delayCall += AutoGenerateScenesIfNeeded;
        }

        private static void AutoGenerateScenesIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            string prototypePath = "Assets/_MyProject/Scenes/PrototypeScene.unity";
            string gameplayPath = "Assets/_MyProject/Scenes/GameplayScene.unity";

            bool needProto = !File.Exists(prototypePath);
            bool needGameplay = !File.Exists(gameplayPath);

            if (needProto || needGameplay)
            {
                string originalScenePath = EditorSceneManager.GetActiveScene().path;

                if (needProto)
                {
                    Debug.Log("PrototypeScene.unity not found. Auto-generating...");
                    CreateScene("PrototypeScene", false);
                }

                if (needGameplay)
                {
                    Debug.Log("GameplayScene.unity not found. Auto-generating...");
                    CreateScene("GameplayScene", true);
                }

                if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
                {
                    EditorSceneManager.OpenScene(originalScenePath);
                }
            }
        }

        [MenuItem("Tools/MyProject/Create Prototype Scene")]
        public static void CreatePrototypeSceneForce()
        {
            Debug.Log("Manually triggering Prototype Scene creation...");
            CreateScene("PrototypeScene", false);
        }

        [MenuItem("Tools/MyProject/Create Gameplay Scene")]
        public static void CreateGameplaySceneForce()
        {
            Debug.Log("Manually triggering Gameplay Scene creation...");
            CreateScene("GameplayScene", true);
        }

        private static void CreateScene(string sceneName, bool usePixelCat)
        {
            Debug.Log($"Starting Scene Setup for {sceneName}...");

            // Programmatically setup "Enemy" layer if not present
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            bool enemyLayerExists = false;
            for (int i = 8; i < 32; i++)
            {
                SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
                if (layerProp.stringValue == "Enemy")
                {
                    enemyLayerExists = true;
                    break;
                }
            }
            if (!enemyLayerExists)
            {
                for (int i = 8; i < 32; i++)
                {
                    SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(layerProp.stringValue))
                    {
                        layerProp.stringValue = "Enemy";
                        break;
                    }
                }
                tagManager.ApplyModifiedProperties();
                Debug.Log("Successfully created 'Enemy' project layer.");
            }

            // Create directories if they don't exist
            string spritesDir = "Assets/_MyProject/Sprites";
            string scenesDir = "Assets/_MyProject/Scenes";
            if (!Directory.Exists(spritesDir))
            {
                Directory.CreateDirectory(spritesDir);
            }
            if (!Directory.Exists(scenesDir))
            {
                Directory.CreateDirectory(scenesDir);
            }

            // Programmatically generate white circle texture
            string circlePath = Path.Combine(spritesDir, "CatCircle.png");
            if (!File.Exists(circlePath))
            {
                int width = 128;
                int height = 128;
                Texture2D circleTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                float center = 63.5f;
                float radius = 64f;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                        if (dist <= radius)
                        {
                            // Smooth anti-aliased edge
                            float alpha = Mathf.Clamp01(radius - dist);
                            circleTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                        }
                        else
                        {
                            circleTex.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                circleTex.Apply();
                byte[] circleBytes = circleTex.EncodeToPNG();
                File.WriteAllBytes(circlePath, circleBytes);
                Debug.Log($"Generated circle texture at: {circlePath}");
                Object.DestroyImmediate(circleTex);
            }

            // Programmatically generate white square texture
            string squarePath = Path.Combine(spritesDir, "WallSquare.png");
            if (!File.Exists(squarePath))
            {
                int width = 128;
                int height = 128;
                Texture2D squareTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        squareTex.SetPixel(x, y, Color.white);
                    }
                }
                squareTex.Apply();
                byte[] squareBytes = squareTex.EncodeToPNG();
                File.WriteAllBytes(squarePath, squareBytes);
                Debug.Log($"Generated square texture at: {squarePath}");
                Object.DestroyImmediate(squareTex);
            }

            // Programmatically generate white triangle texture (pointing up) for Enemy (fallback)
            string enemyPath = Path.Combine(spritesDir, "EnemyMouse.png");
            if (!File.Exists(enemyPath))
            {
                int width = 128;
                int height = 128;
                Texture2D triangleTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                float center = 63.5f;
                for (int y = 0; y < height; y++)
                {
                    // At y = 0, width is full (x from 0 to 127)
                    // At y = 127, width is 0 (centered at x = 63.5)
                    float halfWidthAtY = (127f - y) / 127f * 64f;
                    for (int x = 0; x < width; x++)
                    {
                        if (x >= center - halfWidthAtY && x <= center + halfWidthAtY)
                        {
                            // Smooth anti-aliased edge
                            float dist = Mathf.Min(x - (center - halfWidthAtY), (center + halfWidthAtY) - x);
                            float alpha = Mathf.Clamp01(dist / 1.5f);
                            triangleTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                        }
                        else
                        {
                            triangleTex.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                triangleTex.Apply();
                byte[] triangleBytes = triangleTex.EncodeToPNG();
                File.WriteAllBytes(enemyPath, triangleBytes);
                Debug.Log($"Generated fallback triangle texture at: {enemyPath}");
                Object.DestroyImmediate(triangleTex);
            }

            // Programmatically generate pause icon texture
            string pauseIconPath = Path.Combine(spritesDir, "PauseIcon.png");
            if (!File.Exists(pauseIconPath) && !File.Exists(Path.Combine(spritesDir, "PauseIcon.jpg")))
            {
                int width = 128;
                int height = 128;
                Texture2D pauseTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                float center = 63.5f;
                float radius = 60f;

                Color bgColor = new Color(0.12f, 0.14f, 0.22f, 0.95f);
                Color barColor = new Color(0.95f, 0.96f, 0.98f, 1f);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                        if (dist <= radius)
                        {
                            bool inLeftBar = (x >= 42 && x <= 54 && y >= 36 && y <= 92);
                            bool inRightBar = (x >= 74 && x <= 86 && y >= 36 && y <= 92);

                            float alpha = Mathf.Clamp01(radius - dist);
                            if (inLeftBar || inRightBar)
                            {
                                pauseTex.SetPixel(x, y, barColor);
                            }
                            else
                            {
                                pauseTex.SetPixel(x, y, new Color(bgColor.r, bgColor.g, bgColor.b, bgColor.a * alpha));
                            }
                        }
                        else
                        {
                            pauseTex.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                pauseTex.Apply();
                byte[] pauseBytes = pauseTex.EncodeToPNG();
                File.WriteAllBytes(pauseIconPath, pauseBytes);
                Debug.Log($"Generated Pause Icon texture at: {pauseIconPath}");
                Object.DestroyImmediate(pauseTex);
            }

            // Force import assets
            AssetDatabase.Refresh();

            // Set import settings to Sprite (2D and UI)
            string pixelCatPath = Path.Combine(spritesDir, "pixel_cat.png");
            string pixelCatIdlePath = Path.Combine(spritesDir, "pixel_cat_idle.png");
            string pixelCatTurnPath = Path.Combine(spritesDir, "pixel_cat_turn.png");
            string pixelCatCrouchPath = Path.Combine(spritesDir, "pixel_cat_crouch.png");
            string pixelCatCrouchDeepPath = Path.Combine(spritesDir, "pixel_cat_crouch_deep.png");
            string pixelCatTakeoffPath = Path.Combine(spritesDir, "pixel_cat_takeoff.png");
            string pixelCatLaunchPath = Path.Combine(spritesDir, "pixel_cat_launch.png");
            string pixelCatRise1Path = Path.Combine(spritesDir, "pixel_cat_rise_1.png");
            string pixelCatRise2Path = Path.Combine(spritesDir, "pixel_cat_rise_2.png");
            string pixelCatJumpPath = Path.Combine(spritesDir, "pixel_cat_jump.png");
            string pixelCatJumpUpPath = Path.Combine(spritesDir, "pixel_cat_jump_up.png");
            string pixelCatClimbPath = Path.Combine(spritesDir, "pixel_cat_climb.png");
            string pixelCatWalk2Path = Path.Combine(spritesDir, "pixel_cat_walk_2.png");
            string pixelCatClimb2Path = Path.Combine(spritesDir, "pixel_cat_climb_2.png");
            string pixelCatClimb3Path = Path.Combine(spritesDir, "pixel_cat_climb_3.png");
            string pixelCatClimb4Path = Path.Combine(spritesDir, "pixel_cat_climb_4.png");
            string pixelCatWalk3Path = Path.Combine(spritesDir, "pixel_cat_walk_3.png");
            string pixelCatWalk4Path = Path.Combine(spritesDir, "pixel_cat_walk_4.png");
            string pixelCatAttack1Path = Path.Combine(spritesDir, "pixel_cat_attack_1.png");
            string pixelCatAttack2Path = Path.Combine(spritesDir, "pixel_cat_attack_2.png");
            string pixelCatAttack3Path = Path.Combine(spritesDir, "pixel_cat_attack_3.png");
            string pixelCatAttack4Path = Path.Combine(spritesDir, "pixel_cat_attack_4.png");

            if (File.Exists(pixelCatPath)) SetAsSprite(pixelCatPath);
            if (File.Exists(pixelCatIdlePath)) SetAsSprite(pixelCatIdlePath);
            if (File.Exists(pixelCatTurnPath)) SetAsSprite(pixelCatTurnPath);
            if (File.Exists(pixelCatCrouchPath)) SetAsSprite(pixelCatCrouchPath);
            if (File.Exists(pixelCatCrouchDeepPath)) SetAsSprite(pixelCatCrouchDeepPath);
            if (File.Exists(pixelCatTakeoffPath)) SetAsSprite(pixelCatTakeoffPath);
            if (File.Exists(pixelCatLaunchPath)) SetAsSprite(pixelCatLaunchPath);
            if (File.Exists(pixelCatRise1Path)) SetAsSprite(pixelCatRise1Path);
            if (File.Exists(pixelCatRise2Path)) SetAsSprite(pixelCatRise2Path);
            if (File.Exists(pixelCatJumpPath)) SetAsSprite(pixelCatJumpPath);
            if (File.Exists(pixelCatJumpUpPath)) SetAsSprite(pixelCatJumpUpPath);
            if (File.Exists(pixelCatClimbPath)) SetAsSprite(pixelCatClimbPath);
            if (File.Exists(pixelCatWalk2Path)) SetAsSprite(pixelCatWalk2Path);
            if (File.Exists(pixelCatClimb2Path)) SetAsSprite(pixelCatClimb2Path);
            if (File.Exists(pixelCatClimb3Path)) SetAsSprite(pixelCatClimb3Path);
            if (File.Exists(pixelCatClimb4Path)) SetAsSprite(pixelCatClimb4Path);
            if (File.Exists(pixelCatWalk3Path)) SetAsSprite(pixelCatWalk3Path);
            if (File.Exists(pixelCatWalk4Path)) SetAsSprite(pixelCatWalk4Path);
            if (File.Exists(pixelCatAttack1Path)) SetAsSprite(pixelCatAttack1Path);
            if (File.Exists(pixelCatAttack2Path)) SetAsSprite(pixelCatAttack2Path);
            if (File.Exists(pixelCatAttack3Path)) SetAsSprite(pixelCatAttack3Path);
            if (File.Exists(pixelCatAttack4Path)) SetAsSprite(pixelCatAttack4Path);

            SetAsSprite(circlePath);
            SetAsSprite(squarePath);
            SetAsSprite(enemyPath);
            string wallSpikePath = Path.Combine(spritesDir, "WallSpike.png");
            if (File.Exists(wallSpikePath)) SetAsSprite(wallSpikePath);
            string pixelWallPath = Path.Combine(spritesDir, "pixel_wall_tile.png");
            if (File.Exists(pixelWallPath)) SetAsSprite(pixelWallPath);
            string groundSpritePath = Path.Combine(spritesDir, "GroundSprite.png");
            if (File.Exists(groundSpritePath)) SetAsSprite(groundSpritePath);
            string groundSquarePath = Path.Combine(spritesDir, "GroundSquare.png");
            if (File.Exists(groundSquarePath)) SetAsSprite(groundSquarePath);
            string groundVerticalPath = Path.Combine(spritesDir, "GroundVertical.png");
            if (File.Exists(groundVerticalPath)) SetAsSprite(groundVerticalPath);
            string groundDirtOnlyPath = Path.Combine(spritesDir, "GroundDirtOnly.png");
            if (File.Exists(groundDirtOnlyPath)) SetAsSprite(groundDirtOnlyPath);
            string pauseJpgPath = Path.Combine(spritesDir, "PauseIcon.jpg");
            if (File.Exists(pauseIconPath)) SetAsSprite(pauseIconPath);
            if (File.Exists(pauseJpgPath)) SetAsSprite(pauseJpgPath);

            AssetDatabase.Refresh();

            // Create prototype scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Configure Camera
            GameObject cameraGo = GameObject.Find("Main Camera");
            if (cameraGo != null)
            {
                Camera cam = cameraGo.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.orthographic = true;
                    cam.orthographicSize = 5f;
                    cameraGo.transform.position = new Vector3(0, 0, -10);
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f); // Smooth premium dark theme
                }

                if (cameraGo.GetComponent<CameraResolutionSetter>() == null)
                {
                    cameraGo.AddComponent<CameraResolutionSetter>();
                }
            }

            // Destroy Directional Light (not needed for 2D UI/URP sprite setup without lighting)
            GameObject lightGo = GameObject.Find("Directional Light");
            if (lightGo != null)
            {
                Object.DestroyImmediate(lightGo);
            }

            // Load generated Sprites
            Sprite catSprite;
            Color catColor;
            if (usePixelCat && File.Exists(pixelCatPath))
            {
                catSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatPath);
                catColor = Color.white;
            }
            else
            {
                catSprite = AssetDatabase.LoadAssetAtPath<Sprite>(circlePath);
                catColor = new Color(1f, 0.45f, 0.45f, 1f); // Vibrant light coral pink
            }

            pixelWallPath = Path.Combine(spritesDir, "pixel_wall_tile.png");
            Sprite wallSprite = null;
            if (File.Exists(pixelWallPath))
            {
                wallSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelWallPath);
            }
            else
            {
                wallSprite = AssetDatabase.LoadAssetAtPath<Sprite>(squarePath);
            }
             Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>(enemyPath);
            Sprite wallSpikeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(wallSpikePath);

            groundSpritePath = Path.Combine(spritesDir, "GroundSprite.png");
            Sprite groundSprite = null;
            if (File.Exists(groundSpritePath))
            {
                groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(groundSpritePath);
            }

            if (catSprite == null || wallSprite == null || enemySprite == null || wallSpikeSprite == null)
            {
                Debug.LogError("Failed to load generated sprites. Please check import settings.");
                return;
            }

            // 1080x1920 (9:16) Viewport Calculations:
            // Camera orthographicSize = 5, which means height is 10 units.
            // Width = height * (9 / 16) = 5.625 units.
            // Screen boundaries: Left/Right at X = +/- 2.8125, Bottom at Y = -5.0.

            // Create Cat (Logic & Physics parent, scale = 1.0)
            GameObject catGo = new GameObject("Cat");
            catGo.transform.position = new Vector3(0f, -4.55f, 0f);
            catGo.transform.localScale = Vector3.one;

            // Create Visual child (scale = 0.5)
            GameObject visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(catGo.transform);
            visualGo.transform.localPosition = Vector3.zero;
            visualGo.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            SpriteRenderer catRenderer = visualGo.AddComponent<SpriteRenderer>();
            catRenderer.sprite = catSprite;
            catRenderer.color = catColor;
            catRenderer.sortingOrder = 10;
            
            Rigidbody2D catRb = catGo.AddComponent<Rigidbody2D>();
            catRb.bodyType = RigidbodyType2D.Dynamic; // Explicitly dynamic to be affected by gravity
            catRb.gravityScale = 1f; // Affected by standard gravity
            catRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            catRb.constraints = RigidbodyConstraints2D.FreezeRotation;

            CircleCollider2D catCollider = catGo.AddComponent<CircleCollider2D>();
            catCollider.radius = 0.25f; // Matches sprite world size (0.5 * 0.5 = 0.25)

            // Attach PlayerController runtime component
            PlayerController playerCtrl = catGo.AddComponent<PlayerController>();

            // Assign sprites to PlayerController
            if (usePixelCat)
            {
                if (File.Exists(pixelCatPath))
                {
                    playerCtrl.walkingSprite = catSprite;
                }
                if (File.Exists(pixelCatIdlePath))
                {
                    playerCtrl.idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatIdlePath);
                }
                if (File.Exists(pixelCatTurnPath))
                {
                    playerCtrl.transitionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatTurnPath);
                }
                if (File.Exists(pixelCatCrouchPath))
                {
                    playerCtrl.crouchSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatCrouchPath);
                }
                if (File.Exists(pixelCatCrouchDeepPath))
                {
                    playerCtrl.crouchDeepSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatCrouchDeepPath);
                }
                if (File.Exists(pixelCatTakeoffPath))
                {
                    playerCtrl.takeoffSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatTakeoffPath);
                }
                if (File.Exists(pixelCatLaunchPath))
                {
                    playerCtrl.launchSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatLaunchPath);
                }
                if (File.Exists(pixelCatRise1Path))
                {
                    playerCtrl.rise1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatRise1Path);
                }
                if (File.Exists(pixelCatRise2Path))
                {
                    playerCtrl.rise2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatRise2Path);
                }
                if (File.Exists(pixelCatJumpPath))
                {
                    playerCtrl.jumpSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatJumpPath);
                }
                if (File.Exists(pixelCatJumpUpPath))
                {
                    playerCtrl.verticalJumpSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatJumpUpPath);
                }
                if (File.Exists(pixelCatClimbPath))
                {
                    playerCtrl.climbSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatClimbPath);
                }
                if (File.Exists(pixelCatWalk2Path))
                {
                    playerCtrl.walkingSprite2 = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatWalk2Path);
                }
                if (File.Exists(pixelCatClimb2Path))
                {
                    playerCtrl.climbSprite2 = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatClimb2Path);
                }
                if (File.Exists(pixelCatClimb3Path))
                {
                    playerCtrl.climbSprite3 = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatClimb3Path);
                }
                if (File.Exists(pixelCatClimb4Path))
                {
                    playerCtrl.climbSprite4 = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatClimb4Path);
                }
                if (File.Exists(pixelCatWalk3Path))
                {
                    playerCtrl.walkingSprite3 = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatWalk3Path);
                }
                if (File.Exists(pixelCatWalk4Path))
                {
                    playerCtrl.walkingSprite4 = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatWalk4Path);
                }
                if (File.Exists(pixelCatAttack1Path))
                {
                    playerCtrl.attackSprite1 = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatAttack1Path);
                }
                if (File.Exists(pixelCatAttack2Path))
                {
                    playerCtrl.attackSprite2 = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatAttack2Path);
                }
                if (File.Exists(pixelCatAttack3Path))
                {
                    playerCtrl.attackSprite3 = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatAttack3Path);
                }
                if (File.Exists(pixelCatAttack4Path))
                {
                    playerCtrl.attackSprite4 = AssetDatabase.LoadAssetAtPath<Sprite>(pixelCatAttack4Path);
                }
            }
            else
            {
                playerCtrl.walkingSprite = catSprite;
                playerCtrl.walkingSprite2 = catSprite;
                playerCtrl.walkingSprite3 = catSprite;
                playerCtrl.walkingSprite4 = catSprite;
                playerCtrl.idleSprite = catSprite;
                playerCtrl.transitionSprite = catSprite;
                playerCtrl.crouchSprite = catSprite;
                playerCtrl.crouchDeepSprite = catSprite;
                playerCtrl.takeoffSprite = catSprite;
                playerCtrl.launchSprite = catSprite;
                playerCtrl.rise1Sprite = catSprite;
                playerCtrl.rise2Sprite = catSprite;
                playerCtrl.jumpSprite = catSprite;
                playerCtrl.verticalJumpSprite = catSprite;
                playerCtrl.climbSprite = catSprite;
                playerCtrl.climbSprite2 = catSprite;
                playerCtrl.climbSprite3 = catSprite;
                playerCtrl.climbSprite4 = catSprite;
                playerCtrl.attackSprite1 = catSprite;
                playerCtrl.attackSprite2 = catSprite;
                playerCtrl.attackSprite3 = catSprite;
                playerCtrl.attackSprite4 = catSprite;
            }

            // Create Left Wall (Rectangle)
            GameObject leftWallGo = new GameObject("LeftWall");
            leftWallGo.transform.position = new Vector3(-2.8125f, 0f, 0f);
            leftWallGo.transform.localScale = Vector3.one;
            SpriteRenderer leftWallRenderer = leftWallGo.AddComponent<SpriteRenderer>();
            leftWallRenderer.sprite = wallSprite;
            leftWallRenderer.drawMode = SpriteDrawMode.Tiled;
            leftWallRenderer.tileMode = SpriteTileMode.Continuous;
            leftWallRenderer.size = new Vector2(0.4f, 10f);
            leftWallRenderer.color = Color.white;
            leftWallRenderer.sortingOrder = 1;

            BoxCollider2D leftWallCollider = leftWallGo.AddComponent<BoxCollider2D>();
            leftWallCollider.size = leftWallRenderer.size;

            // Create Right Wall (Rectangle)
            GameObject rightWallGo = new GameObject("RightWall");
            rightWallGo.transform.position = new Vector3(2.8125f, 0f, 0f);
            rightWallGo.transform.localScale = Vector3.one;
            SpriteRenderer rightWallRenderer = rightWallGo.AddComponent<SpriteRenderer>();
            rightWallRenderer.sprite = wallSprite;
            rightWallRenderer.drawMode = SpriteDrawMode.Tiled;
            rightWallRenderer.tileMode = SpriteTileMode.Continuous;
            rightWallRenderer.size = new Vector2(0.4f, 10f);
            rightWallRenderer.color = Color.white;
            rightWallRenderer.sortingOrder = 1;

            BoxCollider2D rightWallCollider = rightWallGo.AddComponent<BoxCollider2D>();
            rightWallCollider.size = rightWallRenderer.size;

            // Create Floor (Rectangle)
            GameObject floorGo = new GameObject("Floor");
            if (groundSprite != null)
            {
                // Align floor position so that the top surface of the 1.28 unit tall sprite lands at Y = -4.8f
                floorGo.transform.position = new Vector3(0f, -5.44f, 0f);
            }
            else
            {
                floorGo.transform.position = new Vector3(0f, -5.0f, 0f);
            }
            floorGo.transform.localScale = Vector3.one;
            SpriteRenderer floorRenderer = floorGo.AddComponent<SpriteRenderer>();
            if (groundSprite != null)
            {
                floorRenderer.sprite = groundSprite;
            }
            else
            {
                floorRenderer.sprite = wallSprite;
            }
            floorRenderer.drawMode = SpriteDrawMode.Tiled;
            floorRenderer.tileMode = SpriteTileMode.Continuous;
            if (groundSprite != null)
            {
                floorRenderer.size = new Vector2(6f, 1.28f);
            }
            else
            {
                floorRenderer.size = new Vector2(6f, 0.4f);
            }
            floorRenderer.color = Color.white;
            floorRenderer.sortingOrder = 1;

            BoxCollider2D floorCollider = floorGo.AddComponent<BoxCollider2D>();
            floorCollider.size = floorRenderer.size;

            // Create Platform Manager
            GameObject platformManagerGo = new GameObject("PlatformManager");
            PlatformManager platformManager = platformManagerGo.AddComponent<PlatformManager>();
            platformManager.enemySprite = enemySprite;
            platformManager.groundSprite = groundSprite;

            if (File.Exists(groundSquarePath))
            {
                platformManager.groundSquareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(groundSquarePath);
            }
            if (File.Exists(groundVerticalPath))
            {
                platformManager.groundVerticalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(groundVerticalPath);
            }
            if (File.Exists(groundDirtOnlyPath))
            {
                platformManager.groundDirtOnlySprite = AssetDatabase.LoadAssetAtPath<Sprite>(groundDirtOnlyPath);
            }
            if (File.Exists(squarePath))
            {
                platformManager.wallSquareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(squarePath);
            }

            // Create Wall Manager
            GameObject wallManagerGo = new GameObject("WallManager");
            WallManager wallManager = wallManagerGo.AddComponent<WallManager>();
            wallManager.spikeSprite = wallSpikeSprite;

            // Create Game Manager
            GameObject gameManagerGo = new GameObject("GameManager");
            gameManagerGo.AddComponent<GameManager>();

            // 5. Cinemachine Setup via Reflection (avoids compilation reference issues)
            System.Type brainType = System.Type.GetType("Unity.Cinemachine.CinemachineBrain,Unity.Cinemachine") ?? System.Type.GetType("Cinemachine.CinemachineBrain,Cinemachine");
            System.Type cameraType = System.Type.GetType("Unity.Cinemachine.CinemachineCamera,Unity.Cinemachine") ?? System.Type.GetType("Cinemachine.CinemachineVirtualCamera,Cinemachine");
            System.Type composerType = System.Type.GetType("Unity.Cinemachine.CinemachinePositionComposer,Unity.Cinemachine") ?? System.Type.GetType("Cinemachine.CinemachineFramingTransposer,Cinemachine");

            if (cameraGo != null && brainType != null)
            {
                if (cameraGo.GetComponent(brainType) == null)
                {
                    cameraGo.AddComponent(brainType);
                }
            }

            // Create Camera Follow Target
            GameObject cameraTargetGo = new GameObject("CameraTarget");
            CameraFollowTarget followTarget = cameraTargetGo.AddComponent<CameraFollowTarget>();
            followTarget.SetTarget(catGo.transform);

            // Create Cinemachine Virtual Camera
            if (cameraType != null)
            {
                GameObject vcamGo = new GameObject("Cinemachine2DCamera");
                Component vcam = vcamGo.AddComponent(cameraType);
                
                // Set Follow Target
                var followProp = cameraType.GetProperty("Follow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (followProp != null)
                {
                    followProp.SetValue(vcam, cameraTargetGo.transform);
                }

                // Set Lens settings dynamically via reflection
                var lensProp = (MemberInfo)cameraType.GetField("Lens", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                               (MemberInfo)cameraType.GetProperty("Lens", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                               (MemberInfo)cameraType.GetField("m_Lens", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (lensProp != null)
                {
                    object lensVal = (lensProp is PropertyInfo pi) ? pi.GetValue(vcam) : ((FieldInfo)lensProp).GetValue(vcam);
                    if (lensVal != null)
                    {
                        var lensType = lensVal.GetType();
                        
                        var orthoField = lensType.GetField("Orthographic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                         lensType.GetField("m_Orthographic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (orthoField != null) orthoField.SetValue(lensVal, true);

                        var sizeField = lensType.GetField("OrthographicSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                        lensType.GetField("m_OrthographicSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (sizeField != null) sizeField.SetValue(lensVal, 5f);

                        // Put modified lens settings struct back
                        if (lensProp is PropertyInfo pi2)
                        {
                            pi2.SetValue(vcam, lensVal);
                        }
                        else if (lensProp is FieldInfo fi2)
                        {
                            fi2.SetValue(vcam, lensVal);
                        }
                    }
                }

                // Add composer for tracking
                if (composerType != null)
                {
                    vcamGo.AddComponent(composerType);
                }
                
                Debug.Log("Cinemachine components attached successfully via reflection.");
            }
            else
            {
                Debug.LogWarning("Cinemachine camera type not found. Please attach Cinemachine camera manually.");
            }

            // Create Canvas and UI panels
            SetupUICanvas();

            // Save Scene
            string scenePath = Path.Combine(scenesDir, $"{sceneName}.unity");
            bool saveResult = EditorSceneManager.SaveScene(scene, scenePath);
            if (saveResult)
            {
                Debug.Log($"Successfully created and saved scene at: {scenePath}");
            }
            else
            {
                Debug.LogError($"Failed to save scene at: {scenePath}");
                return;
            }

            // Register scene in Build Settings
            EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
            bool alreadyRegistered = false;
            foreach (var buildScene in originalScenes)
            {
                if (buildScene.path == scenePath)
                {
                    alreadyRegistered = true;
                    buildScene.enabled = true;
                    break;
                }
            }

            if (!alreadyRegistered)
            {
                EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
                newScenes[0] = new EditorBuildSettingsScene(scenePath, true);
                for (int i = 0; i < originalScenes.Length; i++)
                {
                    newScenes[i + 1] = originalScenes[i];
                }
                EditorBuildSettings.scenes = newScenes;
                Debug.Log($"Registered prototype scene at the top of build settings.");
            }

            // Apply Game View Resolution to 1080x1920 (Portrait)
            SetResolution(1080, 1920, "1080x1920");
        }

        private static void SetResolution(int width, int height, string label = "1080x1920")
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }
            try
            {
                var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null)
                {
                    Debug.LogWarning("Could not find GameView type.");
                    return;
                }

                var gameViewWindow = EditorWindow.GetWindow(gameViewType);
                if (gameViewWindow == null)
                {
                    Debug.LogWarning("Could not open GameView window.");
                    return;
                }

                var sizesType = System.Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
                if (sizesType == null)
                {
                    Debug.Log("Could not find GameViewSizes type (Safe for Unity 6+).");
                    return;
                }

                var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instanceProp = singleType.GetProperty("instance");
                if (instanceProp == null)
                {
                    Debug.Log("Could not find instance property on ScriptableSingleton<GameViewSizes> (Safe for Unity 6+).");
                    return;
                }

                var sizesInstance = instanceProp.GetValue(null, null);
                if (sizesInstance == null)
                {
                    Debug.Log("Could not get instance of GameViewSizes (Safe for Unity 6+).");
                    return;
                }

                var currentGroupProp = sizesType.GetProperty("currentGroup");
                if (currentGroupProp == null)
                {
                    Debug.Log("Could not find currentGroup property on GameViewSizes (Safe for Unity 6+).");
                    return;
                }

                var currentGroup = currentGroupProp.GetValue(sizesInstance, null);
                if (currentGroup == null)
                {
                    Debug.Log("Could not get currentGroup from GameViewSizes (Safe for Unity 6+).");
                    return;
                }

                var groupType = currentGroup.GetType();
                var getGameViewSizeMethod = groupType.GetMethod("GetGameViewSize", new System.Type[] { typeof(int) });
                var getRowCountMethod = groupType.GetMethod("GetRowCount");
                if (getGameViewSizeMethod == null || getRowCountMethod == null)
                {
                    Debug.Log("Could not find GetGameViewSize or GetRowCount methods (Safe for Unity 6+).");
                    return;
                }

                int count = (int)getRowCountMethod.Invoke(currentGroup, null);
                int foundIndex = -1;

                for (int i = 0; i < count; i++)
                {
                    var size = getGameViewSizeMethod.Invoke(currentGroup, new object[] { i });
                    if (size == null) continue;

                    var sizeType = size.GetType();
                    var widthField = sizeType.GetField("m_Width", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var heightField = sizeType.GetField("m_Height", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var typeField = sizeType.GetField("m_SizeType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (widthField == null || heightField == null || typeField == null) continue;

                    int w = (int)widthField.GetValue(size);
                    int h = (int)heightField.GetValue(size);
                    int type = (int)typeField.GetValue(size);

                    if (w == width && h == height && type == 1) // 1 is FixedResolution
                    {
                        foundIndex = i;
                        break;
                    }
                }

                if (foundIndex == -1)
                {
                    var gameViewSizeType = System.Type.GetType("UnityEditor.GameViewSize,UnityEditor");
                    var gameViewSizeTypeEnum = System.Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");
                    if (gameViewSizeType != null && gameViewSizeTypeEnum != null)
                    {
                        var gameViewSizeConstructor = gameViewSizeType.GetConstructor(new System.Type[] {
                            gameViewSizeTypeEnum,
                            typeof(int),
                            typeof(int),
                            typeof(string)
                        });

                        if (gameViewSizeConstructor != null)
                        {
                            // 1 is FixedResolution in GameViewSizeType enum
                            var gameViewSize = gameViewSizeConstructor.Invoke(new object[] { 1, width, height, label });
                            var addCustomSizeMethod = groupType.GetMethod("AddCustomSize", new System.Type[] { gameViewSizeType });
                            if (addCustomSizeMethod != null && gameViewSize != null)
                            {
                                addCustomSizeMethod.Invoke(currentGroup, new object[] { gameViewSize });
                                foundIndex = (int)getRowCountMethod.Invoke(currentGroup, null) - 1;
                                Debug.Log($"Added custom GameView resolution: {label} ({width}x{height})");
                            }
                        }
                    }
                }

                if (foundIndex != -1)
                {
                    // Select the resolution index in GameView
                    var selectedSizeIndexProp = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (selectedSizeIndexProp != null)
                    {
                        selectedSizeIndexProp.SetValue(gameViewWindow, foundIndex, null);
                        gameViewWindow.Repaint();
                        Debug.Log($"Selected GameView resolution index: {foundIndex} ({label})");
                    }
                    else
                    {
                        // In some versions it might be under PlayModeView or different name. Try fallback.
                        var selectedSizeIndexMethod = gameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (selectedSizeIndexMethod != null)
                        {
                            selectedSizeIndexMethod.Invoke(gameViewWindow, new object[] { foundIndex, null });
                            gameViewWindow.Repaint();
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not set GameView resolution via reflection: {e.Message}");
            }
        }

        private static void SetAsSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool needsSave = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    needsSave = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    needsSave = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    needsSave = true;
                }

                FilterMode targetFilter = (path.Contains("pixel_cat") || path.Contains("pixel_wall_tile") || path.Contains("GroundSprite") || path.Contains("GroundSquare") || path.Contains("GroundVertical") || path.Contains("GroundDirtOnly") || path.Contains("EnemyMouse")) ? FilterMode.Point : FilterMode.Bilinear;
                if (importer.filterMode != targetFilter)
                {
                    importer.filterMode = targetFilter;
                    needsSave = true;
                }

                if (path.Contains("pixel_wall_tile") || path.Contains("GroundSprite") || path.Contains("GroundSquare") || path.Contains("GroundVertical") || path.Contains("GroundDirtOnly") || path.Contains("WallSquare"))
                {
                    if (importer.wrapMode != TextureWrapMode.Repeat)
                    {
                        importer.wrapMode = TextureWrapMode.Repeat;
                        needsSave = true;
                    }
                }

                // Force Full Rect Sprite Mesh Type to prevent tiling warning
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMeshType != SpriteMeshType.FullRect)
                {
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    needsSave = true;
                }

                if (needsSave)
                {
                    importer.SaveAndReimport();
                    Debug.Log($"Set import settings to Sprite (Filter: {targetFilter}) for: {path}");
                }
            }
            else
            {
                Debug.LogWarning($"Could not find TextureImporter for asset at: {path}");
            }
        }

        private static void SetupUICanvas()
        {
            string spritesDir = "Assets/_MyProject/Sprites";

            // 1. Create EventSystem if missing
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 2. Create Canvas GameObject
            GameObject canvasGo = new GameObject("UI_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            UnityEngine.UI.CanvasScaler scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            MyProject.UI.UIManager uiManager = canvasGo.AddComponent<MyProject.UI.UIManager>();

            // 3. Create MainMenuPanel
            GameObject mainMenuGo = CreateUIPanel(canvasGo.transform, "MainMenuPanel", new Color(0.08f, 0.09f, 0.12f, 0.55f));
            MyProject.UI.MainMenuUI mainMenuUI = mainMenuGo.AddComponent<MyProject.UI.MainMenuUI>();

            // MainMenu Title
            GameObject titleGo = CreateUIText(mainMenuGo.transform, "TitleText", "CAT CLIMB", 80, FontStyle.Bold, new Color(1f, 0.42f, 0.42f, 1f), TextAnchor.MiddleCenter);
            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.78f);
            titleRect.anchorMax = new Vector2(0.5f, 0.78f);
            titleRect.sizeDelta = new Vector2(800, 110);

            // MainMenu Subtitle
            GameObject subtitleGo = CreateUIText(mainMenuGo.transform, "SubtitleText", "WALL CLIMB & DOODLE JUMP", 28, FontStyle.Bold, new Color(0.3f, 0.8f, 0.75f, 1f), TextAnchor.MiddleCenter);
            RectTransform subRect = subtitleGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 0.72f);
            subRect.anchorMax = new Vector2(0.5f, 0.72f);
            subRect.sizeDelta = new Vector2(800, 50);

            // High Score Display
            GameObject highScoreGo = CreateUIText(mainMenuGo.transform, "HighScoreText", "BEST RECORD: 0 FT", 34, FontStyle.Bold, new Color(1f, 0.9f, 0.4f, 1f), TextAnchor.MiddleCenter);
            RectTransform hsRect = highScoreGo.GetComponent<RectTransform>();
            hsRect.anchorMin = new Vector2(0.5f, 0.58f);
            hsRect.anchorMax = new Vector2(0.5f, 0.58f);
            hsRect.sizeDelta = new Vector2(700, 60);

            // Play Button
            GameObject playBtnGo = CreateUIButton(mainMenuGo.transform, "PlayButton", "TAP TO START", 44, new Color(0.3f, 0.75f, 0.65f, 1f), Color.white);
            RectTransform playBtnRect = playBtnGo.GetComponent<RectTransform>();
            playBtnRect.anchorMin = new Vector2(0.5f, 0.44f);
            playBtnRect.anchorMax = new Vector2(0.5f, 0.44f);
            playBtnRect.sizeDelta = new Vector2(440, 100);
            UnityEngine.UI.Button playBtn = playBtnGo.GetComponent<UnityEngine.UI.Button>();

            // Skins / Shop Button
            GameObject skinsBtnGo = CreateUIButton(mainMenuGo.transform, "SkinsButton", "SKINS & SHOP", 30, new Color(0.85f, 0.45f, 0.25f, 1f), Color.white);
            RectTransform skinsBtnRect = skinsBtnGo.GetComponent<RectTransform>();
            skinsBtnRect.anchorMin = new Vector2(0.5f, 0.32f);
            skinsBtnRect.anchorMax = new Vector2(0.5f, 0.32f);
            skinsBtnRect.sizeDelta = new Vector2(440, 80);
            UnityEngine.UI.Button skinsBtn = skinsBtnGo.GetComponent<UnityEngine.UI.Button>();

            // Daily Missions Button
            GameObject missionBtnGo = CreateUIButton(mainMenuGo.transform, "MissionButton", "DAILY MISSIONS", 30, new Color(0.25f, 0.6f, 0.85f, 1f), Color.white);
            RectTransform missionBtnRect = missionBtnGo.GetComponent<RectTransform>();
            missionBtnRect.anchorMin = new Vector2(0.5f, 0.22f);
            missionBtnRect.anchorMax = new Vector2(0.5f, 0.22f);
            missionBtnRect.sizeDelta = new Vector2(440, 80);
            UnityEngine.UI.Button missionBtn = missionBtnGo.GetComponent<UnityEngine.UI.Button>();

            // Leaderboard / Ranking Button
            GameObject rankingBtnGo = CreateUIButton(mainMenuGo.transform, "RankingButton", "LEADERBOARD", 30, new Color(0.7f, 0.4f, 0.85f, 1f), Color.white);
            RectTransform rankingBtnRect = rankingBtnGo.GetComponent<RectTransform>();
            rankingBtnRect.anchorMin = new Vector2(0.5f, 0.12f);
            rankingBtnRect.anchorMax = new Vector2(0.5f, 0.12f);
            rankingBtnRect.sizeDelta = new Vector2(440, 80);
            UnityEngine.UI.Button rankingBtn = rankingBtnGo.GetComponent<UnityEngine.UI.Button>();

            SetPrivateField(mainMenuUI, "menuPanel", mainMenuGo);
            SetPrivateField(mainMenuUI, "titleText", titleGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(mainMenuUI, "subtitleText", subtitleGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(mainMenuUI, "highScoreText", highScoreGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(mainMenuUI, "playButton", playBtn);
            SetPrivateField(mainMenuUI, "skinsButton", skinsBtn);
            SetPrivateField(mainMenuUI, "missionButton", missionBtn);
            SetPrivateField(mainMenuUI, "rankingButton", rankingBtn);

            // 4. Create GameplayHUDPanel
            GameObject hudGo = CreateUIPanel(canvasGo.transform, "GameplayHUDPanel", Color.clear);
            MyProject.UI.GameplayHUDUI hudUI = hudGo.AddComponent<MyProject.UI.GameplayHUDUI>();

            // Score Card Container
            GameObject scoreCard = CreateUIPanel(hudGo.transform, "ScoreCard", new Color(0.08f, 0.09f, 0.12f, 0.65f));
            RectTransform scoreCardRect = scoreCard.GetComponent<RectTransform>();
            scoreCardRect.anchorMin = new Vector2(0.05f, 0.92f);
            scoreCardRect.anchorMax = new Vector2(0.05f, 0.92f);
            scoreCardRect.pivot = new Vector2(0f, 1f);
            scoreCardRect.sizeDelta = new Vector2(340, 90);

            GameObject hudScoreGo = CreateUIText(scoreCard.transform, "ScoreText", "SCORE: 0", 28, FontStyle.Bold, new Color(0.3f, 0.8f, 0.75f, 1f), TextAnchor.MiddleCenter);
            RectTransform hudScoreRect = hudScoreGo.GetComponent<RectTransform>();
            hudScoreRect.anchorMin = new Vector2(0.5f, 0.5f);
            hudScoreRect.anchorMax = new Vector2(0.5f, 0.5f);
            hudScoreRect.sizeDelta = new Vector2(320, 80);

            // Pause Button Icon Sprite
            Sprite pauseIconSprite = null;
            string pauseJpg = Path.Combine(spritesDir, "PauseIcon.jpg");
            string pausePng = Path.Combine(spritesDir, "PauseIcon.png");
            if (File.Exists(pauseJpg))
            {
                pauseIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pauseJpg);
            }
            else if (File.Exists(pausePng))
            {
                pauseIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pausePng);
            }

            // Pause Button (Top Right - Icon Only)
            GameObject pauseBtnGo = CreateUIButton(hudGo.transform, "PauseButton", "", 0, Color.white, Color.white);
            RectTransform pauseBtnRect = pauseBtnGo.GetComponent<RectTransform>();
            pauseBtnRect.anchorMin = new Vector2(0.95f, 0.94f);
            pauseBtnRect.anchorMax = new Vector2(0.95f, 0.94f);
            pauseBtnRect.pivot = new Vector2(1f, 1f);
            pauseBtnRect.sizeDelta = new Vector2(90, 90);
            UnityEngine.UI.Button pauseBtn = pauseBtnGo.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Image pauseBtnImg = pauseBtnGo.GetComponent<UnityEngine.UI.Image>();
            if (pauseIconSprite != null)
            {
                pauseBtnImg.sprite = pauseIconSprite;
                pauseBtnImg.color = Color.white;
            }
            else
            {
                pauseBtnImg.color = new Color(0.15f, 0.18f, 0.26f, 0.85f);
            }

            // High Score Display (Next to Pause Button)
            GameObject hudHsGo = CreateUIText(hudGo.transform, "HighScoreText", "BEST: 0", 24, FontStyle.Bold, new Color(1f, 0.9f, 0.4f, 1f), TextAnchor.MiddleRight);
            RectTransform hudHsRect = hudHsGo.GetComponent<RectTransform>();
            hudHsRect.anchorMin = new Vector2(0.82f, 0.94f);
            hudHsRect.anchorMax = new Vector2(0.82f, 0.94f);
            hudHsRect.pivot = new Vector2(1f, 1f);
            hudHsRect.sizeDelta = new Vector2(250, 75);

            SetPrivateField(hudUI, "hudPanel", hudGo);
            SetPrivateField(hudUI, "scoreText", hudScoreGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(hudUI, "highScoreText", hudHsGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(hudUI, "pauseButton", pauseBtn);

            // 5. Create GameOverPanel
            GameObject gameOverGo = CreateUIPanel(canvasGo.transform, "GameOverPanel", new Color(0.08f, 0.09f, 0.12f, 0.88f));
            MyProject.UI.GameOverUI gameOverUI = gameOverGo.AddComponent<MyProject.UI.GameOverUI>();

            GameObject goTitleGo = CreateUIText(gameOverGo.transform, "GameOverTitle", "GAME OVER", 76, FontStyle.Bold, new Color(1f, 0.4f, 0.4f, 1f), TextAnchor.MiddleCenter);
            RectTransform goTitleRect = goTitleGo.GetComponent<RectTransform>();
            goTitleRect.anchorMin = new Vector2(0.5f, 0.66f);
            goTitleRect.anchorMax = new Vector2(0.5f, 0.66f);
            goTitleRect.sizeDelta = new Vector2(800, 110);

            GameObject goScoreGo = CreateUIText(gameOverGo.transform, "FinalScoreText", "FINAL SCORE\n0", 44, FontStyle.Bold, new Color(0.3f, 0.8f, 0.75f, 1f), TextAnchor.MiddleCenter);
            RectTransform goScoreRect = goScoreGo.GetComponent<RectTransform>();
            goScoreRect.anchorMin = new Vector2(0.5f, 0.50f);
            goScoreRect.anchorMax = new Vector2(0.5f, 0.50f);
            goScoreRect.sizeDelta = new Vector2(800, 120);

            GameObject goHsGo = CreateUIText(gameOverGo.transform, "HighScoreText", "BEST RECORD: 0", 32, FontStyle.Bold, new Color(1f, 0.9f, 0.4f, 1f), TextAnchor.MiddleCenter);
            RectTransform goHsRect = goHsGo.GetComponent<RectTransform>();
            goHsRect.anchorMin = new Vector2(0.5f, 0.39f);
            goHsRect.anchorMax = new Vector2(0.5f, 0.39f);
            goHsRect.sizeDelta = new Vector2(800, 60);

            GameObject restartBtnGo = CreateUIButton(gameOverGo.transform, "RestartButton", "RESTART", 40, new Color(1f, 0.45f, 0.45f, 1f), Color.white);
            RectTransform restartBtnRect = restartBtnGo.GetComponent<RectTransform>();
            restartBtnRect.anchorMin = new Vector2(0.5f, 0.26f);
            restartBtnRect.anchorMax = new Vector2(0.5f, 0.26f);
            restartBtnRect.sizeDelta = new Vector2(400, 110);
            UnityEngine.UI.Button restartBtn = restartBtnGo.GetComponent<UnityEngine.UI.Button>();

            SetPrivateField(gameOverUI, "gameOverPanel", gameOverGo);
            SetPrivateField(gameOverUI, "titleText", goTitleGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(gameOverUI, "finalScoreText", goScoreGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(gameOverUI, "highScoreText", goHsGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(gameOverUI, "restartButton", restartBtn);

            // 6. Create ShopPanel
            GameObject shopGo = CreateUIPanel(canvasGo.transform, "ShopPanel", new Color(0.08f, 0.09f, 0.14f, 0.96f));
            MyProject.UI.ShopUI shopUI = shopGo.AddComponent<MyProject.UI.ShopUI>();

            GameObject shopTitleGo = CreateUIText(shopGo.transform, "ShopTitle", "CAT SHOP & SKINS", 58, FontStyle.Bold, new Color(1f, 0.84f, 0.3f, 1f), TextAnchor.MiddleCenter);
            RectTransform shopTitleRect = shopTitleGo.GetComponent<RectTransform>();
            shopTitleRect.anchorMin = new Vector2(0.5f, 0.90f);
            shopTitleRect.anchorMax = new Vector2(0.5f, 0.90f);
            shopTitleRect.sizeDelta = new Vector2(800, 80);

            GameObject shopHsGo = CreateUIText(shopGo.transform, "ShopHighScoreText", "BEST RECORD: 0 FT", 28, FontStyle.Bold, new Color(0.3f, 0.8f, 0.75f, 1f), TextAnchor.MiddleCenter);
            RectTransform shopHsRect = shopHsGo.GetComponent<RectTransform>();
            shopHsRect.anchorMin = new Vector2(0.5f, 0.84f);
            shopHsRect.anchorMax = new Vector2(0.5f, 0.84f);
            shopHsRect.sizeDelta = new Vector2(800, 50);

            var skins = MyProject.UI.ShopUI.AvailableSkins;
            float startY = 0.73f;
            float stepY = 0.12f;

            for (int i = 0; i < skins.Count; i++)
            {
                var skin = skins[i];
                float currentY = startY - (i * stepY);

                GameObject cardGo = CreateUIPanel(shopGo.transform, $"SkinCard_{skin.id}", new Color(0.14f, 0.16f, 0.23f, 0.95f));
                RectTransform cardRect = cardGo.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, currentY);
                cardRect.anchorMax = new Vector2(0.5f, currentY);
                cardRect.sizeDelta = new Vector2(920, 150);

                // Color Preview Box
                GameObject iconGo = CreateUIPanel(cardGo.transform, "ColorPreview", skin.tintColor);
                RectTransform iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.08f, 0.5f);
                iconRect.anchorMax = new Vector2(0.08f, 0.5f);
                iconRect.sizeDelta = new Vector2(80, 80);

                // Name & Desc
                GameObject nameGo = CreateUIText(cardGo.transform, "SkinName", skin.skinName, 32, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                RectTransform nameRect = nameGo.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.20f, 0.65f);
                nameRect.anchorMax = new Vector2(0.20f, 0.65f);
                nameRect.pivot = new Vector2(0f, 0.5f);
                nameRect.sizeDelta = new Vector2(400, 45);

                GameObject descGo = CreateUIText(cardGo.transform, "SkinDesc", skin.description, 20, FontStyle.Normal, new Color(0.75f, 0.8f, 0.85f, 1f), TextAnchor.MiddleLeft);
                RectTransform descRect = descGo.GetComponent<RectTransform>();
                descRect.anchorMin = new Vector2(0.20f, 0.32f);
                descRect.anchorMax = new Vector2(0.20f, 0.32f);
                descRect.pivot = new Vector2(0f, 0.5f);
                descRect.sizeDelta = new Vector2(400, 40);

                // Select / Equip Button
                GameObject equipBtnGo = CreateUIButton(cardGo.transform, "EquipButton", "EQUIP", 24, new Color(0.25f, 0.65f, 0.55f, 1f), Color.white);
                RectTransform equipBtnRect = equipBtnGo.GetComponent<RectTransform>();
                equipBtnRect.anchorMin = new Vector2(0.82f, 0.5f);
                equipBtnRect.anchorMax = new Vector2(0.82f, 0.5f);
                equipBtnRect.sizeDelta = new Vector2(230, 75);

                UnityEngine.UI.Button equipBtn = equipBtnGo.GetComponent<UnityEngine.UI.Button>();
                UnityEngine.UI.Text statusTxt = equipBtnGo.GetComponentInChildren<UnityEngine.UI.Text>();

                shopUI.RegisterSkinCard(equipBtn, statusTxt, skin.id);
            }

            // Back Button
            GameObject shopBackBtnGo = CreateUIButton(shopGo.transform, "BackButton", "BACK TO MENU", 36, new Color(0.8f, 0.35f, 0.35f, 1f), Color.white);
            RectTransform shopBackBtnRect = shopBackBtnGo.GetComponent<RectTransform>();
            shopBackBtnRect.anchorMin = new Vector2(0.5f, 0.08f);
            shopBackBtnRect.anchorMax = new Vector2(0.5f, 0.08f);
            shopBackBtnRect.sizeDelta = new Vector2(480, 100);
            UnityEngine.UI.Button shopBackBtn = shopBackBtnGo.GetComponent<UnityEngine.UI.Button>();

            SetPrivateField(shopUI, "shopPanel", shopGo);
            SetPrivateField(shopUI, "titleText", shopTitleGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(shopUI, "highScoreText", shopHsGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(shopUI, "backButton", shopBackBtn);

            // 7. Create MissionPanel
            GameObject missionGo = CreateUIPanel(canvasGo.transform, "MissionPanel", new Color(0.08f, 0.09f, 0.14f, 0.96f));
            MyProject.UI.MissionUI missionUI = missionGo.AddComponent<MyProject.UI.MissionUI>();

            GameObject missionTitleGo = CreateUIText(missionGo.transform, "MissionTitle", "DAILY MISSIONS", 58, FontStyle.Bold, new Color(0.3f, 0.8f, 1f, 1f), TextAnchor.MiddleCenter);
            RectTransform missionTitleRect = missionTitleGo.GetComponent<RectTransform>();
            missionTitleRect.anchorMin = new Vector2(0.5f, 0.90f);
            missionTitleRect.anchorMax = new Vector2(0.5f, 0.90f);
            missionTitleRect.sizeDelta = new Vector2(800, 80);

            GameObject missionSummaryGo = CreateUIText(missionGo.transform, "MissionSummaryText", "COMPLETED: 0 / 5 MISSIONS", 28, FontStyle.Bold, new Color(0.3f, 0.8f, 0.75f, 1f), TextAnchor.MiddleCenter);
            RectTransform missionSummaryRect = missionSummaryGo.GetComponent<RectTransform>();
            missionSummaryRect.anchorMin = new Vector2(0.5f, 0.84f);
            missionSummaryRect.anchorMax = new Vector2(0.5f, 0.84f);
            missionSummaryRect.sizeDelta = new Vector2(800, 50);

            var missions = MyProject.UI.MissionUI.AvailableMissions;
            float mStartY = 0.73f;
            float mStepY = 0.12f;

            for (int i = 0; i < missions.Count; i++)
            {
                var mission = missions[i];
                float currentY = mStartY - (i * mStepY);

                GameObject cardGo = CreateUIPanel(missionGo.transform, $"MissionCard_{mission.id}", new Color(0.14f, 0.16f, 0.23f, 0.95f));
                RectTransform cardRect = cardGo.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, currentY);
                cardRect.anchorMax = new Vector2(0.5f, currentY);
                cardRect.sizeDelta = new Vector2(920, 150);

                // Mission Title
                GameObject mNameGo = CreateUIText(cardGo.transform, "MissionTitle", mission.title, 30, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                RectTransform mNameRect = mNameGo.GetComponent<RectTransform>();
                mNameRect.anchorMin = new Vector2(0.06f, 0.68f);
                mNameRect.anchorMax = new Vector2(0.06f, 0.68f);
                mNameRect.pivot = new Vector2(0f, 0.5f);
                mNameRect.sizeDelta = new Vector2(450, 40);

                // Mission Desc
                GameObject mDescGo = CreateUIText(cardGo.transform, "MissionDesc", $"{mission.description} ({mission.rewardText})", 20, FontStyle.Normal, new Color(0.75f, 0.8f, 0.85f, 1f), TextAnchor.MiddleLeft);
                RectTransform mDescRect = mDescGo.GetComponent<RectTransform>();
                mDescRect.anchorMin = new Vector2(0.06f, 0.32f);
                mDescRect.anchorMax = new Vector2(0.06f, 0.32f);
                mDescRect.pivot = new Vector2(0f, 0.5f);
                mDescRect.sizeDelta = new Vector2(450, 36);

                // Progress Text
                GameObject progGo = CreateUIText(cardGo.transform, "ProgressText", $"0 / {mission.targetValue}", 24, FontStyle.Bold, new Color(0.3f, 0.85f, 0.9f, 1f), TextAnchor.MiddleRight);
                RectTransform progRect = progGo.GetComponent<RectTransform>();
                progRect.anchorMin = new Vector2(0.68f, 0.68f);
                progRect.anchorMax = new Vector2(0.68f, 0.68f);
                progRect.pivot = new Vector2(1f, 0.5f);
                progRect.sizeDelta = new Vector2(200, 40);

                // Claim Button
                GameObject claimBtnGo = CreateUIButton(cardGo.transform, "ClaimButton", "IN PROGRESS", 22, new Color(0.25f, 0.28f, 0.35f, 0.8f), Color.white);
                RectTransform claimBtnRect = claimBtnGo.GetComponent<RectTransform>();
                claimBtnRect.anchorMin = new Vector2(0.82f, 0.40f);
                claimBtnRect.anchorMax = new Vector2(0.82f, 0.40f);
                claimBtnRect.sizeDelta = new Vector2(230, 70);

                UnityEngine.UI.Button claimBtn = claimBtnGo.GetComponent<UnityEngine.UI.Button>();
                UnityEngine.UI.Text claimBtnTxt = claimBtnGo.GetComponentInChildren<UnityEngine.UI.Text>();
                UnityEngine.UI.Text progTxt = progGo.GetComponent<UnityEngine.UI.Text>();

                missionUI.RegisterMissionCard(claimBtn, claimBtnTxt, progTxt, mission.id);
            }

            // Mission Back Button
            GameObject missionBackBtnGo = CreateUIButton(missionGo.transform, "BackButton", "BACK TO MENU", 36, new Color(0.8f, 0.35f, 0.35f, 1f), Color.white);
            RectTransform missionBackBtnRect = missionBackBtnGo.GetComponent<RectTransform>();
            missionBackBtnRect.anchorMin = new Vector2(0.5f, 0.08f);
            missionBackBtnRect.anchorMax = new Vector2(0.5f, 0.08f);
            missionBackBtnRect.sizeDelta = new Vector2(480, 100);
            UnityEngine.UI.Button missionBackBtn = missionBackBtnGo.GetComponent<UnityEngine.UI.Button>();

            SetPrivateField(missionUI, "missionPanel", missionGo);
            SetPrivateField(missionUI, "titleText", missionTitleGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(missionUI, "summaryText", missionSummaryGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(missionUI, "backButton", missionBackBtn);

            // 8. Create RankingPanel
            GameObject rankingGo = CreateUIPanel(canvasGo.transform, "RankingPanel", new Color(0.08f, 0.09f, 0.14f, 0.96f));
            MyProject.UI.RankingUI rankingUI = rankingGo.AddComponent<MyProject.UI.RankingUI>();

            GameObject rankTitleGo = CreateUIText(rankingGo.transform, "RankingTitle", "CAT LEADERBOARD", 58, FontStyle.Bold, new Color(0.85f, 0.5f, 0.95f, 1f), TextAnchor.MiddleCenter);
            RectTransform rankTitleRect = rankTitleGo.GetComponent<RectTransform>();
            rankTitleRect.anchorMin = new Vector2(0.5f, 0.90f);
            rankTitleRect.anchorMax = new Vector2(0.5f, 0.90f);
            rankTitleRect.sizeDelta = new Vector2(800, 80);

            GameObject userRankGo = CreateUIText(rankingGo.transform, "UserRankText", "YOUR RANK: #4 (BEST: 0 FT)", 28, FontStyle.Bold, new Color(1f, 0.9f, 0.3f, 1f), TextAnchor.MiddleCenter);
            RectTransform userRankRect = userRankGo.GetComponent<RectTransform>();
            userRankRect.anchorMin = new Vector2(0.5f, 0.84f);
            userRankRect.anchorMax = new Vector2(0.5f, 0.84f);
            userRankRect.sizeDelta = new Vector2(800, 50);

            float rStartY = 0.73f;
            float rStepY = 0.10f;
            int displayRows = 6;

            for (int i = 0; i < displayRows; i++)
            {
                float currentY = rStartY - (i * rStepY);

                GameObject cardGo = CreateUIPanel(rankingGo.transform, $"RankCard_{i}", new Color(0.14f, 0.16f, 0.23f, 0.95f));
                RectTransform cardRect = cardGo.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, currentY);
                cardRect.anchorMax = new Vector2(0.5f, currentY);
                cardRect.sizeDelta = new Vector2(920, 110);
                UnityEngine.UI.Image cardBgImg = cardGo.GetComponent<UnityEngine.UI.Image>();

                // Rank Num Text
                GameObject rankNumGo = CreateUIText(cardGo.transform, "RankNum", $"#{i + 1}", 32, FontStyle.Bold, new Color(0.8f, 0.85f, 0.9f, 1f), TextAnchor.MiddleLeft);
                RectTransform rankNumRect = rankNumGo.GetComponent<RectTransform>();
                rankNumRect.anchorMin = new Vector2(0.06f, 0.5f);
                rankNumRect.anchorMax = new Vector2(0.06f, 0.5f);
                rankNumRect.pivot = new Vector2(0f, 0.5f);
                rankNumRect.sizeDelta = new Vector2(150, 45);

                // Name Text
                GameObject nameGo = CreateUIText(cardGo.transform, "PlayerName", "Climber", 28, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                RectTransform nameRect = nameGo.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.24f, 0.5f);
                nameRect.anchorMax = new Vector2(0.24f, 0.5f);
                nameRect.pivot = new Vector2(0f, 0.5f);
                nameRect.sizeDelta = new Vector2(400, 45);

                // Score Text
                GameObject scoreGo = CreateUIText(cardGo.transform, "Score", "0 FT", 28, FontStyle.Bold, new Color(0.3f, 0.95f, 0.8f, 1f), TextAnchor.MiddleRight);
                RectTransform scoreRect = scoreGo.GetComponent<RectTransform>();
                scoreRect.anchorMin = new Vector2(0.94f, 0.5f);
                scoreRect.anchorMax = new Vector2(0.94f, 0.5f);
                scoreRect.pivot = new Vector2(1f, 0.5f);
                scoreRect.sizeDelta = new Vector2(240, 45);

                UnityEngine.UI.Text rankTxt = rankNumGo.GetComponent<UnityEngine.UI.Text>();
                UnityEngine.UI.Text nameTxt = nameGo.GetComponent<UnityEngine.UI.Text>();
                UnityEngine.UI.Text scoreTxt = scoreGo.GetComponent<UnityEngine.UI.Text>();

                rankingUI.RegisterRankingCard(rankTxt, nameTxt, scoreTxt, cardBgImg);
            }

            // Ranking Back Button
            GameObject rankBackBtnGo = CreateUIButton(rankingGo.transform, "BackButton", "BACK TO MENU", 36, new Color(0.8f, 0.35f, 0.35f, 1f), Color.white);
            RectTransform rankBackBtnRect = rankBackBtnGo.GetComponent<RectTransform>();
            rankBackBtnRect.anchorMin = new Vector2(0.5f, 0.08f);
            rankBackBtnRect.anchorMax = new Vector2(0.5f, 0.08f);
            rankBackBtnRect.sizeDelta = new Vector2(480, 100);
            UnityEngine.UI.Button rankBackBtn = rankBackBtnGo.GetComponent<UnityEngine.UI.Button>();

            SetPrivateField(rankingUI, "rankingPanel", rankingGo);
            SetPrivateField(rankingUI, "titleText", rankTitleGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(rankingUI, "userRankText", userRankGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(rankingUI, "backButton", rankBackBtn);

            // 9. Create PausePanel
            GameObject pauseGo = CreateUIPanel(canvasGo.transform, "PausePanel", new Color(0.05f, 0.06f, 0.09f, 0.82f));
            MyProject.UI.PauseUI pauseUI = pauseGo.AddComponent<MyProject.UI.PauseUI>();

            // Popup Card Container
            GameObject pauseCardGo = CreateUIPanel(pauseGo.transform, "PauseCard", new Color(0.12f, 0.14f, 0.22f, 0.96f));
            RectTransform pauseCardRect = pauseCardGo.GetComponent<RectTransform>();
            pauseCardRect.anchorMin = new Vector2(0.5f, 0.5f);
            pauseCardRect.anchorMax = new Vector2(0.5f, 0.5f);
            pauseCardRect.pivot = new Vector2(0.5f, 0.5f);
            pauseCardRect.sizeDelta = new Vector2(720, 560);

            GameObject pauseTitleGo = CreateUIText(pauseCardGo.transform, "PauseTitle", "GAME PAUSED", 56, FontStyle.Bold, new Color(1f, 0.85f, 0.3f, 1f), TextAnchor.MiddleCenter);
            RectTransform pauseTitleRect = pauseTitleGo.GetComponent<RectTransform>();
            pauseTitleRect.anchorMin = new Vector2(0.5f, 0.78f);
            pauseTitleRect.anchorMax = new Vector2(0.5f, 0.78f);
            pauseTitleRect.sizeDelta = new Vector2(650, 80);

            GameObject pauseSubGo = CreateUIText(pauseCardGo.transform, "PauseSub", "Take a breather!", 24, FontStyle.Italic, new Color(0.7f, 0.75f, 0.85f, 1f), TextAnchor.MiddleCenter);
            RectTransform pauseSubRect = pauseSubGo.GetComponent<RectTransform>();
            pauseSubRect.anchorMin = new Vector2(0.5f, 0.66f);
            pauseSubRect.anchorMax = new Vector2(0.5f, 0.66f);
            pauseSubRect.sizeDelta = new Vector2(650, 40);

            GameObject continueBtnGo = CreateUIButton(pauseCardGo.transform, "ContinueButton", "CONTINUE", 36, new Color(0.2f, 0.75f, 0.5f, 1f), Color.white);
            RectTransform continueBtnRect = continueBtnGo.GetComponent<RectTransform>();
            continueBtnRect.anchorMin = new Vector2(0.5f, 0.44f);
            continueBtnRect.anchorMax = new Vector2(0.5f, 0.44f);
            continueBtnRect.sizeDelta = new Vector2(480, 90);
            UnityEngine.UI.Button continueBtn = continueBtnGo.GetComponent<UnityEngine.UI.Button>();

            GameObject homeBtnGo = CreateUIButton(pauseCardGo.transform, "HomeButton", "HOME", 36, new Color(0.85f, 0.35f, 0.35f, 1f), Color.white);
            RectTransform homeBtnRect = homeBtnGo.GetComponent<RectTransform>();
            homeBtnRect.anchorMin = new Vector2(0.5f, 0.22f);
            homeBtnRect.anchorMax = new Vector2(0.5f, 0.22f);
            homeBtnRect.sizeDelta = new Vector2(480, 90);
            UnityEngine.UI.Button homeBtn = homeBtnGo.GetComponent<UnityEngine.UI.Button>();

            SetPrivateField(pauseUI, "pausePanel", pauseGo);
            SetPrivateField(pauseUI, "titleText", pauseTitleGo.GetComponent<UnityEngine.UI.Text>());
            SetPrivateField(pauseUI, "continueButton", continueBtn);
            SetPrivateField(pauseUI, "homeButton", homeBtn);

            // Link all to UIManager
            uiManager.SetReferences(mainMenuUI, hudUI, gameOverUI, shopUI, missionUI, rankingUI, pauseUI);

            // Hide panels by default (UIManager handles visibility state)
            pauseGo.SetActive(false);
            rankingGo.SetActive(false);
            missionGo.SetActive(false);
            shopGo.SetActive(false);
            gameOverGo.SetActive(false);
            hudGo.SetActive(false);
            mainMenuGo.SetActive(true);
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Color bgColor)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            UnityEngine.UI.Image img = panel.GetComponent<UnityEngine.UI.Image>();
            img.color = bgColor;
            if (bgColor.a <= 0.01f)
            {
                img.raycastTarget = false;
            }

            return panel;
        }

        private static GameObject CreateUIText(Transform parent, string name, string text, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
        {
            GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textGo.transform.SetParent(parent, false);

            UnityEngine.UI.Text txt = textGo.GetComponent<UnityEngine.UI.Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            txt.fontSize = fontSize;
            txt.fontStyle = fontStyle;
            txt.color = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            return textGo;
        }

        private static GameObject CreateUIButton(Transform parent, string name, string labelText, int fontSize, Color btnColor, Color textColor)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            btnGo.transform.SetParent(parent, false);

            UnityEngine.UI.Image img = btnGo.GetComponent<UnityEngine.UI.Image>();
            img.color = btnColor;

            UnityEngine.UI.Button btn = btnGo.GetComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;

            GameObject labelGo = CreateUIText(btnGo.transform, "Label", labelText, fontSize, FontStyle.Bold, textColor, TextAnchor.MiddleCenter);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return btnGo;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
