#if UNITY_EDITOR
using CampLantern.Cooking;
using CampLantern.Core;
using CampLantern.Fishing;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CampLantern.EditorTools
{
    /// <summary>
    /// 낚시/요리 스테이션 프리팹 생성기 — FishingRod / FishingSpot / CookingPot.
    /// 지금까지 P0PlaySceneFactory·RoomScenesFactory가 씬마다 인라인으로 만들던 것을 재사용 가능한
    /// 프리팹으로 승격한다. 런타임 컴포넌트(CampLantern.Fishing/Cooking)는 이미 존재하므로 여기선
    /// 프리팹 조립만 한다. 데이터(어종 테이블·레시피·실패작)는 Assets/Data의 Def 에셋을 연결한다.
    ///
    /// RULE-02: .prefab 텍스트 직접 작성 금지 — PrefabUtility.SaveAsPrefabAsset만 사용.
    /// 비주얼은 P0 그레이박스 플레이스홀더(프리미티브). 실제 아트로 교체는 아트 파이프라인 확정 후.
    /// 콜라이더는 향후 VR 인터랙션용으로 미리 부착(P0 IMGUI 흐름엔 영향 없음, Rigidbody 없어 RULE-03 무관).
    /// </summary>
    public static class StationFactory
    {
        private const string k_folder         = "Assets/Prefabs/Stations";
        private const string k_materialFolder = "Assets/Prefabs/Materials";

        [MenuItem("Tools/Make Assets/Stations (Create All)")]
        public static void CreateAll() => CreateInternal(force: false);

        [MenuItem("Tools/Make Assets/Stations (Force Recreate)")]
        public static void ForceRecreate() => CreateInternal(force: true);

        private static void CreateInternal(bool force)
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(k_folder);
            EnsureFolder(k_materialFolder);

            CreateFishingRod(force);
            CreateFishingSpot(force);
            CreateCookingPot(force);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MakeAssets] 낚시/요리 스테이션 프리팹 생성 완료: " + k_folder);
        }

        // ── 낚싯대 ───────────────────────────────────────────────────
        // FishingRod 상태머신 + 낚싯대 비주얼 + 그랩 볼륨(BoxCollider). 데이터 참조 없음(어종은 Spot에서 추첨).
        private static void CreateFishingRod(bool force)
        {
            const string path = k_folder + "/FishingRod.prefab";
            if (Skip(path, force)) return;

            var root = new GameObject("FishingRod");
            try
            {
                root.AddComponent<FishingRod>();

                // 향후 VR 그랩용 볼륨 — 손잡이~로드 전체를 감싸는 얇은 박스
                var col = root.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.6f, 0.18f);
                col.size   = new Vector3(0.12f, 1.4f, 0.5f);

                // 낚싯대 비주얼 — 얇고 긴 원통을 앞으로 기울여 배치 (플레이스홀더)
                AddVisual(root, PrimitiveType.Cylinder, new Color(0.4f, 0.25f, 0.1f), "Mat_FishingRod",
                          localPos: new Vector3(0f, 0.7f, 0.2f),
                          localScale: new Vector3(0.03f, 0.75f, 0.03f),
                          localEuler: new Vector3(28f, 0f, 0f));

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        // ── 낚시터(포인트) ───────────────────────────────────────────
        // FishingSpot + 어종 테이블(3종) + 수면 마커 비주얼 + 낚시 존 트리거.
        private static void CreateFishingSpot(bool force)
        {
            const string path = k_folder + "/FishingSpot.prefab";
            if (Skip(path, force)) return;

            var root = new GameObject("FishingSpot");
            try
            {
                var spot = root.AddComponent<FishingSpot>();
                SetObjectArray(spot, "m_fishTable",
                    LoadRequired<FishDef>("Assets/Data/Fish/Fish_Crucian.asset"),
                    LoadRequired<FishDef>("Assets/Data/Fish/Fish_Trout.asset"),
                    LoadRequired<FishDef>("Assets/Data/Fish/Fish_GoldenCarp.asset"));

                // 낚시 가능 존 — 캐스팅 판정/근접 감지용 트리거 (VR 입력 어댑터에서 사용 예정)
                var col = root.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.center    = new Vector3(0f, 0.5f, 0f);
                col.size      = new Vector3(3f, 1f, 3f);

                // 수면 위 얕은 원반 마커 (연못 비주얼은 씬의 별도 오브젝트 — 여기선 위치 표식만)
                AddVisual(root, PrimitiveType.Cylinder, new Color(0.25f, 0.45f, 0.8f), "Mat_Water",
                          localPos: new Vector3(0f, 0.02f, 0f),
                          localScale: new Vector3(1.5f, 0.02f, 1.5f));

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        // ── 냄비 ─────────────────────────────────────────────────────
        // CookingPot + 레시피(3종) + 실패작 + 냄비 비주얼 + 인터랙션 콜라이더.
        // 런타임에 소유 Manager가 Initialize(inventory)로 Inventory를 주입한다(레시피는 이 프리팹 세팅 사용).
        private static void CreateCookingPot(bool force)
        {
            const string path = k_folder + "/CookingPot.prefab";
            if (Skip(path, force)) return;

            var root = new GameObject("CookingPot");
            try
            {
                var pot = root.AddComponent<CookingPot>();
                SetObjectArray(pot, "m_recipes",
                    LoadRequired<RecipeDef>("Assets/Data/Recipes/Recipe_GrilledFish.asset"),
                    LoadRequired<RecipeDef>("Assets/Data/Recipes/Recipe_TroutSteak.asset"),
                    LoadRequired<RecipeDef>("Assets/Data/Recipes/Recipe_GoldenCarpBraise.asset"));
                SetObjectRef(pot, "m_failResult",
                    LoadRequired<ItemDef>("Assets/Data/Items/Item_BurntFood.asset"));

                var col = root.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 0.4f, 0f);
                col.radius = 0.4f;
                col.height = 0.9f;

                AddVisual(root, PrimitiveType.Cylinder, new Color(0.3f, 0.3f, 0.35f), "Mat_Pot",
                          localPos: new Vector3(0f, 0.4f, 0f),
                          localScale: new Vector3(0.4f, 0.4f, 0.4f));

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        // ── 헬퍼 (P0PlaySceneFactory와 동일 패턴) ─────────────────────

        private static bool Skip(string path, bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.LogWarning($"[MakeAssets] 이미 존재합니다. Force Recreate 메뉴를 사용하세요: {path}");
                return true;
            }
            return false;
        }

        private static T LoadRequired<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new System.InvalidOperationException(
                    $"[MakeAssets] 필수 에셋 없음: {path} — 선행 팩토리(P0 Data) 먼저 실행 필요");
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        // 비주얼 자식 부착 — 프리미티브 자동 콜라이더 제거(스테이션 콜라이더는 루트에 둠).
        // 그레이박스 플레이스홀더라 비주얼 자식에 스케일/회전을 준다(콜라이더는 루트에서 별도 사이징).
        private static void AddVisual(GameObject parent, PrimitiveType shape, Color color, string matName,
                                      Vector3 localPos, Vector3 localScale, Vector3 localEuler = default)
        {
            var visual = GameObject.CreatePrimitive(shape);
            visual.name = "Visual";
            visual.transform.SetParent(parent.transform, false);
            visual.transform.localPosition = localPos;
            visual.transform.localRotation = Quaternion.Euler(localEuler);
            visual.transform.localScale    = localScale;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            SetMaterial(visual, color, matName);
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
            var prop = so.FindProperty(fieldName)
                       ?? throw new System.InvalidOperationException($"[MakeAssets] 필드 없음: {comp.GetType().Name}.{fieldName}");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(Component comp, string fieldName, params Object[] values)
        {
            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName)
                       ?? throw new System.InvalidOperationException($"[MakeAssets] 필드 없음: {comp.GetType().Name}.{fieldName}");
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
