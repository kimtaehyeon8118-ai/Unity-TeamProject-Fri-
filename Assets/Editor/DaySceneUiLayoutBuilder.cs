#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class DaySceneUiLayoutBuilder
{
    private const string DayScenePath = "Assets/Scenes/DayScene.unity";
    private const string UiRootName = "DayArtLayer";
    private const string PopupRootName = "PopupRoot";
    private const string GeneratedFolder = "Assets/UI/Generated";
    private const string Background2AlphaPath = GeneratedFolder + "/day_background2_alpha.png";
    private const string MainOverlayAlphaPath = GeneratedFolder + "/day_main_alpha.png";
    private const string KoreanFontPath = "Assets/Fonts/Korean_Full_TMP.asset";

    [MenuItem("Tools/Day UI/Apply Provided Art Layout")]
    public static void ApplyProvidedArtLayout()
    {
        ApplyProvidedArtLayout(interactive: true);
    }

    public static void ApplyProvidedArtLayoutBatch()
    {
        ApplyProvidedArtLayout(interactive: false);
    }

    private static void ApplyProvidedArtLayout(bool interactive)
    {
        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(DayScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        DayUIManager manager = Object.FindAnyObjectByType<DayUIManager>();

        if (canvas == null || manager == null)
        {
            Debug.LogError("DayScene UI layout failed. Canvas or DayUIManager was not found.");
            return;
        }

        EnsureSpriteImportSettings();
        TMP_FontAsset koreanFont = ResolveKoreanFont();
        EnsureKoreanFontFallback(koreanFont);

        RectTransform artRoot = EnsureRoot(canvas.transform, UiRootName);
        ClearChildren(artRoot);

        Image background1 = CreateImage(artRoot, "DayArt_Background1", "Assets/UI/day_background1.png", Vector2.zero, Vector2.one);
        Image npcSlot = CreateImage(artRoot, "DayArt_NpcSlot", "Assets/UI/NPC1_teasu.png", new Vector2(0.155f, 0.20f), new Vector2(0.365f, 0.79f));
        Image background2 = CreateImage(artRoot, "DayArt_Background2", Background2AlphaPath, Vector2.zero, Vector2.one);
        Image mainOverlay = CreateImage(artRoot, "DayArt_MainOverlay", MainOverlayAlphaPath, Vector2.zero, Vector2.one);

        Button optionButton = CreateButton(artRoot, "DayArt_OptionButton", "Assets/UI/day_option.png", new Vector2(0.925f, 0.885f), new Vector2(0.975f, 0.985f));
        Button recipeButton = CreateButton(artRoot, "DayArt_RecipeButton", "Assets/UI/day_menu.png", new Vector2(0.903f, 0.625f), new Vector2(0.974f, 0.790f));
        Button noteButton = CreateButton(artRoot, "DayArt_NoteButton", "Assets/UI/day_note.png", new Vector2(0.903f, 0.430f), new Vector2(0.974f, 0.595f));
        Button dialogueAdvanceButton = CreateTransparentButton(artRoot, "DayArt_DialogueAdvanceButton", new Vector2(0.027f, 0.045f), new Vector2(0.973f, 0.345f));

        TMP_Text timeText = CreateText(artRoot, "TimeText", new Vector2(0.063f, 0.904f), new Vector2(0.175f, 0.970f), "12:00", 34f, TextAlignmentOptions.MidlineLeft, Color.white, koreanFont);
        TMP_Text dayText = CreateText(artRoot, "DayText", new Vector2(0.265f, 0.905f), new Vector2(0.625f, 0.970f), "1일차", 34f, TextAlignmentOptions.Midline, Color.white, koreanFont);
        TMP_Text npcInfoTitleText = CreateText(artRoot, "NpcInfoTitleText", new Vector2(0.175f, 0.745f), new Vector2(0.345f, 0.795f), "정보", 26f, TextAlignmentOptions.MidlineLeft, Color.white, koreanFont);
        TMP_Text npcInfoText = CreateText(artRoot, "NpcInfoText", new Vector2(0.175f, 0.565f), new Vector2(0.345f, 0.735f), "이름: 강태수\n성별: 남성\n직업: 손님\n특징: 얼큰한 국물 요리를 좋아함", 26f, TextAlignmentOptions.TopLeft, Color.white, koreanFont);
        TMP_Text speakerText = CreateText(artRoot, "SpeakerText", new Vector2(0.068f, 0.250f), new Vector2(0.310f, 0.305f), "강태수 [NPC]", 28f, TextAlignmentOptions.MidlineLeft, Color.white, koreanFont);
        TMP_Text dialogueText = CreateText(artRoot, "DialogueText", new Vector2(0.068f, 0.120f), new Vector2(0.915f, 0.242f), "안녕하세요. 오늘은 얼큰한 찌개를 먹고 싶어요.", 29f, TextAlignmentOptions.TopLeft, new Color32(30, 27, 25, 255), koreanFont);
        Button goToKitchenButton = CreateTextButton(artRoot, "GoToKitchenButton", new Vector2(0.795f, 0.080f), new Vector2(0.945f, 0.150f), "주방으로 이동", koreanFont);

        PopupObjects popupObjects = CreatePopupRoot(canvas.transform, koreanFont);

        DayResponseArtView artView = EnsureComponent<DayResponseArtView>(artRoot.gameObject);
        artView.backgroundBack = background1;
        artView.npcImage = npcSlot;
        artView.backgroundFrame = background2;
        artView.mainOverlay = mainOverlay;
        artView.optionButton = optionButton;
        artView.recipeButton = recipeButton;
        artView.noteButton = noteButton;
        artView.dialogueAdvanceButton = dialogueAdvanceButton;
        artView.timeText = timeText;
        artView.dayText = dayText;
        artView.npcInfoTitleText = npcInfoTitleText;
        artView.npcInfoText = npcInfoText;
        artView.speakerText = speakerText;
        artView.dialogueText = dialogueText;
        artView.goToKitchenButton = goToKitchenButton;
        artView.goToKitchenButtonText = goToKitchenButton.GetComponentInChildren<TMP_Text>(true);
        artView.dimButton = popupObjects.dimButton;
        artView.recipePopup = popupObjects.recipePopup;
        artView.recipePopupText = popupObjects.recipePopupText;
        artView.memoPopup = popupObjects.memoPopup;
        artView.memoInputField = popupObjects.memoInputField;

        background1.raycastTarget = false;
        npcSlot.raycastTarget = false;
        background2.raycastTarget = false;
        mainOverlay.raycastTarget = false;
        optionButton.targetGraphic.raycastTarget = true;
        recipeButton.targetGraphic.raycastTarget = true;
        noteButton.targetGraphic.raycastTarget = true;

        manager.portraitImage = npcSlot;
        manager.customerPortrait = LoadSprite("Assets/UI/NPC1_teasu.png");
        manager.menuOpenButton = recipeButton;
        manager.nextButton = dialogueAdvanceButton;
        SetPrivateObjectReference(manager, "dayResponseArtView", artView);

        NpcInfoView npcInfoView = EnsureComponent<NpcInfoView>(artRoot.gameObject);
        npcInfoView.Bind(npcInfoTitleText, npcInfoText);

        DialogueView dialogueView = EnsureComponent<DialogueView>(artRoot.gameObject);
        dialogueView.Bind(speakerText, dialogueText, dialogueAdvanceButton, goToKitchenButton, artView.goToKitchenButtonText);

        RecipePopupView recipePopupView = EnsureComponent<RecipePopupView>(popupObjects.recipePopup);
        recipePopupView.Bind(popupObjects.recipePopupText);

        MemoPopupView memoPopupView = EnsureComponent<MemoPopupView>(popupObjects.memoPopup);
        memoPopupView.Bind(popupObjects.memoInputField);

        PopupRootController popupRoot = EnsureComponent<PopupRootController>(popupObjects.root);
        popupRoot.Bind(popupObjects.dimButton, popupObjects.recipePopup, popupObjects.memoPopup);
        popupRoot.Initialize();

        CustomerDaySceneController sceneController = EnsureComponent<CustomerDaySceneController>(artRoot.gameObject);
        sceneController.ConfigureDefaultContent();
        sceneController.ConfigureSceneReferences(manager.customerPanel, manager.kitchenPanel, artRoot.gameObject);
        SetPrivateObjectReference(sceneController, "artView", artView);
        SetPrivateObjectReference(sceneController, "npcInfoView", npcInfoView);
        SetPrivateObjectReference(sceneController, "dialogueView", dialogueView);
        SetPrivateObjectReference(sceneController, "recipePopupView", recipePopupView);
        SetPrivateObjectReference(sceneController, "memoPopupView", memoPopupView);
        SetPrivateObjectReference(sceneController, "popupRoot", popupRoot);
        SetPrivateObjectReference(sceneController, "timeText", timeText);
        SetPrivateObjectReference(sceneController, "dayText", dayText);
        SetPrivateObjectReference(sceneController, "recipeButton", recipeButton);
        SetPrivateObjectReference(sceneController, "memoButton", noteButton);
        SetPrivateObjectReference(sceneController, "settingsButton", optionButton);

        MakeCurrentCustomerPanelArtFriendly(manager);
        ApplyFontToSceneText(koreanFont);

        artRoot.SetAsFirstSibling();
        popupObjects.root.transform.SetAsLastSibling();

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(artView);
        EditorUtility.SetDirty(sceneController);
        EditorUtility.SetDirty(npcInfoView);
        EditorUtility.SetDirty(dialogueView);
        EditorUtility.SetDirty(recipePopupView);
        EditorUtility.SetDirty(memoPopupView);
        EditorUtility.SetDirty(popupRoot);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("DayScene UI art layout applied with Korean TMP font.");
    }

    private static void EnsureSpriteImportSettings()
    {
        EnsureGeneratedAlphaSprites();

        string[] paths =
        {
            "Assets/UI/day_background1.png",
            Background2AlphaPath,
            MainOverlayAlphaPath,
            "Assets/UI/day_menu.png",
            "Assets/UI/day_note.png",
            "Assets/UI/day_option.png",
            "Assets/UI/day_menu_popup.png",
            "Assets/UI/day_memo_popup.png",
            "Assets/UI/NPC1_teasu.png",
            "Assets/UI/NPC2_seoa.png",
            "Assets/UI/NPC3minjun.png"
        };

        foreach (string path in paths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
        }
    }

    private static TMP_FontAsset ResolveKoreanFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        if (font == null)
        {
            Debug.LogWarning("Korean TMP font asset was not found at " + KoreanFontPath + ". Korean text may render as missing glyphs.");
            return null;
        }

        font.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(font);
        return font;
    }

    private static void EnsureKoreanFontFallback(TMP_FontAsset font)
    {
        if (font == null)
            return;

        TMP_Settings settings = TMP_Settings.instance;
        if (settings == null)
            return;

        SerializedObject serializedSettings = new SerializedObject(settings);
        SerializedProperty fallbacks = serializedSettings.FindProperty("m_fallbackFontAssets");
        if (fallbacks == null || !fallbacks.isArray)
            return;

        for (int i = 0; i < fallbacks.arraySize; i++)
        {
            if (fallbacks.GetArrayElementAtIndex(i).objectReferenceValue == font)
                return;
        }

        fallbacks.InsertArrayElementAtIndex(fallbacks.arraySize);
        fallbacks.GetArrayElementAtIndex(fallbacks.arraySize - 1).objectReferenceValue = font;
        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
    }

    private static void ApplyFontToSceneText(TMP_FontAsset font)
    {
        if (font == null)
            return;

        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text text in texts)
        {
            if (text == null)
                continue;

            text.font = font;
            text.fontSize += 0f;
            EditorUtility.SetDirty(text);
        }
    }

    private static void EnsureGeneratedAlphaSprites()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            AssetDatabase.CreateFolder("Assets/UI", "Generated");

        WriteAlphaKeyedPng("Assets/UI/day_main.png", MainOverlayAlphaPath, IsBlackKeyPixel);
        WriteAlphaKeyedPng("Assets/UI/day_background2.png", Background2AlphaPath, IsWhiteKeyPixel);
        AssetDatabase.ImportAsset(MainOverlayAlphaPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(Background2AlphaPath, ImportAssetOptions.ForceUpdate);
    }

    private static void WriteAlphaKeyedPng(string sourcePath, string destinationPath, AlphaKeyPredicate alphaKeyPredicate)
    {
        string sourceFullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, sourcePath);
        if (!File.Exists(sourceFullPath))
        {
            Debug.LogError("Missing UI source texture: " + sourcePath);
            return;
        }

        byte[] sourceBytes = File.ReadAllBytes(sourceFullPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(sourceBytes))
        {
            Object.DestroyImmediate(texture);
            Debug.LogError("Failed to read UI source texture: " + sourcePath);
            return;
        }

        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (alphaKeyPredicate(pixels[i]))
                pixels[i].a = 0;
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        string destinationFullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFullPath));
        File.WriteAllBytes(destinationFullPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private delegate bool AlphaKeyPredicate(Color32 pixel);

    private static bool IsBlackKeyPixel(Color32 pixel)
    {
        return pixel.r <= 4 && pixel.g <= 4 && pixel.b <= 4;
    }

    private static bool IsWhiteKeyPixel(Color32 pixel)
    {
        return pixel.r >= 250 && pixel.g >= 250 && pixel.b >= 250;
    }

    private static RectTransform EnsureRoot(Transform canvasTransform, string rootName)
    {
        Transform existing = canvasTransform.Find(rootName);
        GameObject rootObject = existing != null
            ? existing.gameObject
            : new GameObject(rootName, typeof(RectTransform));

        rootObject.transform.SetParent(canvasTransform, false);
        RectTransform rect = rootObject.GetComponent<RectTransform>();
        Stretch(rect, Vector2.zero, Vector2.one);
        return rect;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private static Image CreateImage(Transform parent, string objectName, string spritePath, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        Stretch(rect, anchorMin, anchorMax);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = LoadSprite(spritePath);
        image.color = Color.white;
        image.preserveAspect = false;
        return image;
    }

    private static Button CreateButton(Transform parent, string objectName, string spritePath, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Stretch(rect, anchorMin, anchorMax);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = LoadSprite(spritePath);
        image.color = Color.white;
        image.preserveAspect = true;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = image;
        return button;
    }

    private static Button CreateTransparentButton(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Stretch(rect, anchorMin, anchorMax);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        return button;
    }

    private static Button CreateTextButton(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, string label, TMP_FontAsset font)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Stretch(rect, anchorMin, anchorMax);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(72, 84, 112, 230);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        CreateText(buttonObject.transform, "Label", Vector2.zero, Vector2.one, label, 26f, TextAlignmentOptions.Midline, Color.white, font);
        buttonObject.SetActive(false);
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, string text, float fontSize, TextAlignmentOptions alignment, Color color, TMP_FontAsset font)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        Stretch(rect, anchorMin, anchorMax);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        if (font != null)
            label.font = font;
        return label;
    }

    private static PopupObjects CreatePopupRoot(Transform canvasTransform, TMP_FontAsset font)
    {
        RectTransform rootRect = EnsureRoot(canvasTransform, PopupRootName);
        GameObject root = rootRect.gameObject;
        ClearChildren(root.transform);

        Button dimButton = CreateDimButton(root.transform);
        GameObject recipePopup = CreatePopupImage(root.transform, "RecipePopup", "Assets/UI/day_menu_popup.png", new Vector2(0.34f, 0.20f), new Vector2(0.66f, 0.86f));
        TMP_Text recipeText = CreateText(recipePopup.transform, "RecipePopupText", new Vector2(0.11f, 0.13f), new Vector2(0.89f, 0.73f), "김치찌개: 김치 + 돼지고기 + 물\n된장찌개: 된장 + 두부 + 애호박\n순두부찌개: 2일차 이후 잠김", 27f, TextAlignmentOptions.TopLeft, new Color32(55, 48, 42, 255), font);

        GameObject memoPopup = CreatePopupImage(root.transform, "MemoPopup", "Assets/UI/day_memo_popup.png", new Vector2(0.32f, 0.16f), new Vector2(0.68f, 0.88f));
        TMP_InputField memoInput = CreateMemoInput(memoPopup.transform, font);

        recipePopup.SetActive(false);
        memoPopup.SetActive(false);
        dimButton.gameObject.SetActive(false);

        PopupObjects objects = new PopupObjects();
        objects.root = root;
        objects.dimButton = dimButton;
        objects.recipePopup = recipePopup;
        objects.recipePopupText = recipeText;
        objects.memoPopup = memoPopup;
        objects.memoInputField = memoInput;
        return objects;
    }

    private static Button CreateDimButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("DimButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Stretch(buttonObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.56f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        return button;
    }

    private static GameObject CreatePopupImage(Transform parent, string objectName, string spritePath, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject popupObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        popupObject.transform.SetParent(parent, false);
        Stretch(popupObject.GetComponent<RectTransform>(), anchorMin, anchorMax);

        Image image = popupObject.GetComponent<Image>();
        image.sprite = LoadSprite(spritePath);
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = true;
        return popupObject;
    }

    private static TMP_InputField CreateMemoInput(Transform parent, TMP_FontAsset font)
    {
        GameObject inputObject = new GameObject("MemoInputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        inputObject.transform.SetParent(parent, false);
        Stretch(inputObject.GetComponent<RectTransform>(), new Vector2(0.095f, 0.145f), new Vector2(0.835f, 0.695f));

        Image inputImage = inputObject.GetComponent<Image>();
        inputImage.color = new Color(1f, 1f, 1f, 0f);
        inputImage.raycastTarget = true;

        GameObject viewportObject = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        viewportObject.transform.SetParent(inputObject.transform, false);
        Stretch(viewportObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        TMP_Text placeholder = CreateText(viewportObject.transform, "Placeholder", Vector2.zero, Vector2.one, "손님의 취향이나 주문 힌트를 적어두세요.", 26f, TextAlignmentOptions.TopLeft, new Color32(95, 101, 110, 150), font);
        TMP_Text text = CreateText(viewportObject.transform, "Text", Vector2.zero, Vector2.one, string.Empty, 26f, TextAlignmentOptions.TopLeft, new Color32(45, 50, 58, 255), font);
        text.raycastTarget = true;

        TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
        input.textViewport = viewportObject.GetComponent<RectTransform>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.transition = Selectable.Transition.None;
        return input;
    }

    private sealed class PopupObjects
    {
        public GameObject root;
        public Button dimButton;
        public GameObject recipePopup;
        public TMP_Text recipePopupText;
        public GameObject memoPopup;
        public TMP_InputField memoInputField;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void MakeCurrentCustomerPanelArtFriendly(DayUIManager manager)
    {
        GameObject customerPanel = manager.customerPanel;
        if (customerPanel == null)
            return;

        RectTransform panelRect = customerPanel.GetComponent<RectTransform>();
        if (panelRect != null)
            Stretch(panelRect, Vector2.zero, Vector2.one);

        MakeImagesTransparentInChildren(customerPanel);
        MakeLegacyTextsTransparentInChildren(customerPanel, manager);
        SetLegacyPanelActive(customerPanel, "MenuBoardPanel", false);
        SetLegacyPanelActive(customerPanel, "CustomerSpeechPanel", false);
        if (manager.nameText != null)
            manager.nameText.gameObject.SetActive(false);
        if (manager.dialogueText != null)
            manager.dialogueText.gameObject.SetActive(false);
    }

    private static void MakeImagesTransparentInChildren(GameObject target)
    {
        if (target == null)
            return;

        Image[] images = target.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image == null)
                continue;

            image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
            image.raycastTarget = false;
        }
    }

    private static void MakeLegacyTextsTransparentInChildren(GameObject target, DayUIManager manager)
    {
        if (target == null || manager == null)
            return;

        TMP_Text[] texts = target.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null || text == manager.dialogueText || text == manager.nameText)
                continue;

            text.color = new Color(text.color.r, text.color.g, text.color.b, 0f);
            text.raycastTarget = false;
        }
    }

    private static void SetLegacyPanelActive(GameObject root, string panelName, bool active)
    {
        if (root == null)
            return;

        Transform panel = FindChildRecursive(root.transform, panelName);
        if (panel != null)
            panel.gameObject.SetActive(active);
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        if (root.name == childName)
            return root;

        foreach (Transform child in root)
        {
            Transform result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogError("Missing UI sprite: " + path);

        return sprite;
    }

    private static void SetPrivateObjectReference(Object target, string fieldName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property == null)
            return;

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
#endif
