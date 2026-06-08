#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildMainMenuScene
{
    private const string MenuPath = "Tools/Nyan/Rebuild Main Menu Scene";
    private const string ScenePath = "Assets/_Nyan/MainMenu.unity";

    [MenuItem(MenuPath)]
    public static void Build()
    {
        string previousScenePath = SceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(rootObject);
        }

        GameObject eventSystem = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));

        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        ConfigureCanvas(canvasObject);

        RectTransform canvas = canvasObject.GetComponent<RectTransform>();
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        Image background = CreateImage("Background", canvas, null, new Color(0.03f, 0.08f, 0.09f, 1f));
        Stretch(background.rectTransform);

        Image glowA = CreateImage("Green Glow", canvas, circleSprite, new Color(0.08f, 1f, 0.47f, 0.14f));
        SetCenter(glowA.rectTransform, new Vector2(-165f, 300f), new Vector2(310f, 310f));
        Image glowB = CreateImage("Orange Glow", canvas, circleSprite, new Color(1f, 0.38f, 0.08f, 0.16f));
        SetCenter(glowB.rectTransform, new Vector2(170f, -360f), new Vector2(380f, 380f));

        RectTransform root = CreateRect("MainMenuRoot", canvas);
        Stretch(root);
        MainMenuController controller = root.gameObject.AddComponent<MainMenuController>();

        RectTransform mainPanel = CreateRect("MainPanel", root);
        Stretch(mainPanel);

        Text title = CreateText("Title", mainPanel, "MEICHU 100th", new Color(0.94f, 1f, 0.92f, 1f), 44, FontStyle.Bold);
        SetTopCenter(title.rectTransform, new Vector2(0f, -105f), new Vector2(340f, 62f));

        Text subtitle = CreateText("Subtitle", mainPanel, "AR Panda Duel", new Color(0.58f, 1f, 0.79f, 0.92f), 22, FontStyle.Bold);
        SetTopCenter(subtitle.rectTransform, new Vector2(0f, -164f), new Vector2(300f, 34f));

        Image heroCard = CreateImage("Hero Card", mainPanel, panelSprite, new Color(1f, 1f, 1f, 0.08f));
        heroCard.type = Image.Type.Sliced;
        SetCenter(heroCard.rectTransform, new Vector2(0f, 68f), new Vector2(310f, 300f));

        Text heroText = CreateText(
            "Hero Text",
            heroCard.transform,
            "Aim at the panda.\nCharge your gesture.\nRelease bamboo or melee strikes.",
            new Color(1f, 1f, 1f, 0.86f),
            22,
            FontStyle.Bold);
        SetCenter(heroText.rectTransform, new Vector2(0f, 10f), new Vector2(250f, 150f));
        heroText.alignment = TextAnchor.MiddleCenter;

        Button startButton = CreateButton(
            "StartButton",
            mainPanel,
            "START GAME",
            new Color(0.08f, 0.92f, 0.28f, 0.96f),
            new Color(0.02f, 0.05f, 0.04f, 1f),
            panelSprite);
        SetBottomCenter(startButton.GetComponent<RectTransform>(), new Vector2(0f, 142f), new Vector2(260f, 58f));

        Button tutorialButton = CreateButton(
            "TutorialButton",
            mainPanel,
            "TUTORIAL",
            new Color(1f, 1f, 1f, 0.13f),
            Color.white,
            panelSprite);
        SetBottomCenter(tutorialButton.GetComponent<RectTransform>(), new Vector2(0f, 72f), new Vector2(260f, 52f));

        RectTransform tutorialPanel = CreateRect("TutorialPanel", root);
        Stretch(tutorialPanel);
        Image tutorialBackground = CreateImage("Tutorial Background", tutorialPanel, null, new Color(0.02f, 0.04f, 0.045f, 0.96f));
        Stretch(tutorialBackground.rectTransform);

        Text tutorialTitle = CreateText("TutorialTitle", tutorialPanel, "HOW TO PLAY", new Color(0.58f, 1f, 0.79f, 1f), 34, FontStyle.Bold);
        SetTopCenter(tutorialTitle.rectTransform, new Vector2(0f, -96f), new Vector2(320f, 52f));

        Text tutorialBody = CreateText(
            "TutorialBody",
            tutorialPanel,
            "1. Scan a plane and place the panda.\n\n2. Touch left side for melee.\nTouch right side for bow.\n\n3. Drag farther for stronger attacks.\nGesture movement charges bonus power.\n\n4. Slide through the fox icon to trigger ultimate.",
            new Color(1f, 1f, 1f, 0.88f),
            20,
            FontStyle.Normal);
        SetCenter(tutorialBody.rectTransform, new Vector2(0f, 18f), new Vector2(304f, 460f));
        tutorialBody.alignment = TextAnchor.MiddleLeft;

        Button tutorialBackButton = CreateButton(
            "TutorialBackButton",
            tutorialPanel,
            "BACK",
            new Color(1f, 1f, 1f, 0.14f),
            Color.white,
            panelSprite);
        SetBottomCenter(tutorialBackButton.GetComponent<RectTransform>(), new Vector2(0f, 70f), new Vector2(230f, 52f));

        tutorialPanel.gameObject.SetActive(false);

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("gameSceneName").stringValue = "FakrazeNyanMerge";
        serializedController.FindProperty("startButton").objectReferenceValue = startButton;
        serializedController.FindProperty("tutorialButton").objectReferenceValue = tutorialButton;
        serializedController.FindProperty("tutorialBackButton").objectReferenceValue = tutorialBackButton;
        serializedController.FindProperty("mainPanel").objectReferenceValue = mainPanel.gameObject;
        serializedController.FindProperty("tutorialPanel").objectReferenceValue = tutorialPanel.gameObject;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene);
        if (!string.IsNullOrEmpty(previousScenePath) && previousScenePath != ScenePath)
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"[MainMenu] Rebuilt scene: {ScenePath}");
    }

    private static void ConfigureCanvas(GameObject canvasObject)
    {
        RectTransform rootRect = canvasObject.GetComponent<RectTransform>();
        rootRect.localPosition = Vector3.zero;
        rootRect.localRotation = Quaternion.identity;
        rootRect.localScale = Vector3.one;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390f, 844f);
        scaler.matchWidthOrHeight = 1f;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Color backgroundColor,
        Color textColor,
        Sprite sprite)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = backgroundColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Text text = CreateText("Label", buttonObject.transform, label, textColor, 19, FontStyle.Bold);
        Stretch(text.rectTransform);
        text.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string textValue, Color color, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.text = textValue;
        text.color = color;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.font = GetBuiltinFont();
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetCenter(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
    }

    private static void SetTopCenter(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
    }

    private static void SetBottomCenter(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
    }

    private static Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }
}
#endif
