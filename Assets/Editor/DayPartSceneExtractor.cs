#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DayPartSceneExtractor
{
    private const string DayScenePath = "Assets/Scenes/DayScene.unity";
    private const string PrefabFolder = "Assets/Prefabs/DayParts";
    private const string SceneFolder = "Assets/Scenes/DayParts";

    [MenuItem("Tools/Day Parts/Extract Panels To Prefabs And Scenes")]
    public static void ExtractPanelsToPrefabsAndScenes()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);
        EnsureFolder(SceneFolder);

        Scene dayScene = EditorSceneManager.OpenScene(DayScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("DayScene Canvas not found.");
            return;
        }

        GameObject customerPanel = FindRequiredChild(canvas.transform, "CustomerPanel");
        GameObject kitchenPanel = FindRequiredChild(canvas.transform, "KitchenPanel");
        GameObject resultPanel = FindRequiredChild(canvas.transform, "ResultPanel");

        customerPanel.SetActive(true);
        kitchenPanel.SetActive(true);
        resultPanel.SetActive(true);
        BakeRuntimeUiObjects();
        BakeDesignerUiObjects(customerPanel, kitchenPanel, resultPanel);

        SavePanelPrefab(customerPanel, "CustomerPanel");
        SavePanelPrefab(kitchenPanel, "KitchenPanel");
        SavePanelPrefab(resultPanel, "ResultPanel");

        customerPanel.SetActive(true);
        kitchenPanel.SetActive(false);
        resultPanel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(dayScene);
        EditorSceneManager.SaveScene(dayScene);

        CreateEditScene("CustomerPanelScene", "CustomerPanel");
        CreateEditScene("KitchenPanelScene", "KitchenPanel");
        CreateEditScene("ResultPanelScene", "ResultPanel");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Day part panels extracted to prefabs and edit scenes.");
    }

    private static void BakeRuntimeUiObjects()
    {
        DayUIManager manager = Object.FindAnyObjectByType<DayUIManager>();
        if (manager == null)
        {
            Debug.LogWarning("DayUIManager not found. Prefabs will be created from existing scene objects only.");
            return;
        }

        InvokePrivate(manager, "ApplyColorPreset");
        InvokePrivate(manager, "ApplyTypographyPreset");
        InvokePrivate(manager, "ApplyViewportFitPreset");
        InvokePrivate(manager, "ApplyCustomerOrderLayout");
        InvokePrivate(manager, "ApplyKitchenPrepLayout");
        InvokePrivate(manager, "ApplyResultLayout");
        InvokePrivate(manager, "ApplyMenuBoardLayout");
        InvokePrivate(manager, "ApplyIndieUiPolish");
        InvokePrivate(manager, "ApplyTextPlacementPolish");
    }

    private static void InvokePrivate(DayUIManager manager, string methodName)
    {
        MethodInfo method = typeof(DayUIManager).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            Debug.LogWarning("Missing DayUIManager method: " + methodName);
            return;
        }

        method.Invoke(manager, null);
    }

    private static void BakeDesignerUiObjects(GameObject customerPanel, GameObject kitchenPanel, GameObject resultPanel)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/GALMURI11_TMP.asset");

        EnsurePaperFrame(customerPanel.transform, "오늘의 한식", new Color32(165, 49, 35, 255), font);
        EnsurePaperFrame(kitchenPanel.transform, "주방 조리대", new Color32(128, 70, 34, 255), font);
        EnsurePaperFrame(resultPanel.transform, "식사 평가", new Color32(151, 57, 39, 255), font);

        Transform portraitPanel = FindChildRecursive(customerPanel.transform, "CustomerPortraitPanel");
        Transform speechPanel = FindChildRecursive(customerPanel.transform, "CustomerSpeechPanel");
        Transform bottomPanel = FindChildRecursive(customerPanel.transform, "BottomPanel");
        Transform menuBoardPanel = FindChildRecursive(customerPanel.transform, "MenuBoardPanel");

        EnsureLabel(portraitPanel, "SectionLabel_Customer", "방문 손님", new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.94f), font);
        EnsureLabel(speechPanel, "SectionLabel_Speech", "손님 메모", new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.94f), font);
        EnsureDivider(speechPanel, "SectionRule_Speech", new Vector2(0.06f, 0.79f), new Vector2(0.94f, 0.805f));
        EnsureLabel(bottomPanel, "SectionLabel_Order", "주문 상담", new Vector2(0.04f, 0.86f), new Vector2(0.70f, 0.96f), font);
        EnsureDivider(bottomPanel, "SectionRule_Order", new Vector2(0.04f, 0.83f), new Vector2(0.70f, 0.845f));

        if (menuBoardPanel != null)
            EnsurePaperFrame(menuBoardPanel, "한식 메뉴판", new Color32(151, 57, 39, 255), font);

        EnsureLabel(kitchenPanel.transform, "SectionLabel_Recipes", "메뉴 선택", new Vector2(0.07f, 0.86f), new Vector2(0.30f, 0.91f), font);
        EnsureLabel(kitchenPanel.transform, "SectionLabel_Pot", "뚝배기", new Vector2(0.38f, 0.67f), new Vector2(0.66f, 0.72f), font);
        EnsureLabel(kitchenPanel.transform, "SectionLabel_Shelf", "재료 선반", new Vector2(0.71f, 0.71f), new Vector2(0.93f, 0.76f), font);

        EnsureLabel(resultPanel.transform, "SectionLabel_ResultFood", "완성 음식", new Vector2(0.07f, 0.79f), new Vector2(0.37f, 0.84f), font);
        EnsureLabel(resultPanel.transform, "SectionLabel_ResultReaction", "손님 반응", new Vector2(0.08f, 0.54f), new Vector2(0.51f, 0.59f), font);
        EnsureLabel(resultPanel.transform, "SectionLabel_ResultClue", "플레이어 대화", new Vector2(0.56f, 0.54f), new Vector2(0.91f, 0.59f), font);
        EnsureLabel(resultPanel.transform, "SectionLabel_ResultUnlock", "해금 기록", new Vector2(0.56f, 0.31f), new Vector2(0.91f, 0.36f), font);
        EnsureDivider(resultPanel.transform, "SectionRule_ResultLeft", new Vector2(0.08f, 0.535f), new Vector2(0.51f, 0.545f));
        EnsureDivider(resultPanel.transform, "SectionRule_ResultRight", new Vector2(0.56f, 0.535f), new Vector2(0.91f, 0.545f));
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        Transform direct = parent.Find(childName);
        if (direct != null)
            return direct;

        foreach (Transform child in parent)
        {
            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static void EnsurePaperFrame(Transform target, string headerTitle, Color32 headerColor, TMP_FontAsset font)
    {
        if (target == null || target.Find("PaperFrameHeader") != null)
            return;

        GameObject header = new GameObject("PaperFrameHeader", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        header.transform.SetParent(target, false);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, 58f);
        header.GetComponent<Image>().color = headerColor;

        GameObject label = new GameObject("PaperFrameHeaderText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(header.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(18f, 8f);
        labelRect.offsetMax = new Vector2(-18f, -8f);

        TMP_Text text = label.GetComponent<TMP_Text>();
        if (font != null)
            text.font = font;
        text.text = headerTitle;
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = new Color32(252, 246, 232, 255);
        text.raycastTarget = false;

        GameObject rule = new GameObject("PaperFrameRule", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rule.transform.SetParent(target, false);
        RectTransform ruleRect = rule.GetComponent<RectTransform>();
        ruleRect.anchorMin = new Vector2(0f, 1f);
        ruleRect.anchorMax = new Vector2(1f, 1f);
        ruleRect.pivot = new Vector2(0.5f, 1f);
        ruleRect.anchoredPosition = new Vector2(0f, -58f);
        ruleRect.sizeDelta = new Vector2(0f, 2f);
        rule.GetComponent<Image>().color = new Color32(86, 66, 46, 120);
    }

    private static void EnsureLabel(Transform parent, string objectName, string textValue, Vector2 anchorMin, Vector2 anchorMax, TMP_FontAsset font)
    {
        if (parent == null)
            return;

        Transform existing = parent.Find(objectName);
        GameObject labelObject;
        if (existing == null)
        {
            labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
        }
        else
        {
            labelObject = existing.gameObject;
        }

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        if (rect == null)
            rect = labelObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        if (label == null)
            label = labelObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            label.font = font;
        label.text = textValue;
        label.fontSize = 15f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = new Color32(118, 86, 59, 255);
        label.raycastTarget = false;
    }

    private static void EnsureDivider(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (parent == null)
            return;

        Transform existing = parent.Find(objectName);
        GameObject divider;
        if (existing == null)
        {
            divider = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            divider.transform.SetParent(parent, false);
        }
        else
        {
            divider = existing.gameObject;
        }

        RectTransform rect = divider.GetComponent<RectTransform>();
        if (rect == null)
            rect = divider.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = divider.GetComponent<Image>();
        if (image == null)
            image = divider.AddComponent<Image>();
        image.color = new Color32(112, 85, 58, 90);
    }

    private static GameObject FindRequiredChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
            throw new MissingReferenceException("Missing child: " + childName);

        return child.gameObject;
    }

    private static void SavePanelPrefab(GameObject panel, string panelName)
    {
        string prefabPath = PrefabFolder + "/" + panelName + ".prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(panel, prefabPath, InteractionMode.UserAction);
    }

    private static void CreateEditScene(string sceneName, string panelName)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        string prefabPath = PrefabFolder + "/" + panelName + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("Panel prefab not found: " + prefabPath);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.transform.SetParent(canvasObject.transform, false);
        instance.SetActive(true);

        RectTransform rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        EditorSceneManager.MoveGameObjectToScene(canvasObject, scene);
        EditorSceneManager.MoveGameObjectToScene(eventSystem, scene);
        EditorSceneManager.SaveScene(scene, SceneFolder + "/" + sceneName + ".unity");
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
        string folder = Path.GetFileName(folderPath);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
