#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BuildPlayerCombatHudPrefab
{
    private const string MenuPath = "Tools/Nyan/Rebuild Player Combat HUD Prefab";
    private const string PrefabFolder = "Assets/_Nyan/Prefabs/UI";
    private const string PrefabPath = PrefabFolder + "/PlayerCombatHUD.prefab";

    [MenuItem(MenuPath)]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[PlayerCombatHUD] Exit Play Mode before rebuilding the HUD prefab.");
            return;
        }

        EnsureFolder("Assets/_Nyan/Prefabs", "UI");

        Sprite ringSprite = LoadSprite("Assets/_Nyan/UI/Icons/MeleeBaseRing.png");
        Sprite meleeSprite = LoadSprite("Assets/_Nyan/UI/Icons/MeleeKnifeIcon.png");
        Sprite rangedSprite = LoadSprite("Assets/_Nyan/UI/Icons/BowIcon.png");
        Sprite arrowSprite = LoadSprite("Assets/_Nyan/UI/Icons/PowerArrow.png");
        Sprite ultimateSprite = LoadSprite("Assets/_Nyan/UI/Icons/UltimateFoxIcon.png");
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        GameObject prefabSource = new GameObject(
            "PlayerCombatHUD",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(PlayerCombatCanvasHud));

        ConfigureCanvas(prefabSource);

        RectTransform content = CreateRect("Content", prefabSource.transform);
        Stretch(content);

        Image healthBackground = CreateImage("Health Bar", content, null, new Color(0f, 0f, 0f, 0.53f));
        SetTopCenter(healthBackground.rectTransform, new Vector2(0f, -45f), new Vector2(310f, 30f));
        Image healthFill = CreateImage("Fill", healthBackground.transform, null, new Color(0f, 1f, 0.03f, 0.88f));
        Stretch(healthFill.rectTransform);

        RectTransform crosshair = CreateRect("Crosshair", content);
        SetCenter(crosshair, Vector2.zero, new Vector2(35f, 35f));
        Image crosshairHorizontal = CreateImage("Horizontal", crosshair, null, new Color(1f, 0.18f, 0.12f, 0.9f));
        SetCenter(crosshairHorizontal.rectTransform, Vector2.zero, new Vector2(35f, 2f));
        Image crosshairVertical = CreateImage("Vertical", crosshair, null, new Color(1f, 0.18f, 0.12f, 0.9f));
        SetCenter(crosshairVertical.rectTransform, Vector2.zero, new Vector2(2f, 35f));

        RectTransform dragControl = CreateRect("Drag Control", content);
        dragControl.anchorMin = Vector2.zero;
        dragControl.anchorMax = Vector2.zero;
        dragControl.pivot = new Vector2(0.5f, 0.5f);
        dragControl.sizeDelta = new Vector2(100f, 100f);

        Image controlRingBase = CreateImage("Charge Ring Background", dragControl, ringSprite, new Color(1f, 1f, 1f, 0.3f));
        Stretch(controlRingBase.rectTransform);
        Image chargeArc = CreateImage("Gesture Charge Progress", dragControl, ringSprite, new Color(14f / 255f, 237f / 255f, 19f / 255f, 0.9f));
        Stretch(chargeArc.rectTransform);
        chargeArc.type = Image.Type.Filled;
        chargeArc.fillMethod = Image.FillMethod.Radial360;
        chargeArc.fillOrigin = (int)Image.Origin360.Top;
        chargeArc.fillClockwise = true;
        chargeArc.fillAmount = 0f;

        Image meleeIcon = CreateImage("Melee Icon", dragControl, meleeSprite, Color.white);
        SetCenter(meleeIcon.rectTransform, Vector2.zero, new Vector2(35f, 35f));
        meleeIcon.preserveAspect = true;
        Image rangedIcon = CreateImage("Ranged Icon", dragControl, rangedSprite, Color.white);
        SetCenter(rangedIcon.rectTransform, Vector2.zero, new Vector2(35f, 35f));
        rangedIcon.preserveAspect = true;

        RectTransform powerArrowPivot = CreateRect("Displacement Arrow Pivot", dragControl);
        SetCenter(powerArrowPivot, Vector2.zero, Vector2.zero);
        Image powerArrow = CreateImage("Displacement Arrow", powerArrowPivot, arrowSprite, Color.white);
        powerArrow.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        powerArrow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        powerArrow.rectTransform.pivot = new Vector2(0f, 0.5f);
        powerArrow.rectTransform.anchoredPosition = Vector2.zero;
        powerArrow.rectTransform.sizeDelta = new Vector2(185.397f, 44.82f);
        powerArrow.preserveAspect = false;

        RectTransform ultimateTarget = CreateRect("Ultimate Target", content);
        ultimateTarget.anchorMin = Vector2.zero;
        ultimateTarget.anchorMax = Vector2.zero;
        ultimateTarget.pivot = new Vector2(0.5f, 0.5f);
        ultimateTarget.sizeDelta = new Vector2(50f, 50f);
        Image ultimateBackground = CreateImage("Background", ultimateTarget, circleSprite, new Color(0f, 0f, 0f, 0.57f));
        Stretch(ultimateBackground.rectTransform);
        Image ultimateRing = CreateImage("Ring", ultimateTarget, ringSprite, new Color(1f, 1f, 1f, 0.3f));
        Stretch(ultimateRing.rectTransform);
        Image ultimateIcon = CreateImage("Fox Icon", ultimateTarget, ultimateSprite, Color.white);
        SetCenter(ultimateIcon.rectTransform, Vector2.zero, new Vector2(24f, 24f));
        ultimateIcon.preserveAspect = true;

        Image releaseFlash = CreateImage("Release Flash", content, circleSprite, new Color(1f, 0.9f, 0.2f, 1f));
        releaseFlash.rectTransform.anchorMin = Vector2.zero;
        releaseFlash.rectTransform.anchorMax = Vector2.zero;
        releaseFlash.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        releaseFlash.rectTransform.sizeDelta = new Vector2(80f, 80f);

        dragControl.gameObject.SetActive(false);
        ultimateTarget.gameObject.SetActive(false);
        releaseFlash.gameObject.SetActive(false);
        content.gameObject.SetActive(false);

        PlayerCombatCanvasHud hud = prefabSource.GetComponent<PlayerCombatCanvasHud>();
        AssignReferences(
            hud,
            content,
            healthBackground,
            healthFill,
            crosshair,
            dragControl,
            controlRingBase,
            chargeArc,
            meleeIcon,
            rangedIcon,
            powerArrowPivot,
            powerArrow,
            ultimateTarget,
            ultimateBackground,
            ultimateRing,
            ultimateIcon,
            releaseFlash);

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefabSource, PrefabPath);
        Object.DestroyImmediate(prefabSource);

        if (prefabAsset == null)
        {
            Debug.LogError("[PlayerCombatHUD] Failed to save prefab.");
            return;
        }

        InstallSceneInstance(prefabAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PlayerCombatHUD] Rebuilt prefab and installed scene instance: {PrefabPath}");
    }

    private static void InstallSceneInstance(GameObject prefabAsset)
    {
        PlayerCombatController controller = Object.FindFirstObjectByType<PlayerCombatController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            Debug.LogError("[PlayerCombatHUD] PlayerCombatController was not found in the loaded scene.");
            return;
        }

        Transform parent = controller.transform;
        Transform existing = parent.Find("PlayerCombatHUD");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
        instance.name = "PlayerCombatHUD";
        RectTransform instanceRect = instance.GetComponent<RectTransform>();
        instanceRect.localPosition = Vector3.zero;
        instanceRect.localRotation = Quaternion.identity;
        instanceRect.localScale = Vector3.one;
        PlayerCombatCanvasHud newHud = instance.GetComponent<PlayerCombatCanvasHud>();

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("canvasHud").objectReferenceValue = newHud;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);

        PlayerCombatCanvasHud[] oldHuds = controller.GetComponents<PlayerCombatCanvasHud>();
        foreach (PlayerCombatCanvasHud oldHud in oldHuds)
        {
            Object.DestroyImmediate(oldHud);
        }

        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorSceneManager.SaveScene(controller.gameObject.scene);
        Selection.activeGameObject = instance;
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
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390f, 844f);
        scaler.matchWidthOrHeight = 1f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;
    }

    private static void AssignReferences(
        PlayerCombatCanvasHud hud,
        RectTransform content,
        Image healthBackground,
        Image healthFill,
        RectTransform crosshair,
        RectTransform dragControl,
        Image controlRingBase,
        Image chargeArc,
        Image meleeIcon,
        Image rangedIcon,
        RectTransform powerArrowPivot,
        Image powerArrow,
        RectTransform ultimateTarget,
        Image ultimateBackground,
        Image ultimateRing,
        Image ultimateIcon,
        Image releaseFlash)
    {
        SerializedObject serializedHud = new SerializedObject(hud);
        SetReference(serializedHud, "root", content);
        SetReference(serializedHud, "healthBackground", healthBackground);
        SetReference(serializedHud, "healthFill", healthFill);
        SetReference(serializedHud, "crosshair", crosshair);
        SetReference(serializedHud, "dragControl", dragControl);
        SetReference(serializedHud, "controlRingBase", controlRingBase);
        SetReference(serializedHud, "chargeArc", chargeArc);
        SetReference(serializedHud, "meleeIcon", meleeIcon);
        SetReference(serializedHud, "rangedIcon", rangedIcon);
        SetReference(serializedHud, "powerArrowPivot", powerArrowPivot);
        SetReference(serializedHud, "powerArrow", powerArrow);
        SetReference(serializedHud, "ultimateTarget", ultimateTarget);
        SetReference(serializedHud, "ultimateTargetBackground", ultimateBackground);
        SetReference(serializedHud, "ultimateTargetRing", ultimateRing);
        SetReference(serializedHud, "ultimateTargetIcon", ultimateIcon);
        SetReference(serializedHud, "releaseFlash", releaseFlash);
        serializedHud.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"[PlayerCombatHUD] Missing serialized property: {propertyName}");
            return;
        }

        property.objectReferenceValue = value;
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

    private static Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            Debug.LogWarning($"[PlayerCombatHUD] Sprite not found: {assetPath}");
        }

        return sprite;
    }

    private static void EnsureFolder(string parentFolder, string folderName)
    {
        string folderPath = parentFolder + "/" + folderName;
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }
}
#endif
