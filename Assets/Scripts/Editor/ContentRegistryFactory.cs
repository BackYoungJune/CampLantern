#if UNITY_EDITOR
using System.Linq;
using CampLantern.Core;
using UnityEditor;
using UnityEngine;

namespace CampLantern.EditorTools
{
    /// <summary>
    /// Assets/Data를 스캔해 모든 ItemDef(FishDef 등 파생 포함)·EstateObjectDef를 모아
    /// Assets/Resources/ContentRegistry.asset을 (재)생성한다.
    /// RULE-02: .asset 직접 작성 금지 — ScriptableObject.CreateInstance + AssetDatabase만 사용.
    /// 새 콘텐츠 데이터(.asset)를 추가한 뒤에는 반드시 재실행해야 저장/로드가 그 아이템을 인식한다.
    /// </summary>
    public static class ContentRegistryFactory
    {
        private const string k_folder = "Assets/Resources";
        private const string k_path   = k_folder + "/ContentRegistry.asset";

        [MenuItem("Tools/Make Assets/Content Registry (Rebuild)")]
        public static void Rebuild()
        {
            if (!AssetDatabase.IsValidFolder(k_folder))
                AssetDatabase.CreateFolder("Assets", "Resources");

            ItemDef[] items = AssetDatabase.FindAssets("t:ItemDef")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ItemDef>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(d => d != null)
                .ToArray();

            EstateObjectDef[] estateObjects = AssetDatabase.FindAssets("t:EstateObjectDef")
                .Select(guid => AssetDatabase.LoadAssetAtPath<EstateObjectDef>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(d => d != null)
                .ToArray();

            var registry = AssetDatabase.LoadAssetAtPath<ContentRegistry>(k_path);
            bool isNew = registry == null;
            if (isNew) registry = ScriptableObject.CreateInstance<ContentRegistry>();

            var so = new SerializedObject(registry);
            SetArray(so, "m_items", items);
            SetArray(so, "m_estateObjects", estateObjects);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (isNew) AssetDatabase.CreateAsset(registry, k_path);
            else EditorUtility.SetDirty(registry);

            AssetDatabase.SaveAssets();
            Debug.Log($"[MakeAssets] ContentRegistry 갱신 완료 — 아이템 {items.Length}종, 영지 오브젝트 {estateObjects.Length}종");
        }

        private static void SetArray(SerializedObject so, string fieldName, Object[] values)
        {
            var prop = so.FindProperty(fieldName);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
#endif
