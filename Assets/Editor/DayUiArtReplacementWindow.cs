#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class DayUiArtReplacementWindow : EditorWindow
{
    private const string DayScenePath = "Assets/Scenes/DayScene.unity";
    private const string PrefabSearchRoot = "Assets/Prefabs";

    private enum SceneSource
    {
        OpenScenes,
        DayScene
    }

    private readonly List<UiSlot> slots = new List<UiSlot>();
    private readonly List<UnityEngine.Object> dropAssets = new List<UnityEngine.Object>();

    private SceneSource sceneSource = SceneSource.OpenScenes;
    private Vector2 scroll;
    private string searchText = string.Empty;
    private bool includeInactive = true;
    private bool pingAfterApply = true;

    [MenuItem("Tools/Day UI/Art Replacement Tool")]
    public static void Open()
    {
        DayUiArtReplacementWindow window = GetWindow<DayUiArtReplacementWindow>("Day UI Art");
        window.minSize = new Vector2(780f, 480f);
        window.ScanSceneSlots();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawDropArea();
        DrawBulkActions();
        DrawSlotList();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                sceneSource = (SceneSource)EditorGUILayout.EnumPopup("Scene Target", sceneSource);
                includeInactive = EditorGUILayout.ToggleLeft("Include inactive", includeInactive, GUILayout.Width(130f));
                pingAfterApply = EditorGUILayout.ToggleLeft("Ping applied", pingAfterApply, GUILayout.Width(100f));

                if (GUILayout.Button("Scan Scene UI", GUILayout.Width(130f)))
                    ScanSceneSlots();
            }

            searchText = EditorGUILayout.TextField("Filter", searchText);
        }
    }

    private void DrawDropArea()
    {
        Rect dropRect = GUILayoutUtility.GetRect(0f, 58f, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "Drop sprites, textures, or UI prefabs here for name-matched replacement", EditorStyles.helpBox);

        Event evt = Event.current;
        if (!dropRect.Contains(evt.mousePosition))
            return;

        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (UnityEngine.Object asset in DragAndDrop.objectReferences)
                {
                    if (IsSupportedReplacement(asset) && !dropAssets.Contains(asset))
                        dropAssets.Add(asset);
                }
            }

            evt.Use();
        }
    }

    private void DrawBulkActions()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Dropped Assets", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear", GUILayout.Width(72f)))
                    dropAssets.Clear();
            }

            if (dropAssets.Count == 0)
            {
                EditorGUILayout.LabelField("No assets dropped.", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < dropAssets.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        dropAssets[i] = EditorGUILayout.ObjectField(dropAssets[i], typeof(UnityEngine.Object), false);
                        if (GUILayout.Button("X", GUILayout.Width(24f)))
                        {
                            dropAssets.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = dropAssets.Count > 0;

                if (GUILayout.Button("Apply Dropped Assets To Scanned Scene By Name"))
                    ApplyDroppedAssetsToScannedScene();

                if (GUILayout.Button("Apply Dropped Assets To UI Prefabs By Name"))
                    ApplyDroppedAssetsToPrefabs();

                GUI.enabled = true;
            }
        }
    }

    private void DrawSlotList()
    {
        IEnumerable<UiSlot> filteredSlots = slots;
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string filter = searchText.Trim();
            filteredSlots = filteredSlots.Where(slot =>
                slot.Path.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || slot.Kind.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Scanned Slots: " + slots.Count, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (UiSlot slot in filteredSlots)
        {
            if (slot == null || slot.Target == null)
                continue;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(slot.Target, typeof(GameObject), true, GUILayout.Width(210f));

                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(slot.Path, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(slot.Kind, EditorStyles.miniLabel);
                    }

                    slot.PendingReplacement = EditorGUILayout.ObjectField(
                        slot.PendingReplacement,
                        typeof(UnityEngine.Object),
                        false,
                        GUILayout.Width(190f));

                    GUI.enabled = IsSupportedReplacement(slot.PendingReplacement);
                    if (GUILayout.Button("Apply", GUILayout.Width(72f)))
                        ApplyReplacementToSlot(slot, slot.PendingReplacement);
                    GUI.enabled = true;
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void ScanSceneSlots()
    {
        if (sceneSource == SceneSource.DayScene)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(DayScenePath, OpenSceneMode.Single);
        }

        slots.Clear();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Canvas[] canvases = root.GetComponentsInChildren<Canvas>(includeInactive);
                foreach (Canvas canvas in canvases)
                    AddCanvasSlots(canvas);
            }
        }

        slots.Sort((a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));
        Repaint();
    }

    private void AddCanvasSlots(Canvas canvas)
    {
        RectTransform[] rects = canvas.GetComponentsInChildren<RectTransform>(includeInactive);
        foreach (RectTransform rect in rects)
        {
            if (rect == null || rect.transform == canvas.transform)
                continue;

            string kind = GetUiKind(rect.gameObject);
            if (string.IsNullOrEmpty(kind))
                continue;

            slots.Add(new UiSlot
            {
                Target = rect.gameObject,
                RectTransform = rect,
                Path = GetTransformPath(rect.transform, canvas.transform),
                Kind = kind
            });
        }
    }

    private void ApplyDroppedAssetsToScannedScene()
    {
        int applied = 0;
        foreach (UnityEngine.Object asset in dropAssets)
        {
            UiSlot match = FindBestSlotMatch(asset, slots);
            if (match == null)
                continue;

            if (ApplyReplacementToSlot(match, asset))
                applied++;
        }

        Debug.Log("Day UI Art Replacement: applied " + applied + " dropped asset(s) to scanned scene UI.");
    }

    private void ApplyDroppedAssetsToPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabSearchRoot });
        int applied = 0;
        int changedPrefabs = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;

            try
            {
                List<UiSlot> prefabSlots = CollectPrefabSlots(root);
                foreach (UnityEngine.Object asset in dropAssets)
                {
                    UiSlot match = FindBestSlotMatch(asset, prefabSlots);
                    if (match == null)
                        continue;

                    if (ApplyReplacementToSlot(match, asset, root))
                    {
                        applied++;
                        changed = true;
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changedPrefabs++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Day UI Art Replacement: applied " + applied + " replacement(s) across " + changedPrefabs + " prefab(s).");
    }

    private static List<UiSlot> CollectPrefabSlots(GameObject root)
    {
        List<UiSlot> result = new List<UiSlot>();
        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform rect in rects)
        {
            string kind = GetUiKind(rect.gameObject);
            if (string.IsNullOrEmpty(kind))
                continue;

            result.Add(new UiSlot
            {
                Target = rect.gameObject,
                RectTransform = rect,
                Path = GetTransformPath(rect.transform, root.transform),
                Kind = kind
            });
        }

        return result;
    }

    private bool ApplyReplacementToSlot(UiSlot slot, UnityEngine.Object replacement, GameObject referenceRoot = null)
    {
        if (slot == null || slot.Target == null || !IsSupportedReplacement(replacement))
            return false;

        Sprite sprite = ResolveSprite(replacement, true);
        if (sprite != null)
            return ApplySprite(slot.Target, sprite);

        GameObject prefab = replacement as GameObject;
        if (prefab == null)
            return false;

        return ReplaceWithPrefab(slot.Target, prefab, referenceRoot);
    }

    private bool ApplySprite(GameObject target, Sprite sprite)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning("Day UI Art Replacement: target has no Image component: " + target.name, target);
            return false;
        }

        Undo.RecordObject(image, "Replace UI Sprite");
        image.sprite = sprite;
        EditorUtility.SetDirty(image);

        if (pingAfterApply)
            EditorGUIUtility.PingObject(target);

        return true;
    }

    private bool ReplaceWithPrefab(GameObject oldObject, GameObject prefab, GameObject referenceRoot)
    {
        RectTransform oldRect = oldObject.GetComponent<RectTransform>();
        Transform parent = oldObject.transform.parent;
        if (oldRect == null || parent == null)
            return false;

        int siblingIndex = oldObject.transform.GetSiblingIndex();
        bool wasActive = oldObject.activeSelf;
        string oldName = oldObject.name;

        GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        if (newObject == null)
            newObject = Instantiate(prefab, parent);

        Undo.RegisterCreatedObjectUndo(newObject, "Replace UI Prefab");
        newObject.name = oldName;
        newObject.SetActive(wasActive);
        newObject.transform.SetSiblingIndex(siblingIndex);

        RectTransform newRect = newObject.GetComponent<RectTransform>();
        if (newRect == null)
            newRect = newObject.AddComponent<RectTransform>();

        CopyRectTransform(oldRect, newRect);
        EnsureCoreComponents(oldObject, newObject);
        RebindObjectReferences(oldObject, newObject, referenceRoot);

        Undo.DestroyObjectImmediate(oldObject);
        EditorUtility.SetDirty(newObject);

        if (referenceRoot == null && newObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(newObject.scene);

        if (pingAfterApply)
            EditorGUIUtility.PingObject(newObject);

        if (referenceRoot == null)
            ScanSceneSlots();

        return true;
    }

    private static void CopyRectTransform(RectTransform source, RectTransform destination)
    {
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.anchoredPosition = source.anchoredPosition;
        destination.sizeDelta = source.sizeDelta;
        destination.pivot = source.pivot;
        destination.offsetMin = source.offsetMin;
        destination.offsetMax = source.offsetMax;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;
    }

    private static void EnsureCoreComponents(GameObject oldObject, GameObject newObject)
    {
        CopyComponentIfMissing<Image>(oldObject, newObject);
        CopyComponentIfMissing<Button>(oldObject, newObject);
        CopyComponentIfMissing<CanvasGroup>(oldObject, newObject);
        CopyComponentIfMissing<LayoutElement>(oldObject, newObject);
    }

    private static void CopyComponentIfMissing<T>(GameObject oldObject, GameObject newObject) where T : Component
    {
        T oldComponent = oldObject.GetComponent<T>();
        if (oldComponent == null || newObject.GetComponent<T>() != null)
            return;

        T newComponent = newObject.AddComponent<T>();
        EditorUtility.CopySerialized(oldComponent, newComponent);
    }

    private static void RebindObjectReferences(GameObject oldObject, GameObject newObject, GameObject referenceRoot)
    {
        GameObject root = referenceRoot;
        if (root == null)
            root = FindTopRoot(newObject);

        Component[] components = root.GetComponentsInChildren<Component>(true);
        foreach (Component component in components)
        {
            if (component == null)
                continue;

            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.GetIterator();
            bool changed = false;

            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                UnityEngine.Object value = property.objectReferenceValue;
                if (value == null)
                    continue;

                if (value == oldObject)
                {
                    property.objectReferenceValue = newObject;
                    changed = true;
                    continue;
                }

                Component oldComponent = value as Component;
                if (oldComponent == null || oldComponent.gameObject != oldObject)
                    continue;

                Component newComponent = newObject.GetComponent(oldComponent.GetType());
                if (newComponent != null)
                {
                    property.objectReferenceValue = newComponent;
                    changed = true;
                }
            }

            if (changed)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(component);
            }
        }
    }

    private static GameObject FindTopRoot(GameObject target)
    {
        Transform current = target.transform;
        while (current.parent != null)
            current = current.parent;

        return current.gameObject;
    }

    private static UiSlot FindBestSlotMatch(UnityEngine.Object asset, List<UiSlot> candidates)
    {
        if (asset == null)
            return null;

        string assetKey = NormalizeName(Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(asset)));
        if (string.IsNullOrEmpty(assetKey))
            assetKey = NormalizeName(asset.name);

        UiSlot exact = candidates.FirstOrDefault(slot => NormalizeName(slot.Target.name) == assetKey);
        if (exact != null)
            return exact;

        return candidates.FirstOrDefault(slot =>
        {
            string slotKey = NormalizeName(slot.Target.name);
            return slotKey.Contains(assetKey) || assetKey.Contains(slotKey);
        });
    }

    private static string GetUiKind(GameObject target)
    {
        List<string> kinds = new List<string>();

        if (target.GetComponent<Image>() != null)
            kinds.Add("Image");
        if (target.GetComponent<Button>() != null)
            kinds.Add("Button");
        if (target.GetComponent<TMP_Text>() != null)
            kinds.Add("TMP Text");
        if (target.GetComponent<CanvasGroup>() != null)
            kinds.Add("CanvasGroup");
        if (target.GetComponent<LayoutGroup>() != null || target.GetComponent<LayoutElement>() != null)
            kinds.Add("Layout");

        string name = target.name.ToLowerInvariant();
        if (name.Contains("panel") || name.Contains("group") || name.Contains("board") || name.Contains("box"))
            kinds.Add("Container");

        return kinds.Count == 0 ? string.Empty : string.Join(", ", kinds);
    }

    private static bool IsSupportedReplacement(UnityEngine.Object asset)
    {
        return asset is GameObject || ResolveSprite(asset, false) != null || asset is Texture2D;
    }

    private static Sprite ResolveSprite(UnityEngine.Object asset, bool allowImportFix)
    {
        if (asset == null)
            return null;

        Sprite sprite = asset as Sprite;
        if (sprite != null)
            return sprite;

        Texture2D texture = asset as Texture2D;
        if (texture == null)
            return null;

        string path = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(path))
            return null;

        Sprite importedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (importedSprite != null || !allowImportFix)
            return importedSprite;

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return null;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static string GetTransformPath(Transform transform, Transform stopAt)
    {
        Stack<string> names = new Stack<string>();
        Transform current = transform;

        while (current != null && current != stopAt)
        {
            names.Push(current.name);
            current = current.parent;
        }

        if (stopAt != null)
            names.Push(stopAt.name);

        return string.Join("/", names.ToArray());
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        char[] chars = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();

        return new string(chars);
    }

    private sealed class UiSlot
    {
        public GameObject Target;
        public RectTransform RectTransform;
        public string Path;
        public string Kind;
        public UnityEngine.Object PendingReplacement;
    }
}
#endif
