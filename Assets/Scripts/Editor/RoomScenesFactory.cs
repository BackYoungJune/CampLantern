#if UNITY_EDITOR
using System.Collections.Generic;
using CampLantern.Bootstrap;
using CampLantern.Cooking;
using CampLantern.Core;
using CampLantern.Estate;
using CampLantern.Fishing;
using CampLantern.Networking;
using CampLantern.Networking.Voice;
using Fusion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CampLantern.EditorTools
{
    /// <summary>
    /// 로비/낚시터/사냥터(존A)/영지 4개 공간 씬 생성기 (room-architecture.md 공간 정의 그대로 착수 —
    /// 사용자가 P0 게이트를 넘어 명시적으로 요청함, 2026-07-07). RULE-02: .unity 직접 작성 금지 —
    /// EditorSceneManager API로만 생성. 매칭/샤딩/영지 소유 인증은 백엔드 미정이라 각 씬은
    /// 고정 이름 Room 접속만 지원 (SessionLauncher.StartSession 참조).
    /// </summary>
    public static class RoomScenesFactory
    {
        private const string k_lobbyScenePath   = "Assets/Scenes/Lobby.unity";
        private const string k_fishingScenePath = "Assets/Scenes/FishingGround.unity";
        private const string k_huntScenePath    = "Assets/Scenes/HuntZone_A.unity";
        private const string k_estateScenePath  = "Assets/Scenes/EstateTemplate.unity";
        private const string k_materialFolder   = "Assets/Prefabs/Materials";

        [MenuItem("Tools/Make Assets/Room Scenes (Create All)")]
        public static void CreateAll()
        {
            CreateLobby();
            CreateFishingGround();
            CreateHuntZone();
            CreateEstateTemplate();
            Debug.Log("[MakeAssets] 로비/낚시터/사냥터_존A/영지_템플릿 생성 완료");
        }

        // ── 로비 ─────────────────────────────────────────────────────

        private static void CreateLobby()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(k_lobbyScenePath) != null) return;

            Scene scene = BeginNewScene();

            var camera = GameObject.Find("Main Camera");
            camera.transform.SetPositionAndRotation(new Vector3(0f, 3f, -6f), Quaternion.Euler(15f, 0f, 0f));

            CreateGround(new Color(0.5f, 0.5f, 0.55f), "Mat_LobbyGround");

            var harnessGo = new GameObject("LobbyHarness");
            var harness = harnessGo.AddComponent<LobbyHarness>();
            SetString(harness, "m_fishingSceneName", "FishingGround");
            SetString(harness, "m_huntSceneName", "HuntZone_A");
            SetString(harness, "m_estateSceneName", "EstateTemplate");

            SaveSceneAndRegister(scene, k_lobbyScenePath);
        }

        // ── 낚시터 ────────────────────────────────────────────────────

        private static void CreateFishingGround()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(k_fishingScenePath) != null) return;

            Scene scene = BeginNewScene();

            var camera = GameObject.Find("Main Camera");
            camera.transform.SetPositionAndRotation(new Vector3(0f, 6f, -8f), Quaternion.Euler(30f, 0f, 0f));

            CreateGround(new Color(0.35f, 0.55f, 0.3f), "Mat_FishingGround");

            var pond = GameObject.CreatePrimitive(PrimitiveType.Plane);
            pond.name = "Pond";
            pond.transform.position   = new Vector3(0f, 0.02f, 2f);
            pond.transform.localScale = new Vector3(1f, 1f, 1f); // 10x10m
            SetMaterial(pond, new Color(0.25f, 0.45f, 0.8f), "Mat_Water");

            var spotGo = new GameObject("FishingSpot");
            spotGo.transform.position = pond.transform.position;
            var spot = spotGo.AddComponent<FishingSpot>();
            SetObjectArray(spot, "m_fishTable",
                LoadRequired<FishDef>("Assets/Data/Fish/Fish_Crucian.asset"),
                LoadRequired<FishDef>("Assets/Data/Fish/Fish_Trout.asset"),
                LoadRequired<FishDef>("Assets/Data/Fish/Fish_GoldenCarp.asset"));

            var rodGo = new GameObject("FishingRod");
            rodGo.transform.position = new Vector3(0f, 0f, -1f);
            var rod = rodGo.AddComponent<FishingRod>();

            var networkGo = new GameObject("Network");
            var launcher = networkGo.AddComponent<SessionLauncher>();

            var harnessGo = new GameObject("FishingGroundHarness");
            var harness = harnessGo.AddComponent<FishingGroundHarness>();
            SetObjectRef(harness, "m_rod", rod);
            SetObjectRef(harness, "m_spot", spot);
            SetObjectRef(harness, "m_launcher", launcher);
            SetString(harness, "m_lobbySceneName", "Lobby");
            SetString(harness, "m_shardId", "shard0");

            SaveSceneAndRegister(scene, k_fishingScenePath);
        }

        // ── 사냥터 (존 A) ─────────────────────────────────────────────

        private static void CreateHuntZone()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(k_huntScenePath) != null) return;

            Scene scene = BeginNewScene();

            var camera = GameObject.Find("Main Camera");
            camera.transform.SetPositionAndRotation(new Vector3(0f, 8f, -10f), Quaternion.Euler(30f, 0f, 0f));

            CreateGround(new Color(0.45f, 0.4f, 0.3f), "Mat_HuntGround");

            var networkGo = new GameObject("Network");
            var launcher = networkGo.AddComponent<SessionLauncher>();
            var voice    = networkGo.AddComponent<VoiceController>();
            networkGo.AddComponent<PlayerMute>();
            SetObjectRef(voice, "m_voicePlayerPrefab",
                LoadRequired<NetworkObject>("Assets/Prefabs/VoicePlayer.prefab"));

            var harnessGo = new GameObject("HuntZoneHarness");
            var harness = harnessGo.AddComponent<HuntZoneHarness>();
            SetObjectRef(harness, "m_launcher", launcher);
            SetObjectRef(harness, "m_huntTargetPrefab",
                LoadRequired<GameObject>("Assets/Prefabs/HuntTarget.prefab").GetComponent<NetworkObject>());
            SetString(harness, "m_lobbySceneName", "Lobby");
            SetString(harness, "m_zoneId", "a");

            SaveSceneAndRegister(scene, k_huntScenePath);
        }

        // ── 영지 템플릿 ───────────────────────────────────────────────

        private static void CreateEstateTemplate()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(k_estateScenePath) != null) return;

            Scene scene = BeginNewScene();

            var camera = GameObject.Find("Main Camera");
            camera.transform.SetPositionAndRotation(new Vector3(0f, 8f, -10f), Quaternion.Euler(30f, 0f, 0f));

            CreateGround(new Color(0.35f, 0.55f, 0.3f), "Mat_EstateGround");

            var potGo = new GameObject("CookingPot");
            potGo.transform.position = new Vector3(0f, 0f, 1f);
            AddVisual(potGo, PrimitiveType.Cylinder, new Color(0.3f, 0.3f, 0.35f), "Mat_Pot",
                      localPos: new Vector3(0f, 0.5f, 0f));
            var pot = potGo.AddComponent<CookingPot>();
            SetObjectArray(pot, "m_recipes",
                LoadRequired<RecipeDef>("Assets/Data/Recipes/Recipe_GrilledFish.asset"),
                LoadRequired<RecipeDef>("Assets/Data/Recipes/Recipe_TroutSteak.asset"),
                LoadRequired<RecipeDef>("Assets/Data/Recipes/Recipe_GoldenCarpBraise.asset"));
            SetObjectRef(pot, "m_failResult", LoadRequired<ItemDef>("Assets/Data/Items/Item_BurntFood.asset"));

            var estateGo = new GameObject("EstateManager");
            var estateManager = estateGo.AddComponent<EstateManager>();

            var networkGo = new GameObject("Network");
            var launcher = networkGo.AddComponent<SessionLauncher>();

            var harnessGo = new GameObject("EstateHarness");
            var harness = harnessGo.AddComponent<EstateHarness>();
            SetObjectRef(harness, "m_estateManager", estateManager);
            SetObjectRef(harness, "m_pot", pot);
            SetObjectRef(harness, "m_launcher", launcher);
            SetString(harness, "m_lobbySceneName", "Lobby");
            SetObjectArray(harness, "m_estateCatalog",
                LoadRequired<EstateObjectDef>("Assets/Data/Estate/Estate_Tent.asset"),
                LoadRequired<EstateObjectDef>("Assets/Data/Estate/Estate_Lantern.asset"),
                LoadRequired<EstateObjectDef>("Assets/Data/Estate/Estate_CampChair.asset"),
                LoadRequired<EstateObjectDef>("Assets/Data/Estate/Estate_Planter.asset"),
                LoadRequired<EstateObjectDef>("Assets/Data/Estate/Estate_Deck.asset"),
                LoadRequired<EstateObjectDef>("Assets/Data/Estate/Estate_Campfire.asset"));

            SaveSceneAndRegister(scene, k_estateScenePath);
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────

        private static Scene BeginNewScene()
        {
            EditorSceneManager.SaveOpenScenes();
            return EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        private static void CreateGround(Color color, string matName)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f); // 40x40m
            SetMaterial(ground, color, matName);
        }

        private static void SaveSceneAndRegister(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static T LoadRequired<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new System.InvalidOperationException(
                    $"[MakeAssets] 필수 에셋 없음: {path} — 선행 팩토리(P0 Data / Voice Player / P0 Play Scene) 먼저 실행 필요");
            return asset;
        }

        private static GameObject AddVisual(GameObject parent, PrimitiveType shape, Color color, string matName,
                                            Vector3 localPos = default)
        {
            var visual = GameObject.CreatePrimitive(shape);
            visual.name = "Visual";
            visual.transform.SetParent(parent.transform, false);
            visual.transform.localPosition = localPos;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            SetMaterial(visual, color, matName);
            return visual;
        }

        private static void SetMaterial(GameObject go, Color color, string matName)
        {
            string matPath = $"{k_materialFolder}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = GraphicsSettings.currentRenderPipeline != null
                    ? Shader.Find("Universal Render Pipeline/Lit")
                    : Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader) { color = color };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void SetObjectRef(Component comp, string fieldName, Object value)
        {
            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
                throw new System.InvalidOperationException($"[MakeAssets] 필드 없음: {comp.GetType().Name}.{fieldName}");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(Component comp, string fieldName, params Object[] values)
        {
            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
                throw new System.InvalidOperationException($"[MakeAssets] 필드 없음: {comp.GetType().Name}.{fieldName}");
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Component comp, string fieldName, string value)
        {
            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
                throw new System.InvalidOperationException($"[MakeAssets] 필드 없음: {comp.GetType().Name}.{fieldName}");
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
