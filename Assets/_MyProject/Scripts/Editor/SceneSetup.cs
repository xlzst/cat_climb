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
            string scenePath = "Assets/_MyProject/Scenes/PrototypeScene.unity";
            // Only auto-generate if the scene file doesn't exist
            if (!File.Exists(scenePath))
            {
                Debug.Log("PrototypeScene.unity not found. Auto-generating...");
                CreatePrototypeScene();
            }
        }

        [MenuItem("Tools/MyProject/Create Prototype Scene")]
        public static void CreatePrototypeSceneForce()
        {
            Debug.Log("Manually triggering Prototype Scene creation...");
            CreatePrototypeScene();
        }

        private static void CreatePrototypeScene()
        {
            Debug.Log("Starting Scene Setup...");

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

            // Programmatically generate white triangle texture (pointing up) for Enemy
            string trianglePath = Path.Combine(spritesDir, "EnemyTriangle.png");
            if (!File.Exists(trianglePath))
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
                            float distFromEdge = halfWidthAtY - Mathf.Abs(x - center);
                            float alpha = Mathf.Clamp01(distFromEdge);
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
                File.WriteAllBytes(trianglePath, triangleBytes);
                Debug.Log($"Generated triangle texture at: {trianglePath}");
                Object.DestroyImmediate(triangleTex);
            }

            // Force import assets
            AssetDatabase.Refresh();

            // Set import settings to Sprite (2D and UI)
            SetAsSprite(circlePath);
            SetAsSprite(squarePath);
            SetAsSprite(trianglePath);

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
            }

            // Destroy Directional Light (not needed for 2D UI/URP sprite setup without lighting)
            GameObject lightGo = GameObject.Find("Directional Light");
            if (lightGo != null)
            {
                Object.DestroyImmediate(lightGo);
            }

            // Load generated Sprites
            Sprite catSprite = AssetDatabase.LoadAssetAtPath<Sprite>(circlePath);
            Sprite wallSprite = AssetDatabase.LoadAssetAtPath<Sprite>(squarePath);
            Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>(trianglePath);

            if (catSprite == null || wallSprite == null || enemySprite == null)
            {
                Debug.LogError("Failed to load generated sprites. Please check import settings.");
                return;
            }

            // 1080x1920 (9:16) Viewport Calculations:
            // Camera orthographicSize = 5, which means height is 10 units.
            // Width = height * (9 / 16) = 5.625 units.
            // Screen boundaries: Left/Right at X = +/- 2.8125, Bottom at Y = -5.0.

            // Create Cat (Circle)
            GameObject catGo = new GameObject("Cat");
            catGo.transform.position = new Vector3(0f, -4.55f, 0f);
            catGo.transform.localScale = new Vector3(0.5f, 0.5f, 1f); // Set to half-size
            SpriteRenderer catRenderer = catGo.AddComponent<SpriteRenderer>();
            catRenderer.sprite = catSprite;
            catRenderer.color = new Color(1f, 0.45f, 0.45f, 1f); // Vibrant light coral pink
            
            Rigidbody2D catRb = catGo.AddComponent<Rigidbody2D>();
            catRb.bodyType = RigidbodyType2D.Dynamic; // Explicitly dynamic to be affected by gravity
            catRb.gravityScale = 1f; // Affected by standard gravity
            catRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            catRb.constraints = RigidbodyConstraints2D.FreezeRotation;

            CircleCollider2D catCollider = catGo.AddComponent<CircleCollider2D>();
            catCollider.radius = 0.5f; // Matches sprite bounds (scale will make it 0.25 in world space)

            // Attach PlayerController runtime component
            catGo.AddComponent<PlayerController>();

            // Create Left Wall (Rectangle)
            GameObject leftWallGo = new GameObject("LeftWall");
            leftWallGo.transform.position = new Vector3(-2.8125f, 0f, 0f);
            leftWallGo.transform.localScale = new Vector3(0.4f, 10f, 1f);
            SpriteRenderer leftWallRenderer = leftWallGo.AddComponent<SpriteRenderer>();
            leftWallRenderer.sprite = wallSprite;
            leftWallRenderer.color = new Color(0.24f, 0.28f, 0.36f, 1f); // Sleek grey-blue

            BoxCollider2D leftWallCollider = leftWallGo.AddComponent<BoxCollider2D>();

            // Create Right Wall (Rectangle)
            GameObject rightWallGo = new GameObject("RightWall");
            rightWallGo.transform.position = new Vector3(2.8125f, 0f, 0f);
            rightWallGo.transform.localScale = new Vector3(0.4f, 10f, 1f);
            SpriteRenderer rightWallRenderer = rightWallGo.AddComponent<SpriteRenderer>();
            rightWallRenderer.sprite = wallSprite;
            rightWallRenderer.color = new Color(0.24f, 0.28f, 0.36f, 1f);

            BoxCollider2D rightWallCollider = rightWallGo.AddComponent<BoxCollider2D>();

            // Create Floor (Rectangle)
            GameObject floorGo = new GameObject("Floor");
            floorGo.transform.position = new Vector3(0f, -5.0f, 0f);
            floorGo.transform.localScale = new Vector3(6f, 0.4f, 1f);
            SpriteRenderer floorRenderer = floorGo.AddComponent<SpriteRenderer>();
            floorRenderer.sprite = wallSprite;
            floorRenderer.color = new Color(0.24f, 0.28f, 0.36f, 1f);

            BoxCollider2D floorCollider = floorGo.AddComponent<BoxCollider2D>();

            // Create Platform Manager
            GameObject platformManagerGo = new GameObject("PlatformManager");
            PlatformManager platformManager = platformManagerGo.AddComponent<PlatformManager>();
            platformManager.enemySprite = enemySprite;

            // Create Wall Manager
            GameObject wallManagerGo = new GameObject("WallManager");
            WallManager wallManager = wallManagerGo.AddComponent<WallManager>();
            wallManager.spikeSprite = enemySprite;

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



            // Save Scene
            string scenePath = Path.Combine(scenesDir, "PrototypeScene.unity");
            bool saveResult = EditorSceneManager.SaveScene(scene, scenePath);
            if (saveResult)
            {
                Debug.Log($"Successfully created and saved prototype scene at: {scenePath}");
            }
            else
            {
                Debug.LogError($"Failed to save prototype scene at: {scenePath}");
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
                    Debug.LogWarning("Could not find GameViewSizes type.");
                    return;
                }

                var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instanceProp = singleType.GetProperty("instance");
                if (instanceProp == null)
                {
                    Debug.LogWarning("Could not find instance property on ScriptableSingleton<GameViewSizes>.");
                    return;
                }

                var sizesInstance = instanceProp.GetValue(null, null);
                if (sizesInstance == null)
                {
                    Debug.LogWarning("Could not get instance of GameViewSizes.");
                    return;
                }

                var currentGroupProp = sizesType.GetProperty("currentGroup");
                if (currentGroupProp == null)
                {
                    Debug.LogWarning("Could not find currentGroup property on GameViewSizes.");
                    return;
                }

                var currentGroup = currentGroupProp.GetValue(sizesInstance, null);
                if (currentGroup == null)
                {
                    Debug.LogWarning("Could not get currentGroup from GameViewSizes.");
                    return;
                }

                var groupType = currentGroup.GetType();
                var getGameViewSizeMethod = groupType.GetMethod("GetGameViewSize", new System.Type[] { typeof(int) });
                var getRowCountMethod = groupType.GetMethod("GetRowCount");
                if (getGameViewSizeMethod == null || getRowCountMethod == null)
                {
                    Debug.LogWarning("Could not find GetGameViewSize or GetRowCount methods.");
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
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                    Debug.Log($"Set import settings to Sprite for: {path}");
                }
            }
            else
            {
                Debug.LogWarning($"Could not find TextureImporter for asset at: {path}");
            }
        }
    }
}
