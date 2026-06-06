#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AssignPlayerHudSprites
{
    private const string MenuPath = "Tools/Nyan/Assign Player HUD Sprites";

    [MenuItem(MenuPath)]
    public static void Assign()
    {
        PlayerCombatCanvasHud[] huds = Object.FindObjectsByType<PlayerCombatCanvasHud>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (huds.Length == 0)
        {
            Debug.LogError("[AssignPlayerHudSprites] No PlayerCombatCanvasHud found in the loaded scene.");
            return;
        }

        PlayerCombatCanvasHud hud = huds[0];
        SerializedObject serializedHud = new SerializedObject(hud);

        SetSprite(serializedHud, "controlRingBaseSprite", "Assets/_Nyan/UI/Icons/MeleeBaseRing.png");
        SetSprite(serializedHud, "meleeIconSprite", "Assets/_Nyan/UI/Icons/MeleeKnifeIcon.png");
        SetSprite(serializedHud, "rangedIconSprite", "Assets/_Nyan/UI/Icons/BowIcon.png");
        SetSprite(serializedHud, "powerArrowSprite", "Assets/_Nyan/UI/Icons/PowerArrow.png");
        SetSprite(serializedHud, "ultimateIconSprite", "Assets/_Nyan/UI/Icons/UltimateFoxIcon.png");

        serializedHud.FindProperty("healthBarOffset").vector2Value = new Vector2(40f, -45f);
        serializedHud.FindProperty("healthBarSize").vector2Value = new Vector2(310f, 30f);
        serializedHud.FindProperty("iconSize").floatValue = 35f;
        serializedHud.FindProperty("powerArrowWidthRatio").floatValue = 0.28f;
        serializedHud.FindProperty("healthBackgroundColor").colorValue = new Color(0f, 0f, 0f, 0.53f);
        serializedHud.FindProperty("healthFillColor").colorValue = new Color(0f, 1f, 0.0333333f, 0.88f);
        serializedHud.FindProperty("powerArrowColor").colorValue = new Color(1f, 1f, 1f, 0.8f);
        serializedHud.FindProperty("ultimateBaseColor").colorValue = new Color(0f, 0f, 0f, 0.57f);
        serializedHud.FindProperty("ultimateRingColor").colorValue = new Color(1f, 1f, 1f, 0.3f);

        serializedHud.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        EditorSceneManager.SaveScene(hud.gameObject.scene);
        Debug.Log("[AssignPlayerHudSprites] Assigned split Figma HUD sprites and saved the scene.");
    }

    private static void SetSprite(SerializedObject serializedObject, string propertyName, string assetPath)
    {
        Sprite sprite = LoadSprite(assetPath);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[AssignPlayerHudSprites] Missing serialized field: {propertyName}");
            return;
        }

        property.objectReferenceValue = sprite;
    }

    private static Sprite LoadSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            Debug.LogError($"[AssignPlayerHudSprites] Failed to load Sprite at {assetPath}");
        }

        return sprite;
    }
}
#endif
