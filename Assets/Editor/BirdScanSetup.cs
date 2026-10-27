/*
    BirdScanSetup.cs: editor menu items for the bird sampling step
    - ProteinSynth/Bird Scan/Add Bird Scan To Open Scene: builds the scan canvas (sample slots, reticle,
      "Specimen detected" badge, Take sample button, reveal card) and a wired-up BirdScanManager
    - ProteinSynth/Bird Scan/Convert Living Birds Materials To URP: the package ships built-in shaders,
      which render pink under URP
*/

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BirdScanSetup
{
    const string LivingBirdsFolder = "Assets/living birds";
    const int SlotCount = 6;

    static readonly Color Black = new Color(0.07f, 0.07f, 0.07f);
    static readonly Color BadgeGreen = new Color(0.18f, 0.86f, 0.13f);
    static readonly Color BadgeText = new Color(0.07f, 0.42f, 0.07f);

    [MenuItem("ProteinSynth/Bird Scan/Add Bird Scan To Open Scene")]
    static void AddBirdScanToScene()
    {
        if (Object.FindObjectOfType<BirdScanManager>() != null &&
            !EditorUtility.DisplayDialog("Bird Scan", "This scene already has a BirdScanManager. Add another?", "Add", "Cancel"))
            return;

        Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Canvas
        GameObject canvasGO = new GameObject("BirdScanCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0f;

        RectTransform root = Rect(NewUI("ScanUI", canvasGO.transform));
        Stretch(root);

        // Header + sample slots
        TextMeshProUGUI header = Text("Header", root, "Collect 6 samples", 40, Black, FontStyles.Bold);
        Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -112), new Vector2(900, 60));

        RectTransform slotRow = Rect(NewUI("SampleSlots", root));
        Place(slotRow, new Vector2(0.5f, 1f), new Vector2(0, -225), new Vector2(970, 150));
        HorizontalLayoutGroup row = slotRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 14;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = true;

        BirdSampleSlot[] slots = new BirdSampleSlot[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            slots[i] = BuildSlot(slotRow, i, rounded);

        // "Specimen detected" badge
        Image badge = AddImage(NewUI("DetectedBadge", root), BadgeGreen, rounded, 0.25f);
        Place(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 430), new Vector2(430, 72));
        Stretch(Text("Label", badge.transform, "Specimen detected", 34, BadgeText, FontStyles.Normal).rectTransform);
        badge.gameObject.SetActive(false);

        // Reticle: four L-shaped corners
        RectTransform reticle = Rect(NewUI("Reticle", root));
        Place(reticle, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(523, 523));
        const float arm = 174f, thickness = 29f;
        foreach (Vector2 corner in new[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) })
        {
            AnchorBar(reticle, corner, new Vector2(arm, thickness));
            AnchorBar(reticle, corner, new Vector2(thickness, arm));
        }

        // Take sample button
        Image buttonImage = AddImage(NewUI("TakeSampleButton", root), new Color(1f, 1f, 1f, 0.2f), rounded, 0.25f);
        Place(buttonImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -281), new Vector2(360, 80));
        Outline outline = buttonImage.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2, -2);
        buttonImage.raycastTarget = true;
        Button button = buttonImage.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        TextMeshProUGUI buttonLabel = Text("Label", buttonImage.transform, "Take sample", 32, Color.white, FontStyles.Normal);
        Stretch(buttonLabel.rectTransform);

        // Camera flash
        Image flash = AddImage(NewUI("Flash", root), new Color(1, 1, 1, 0), null, 1f);
        Stretch(flash.rectTransform);
        flash.raycastTarget = false;

        // Reveal card (tap to skip)
        Image card = AddImage(NewUI("RevealCard", root), new Color(0.79f, 0.95f, 1f), rounded, 0.15f);
        Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(540, 540));
        AddShadow(card.gameObject, 8);
        card.raycastTarget = true;
        card.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
        Image cardPortrait = AddImage(NewUI("Portrait", card.transform), Color.white, null, 1f);
        Stretch(cardPortrait.rectTransform, 60, 60, 110, 60);
        cardPortrait.preserveAspect = true;
        cardPortrait.raycastTarget = false;
        Image cardLabel = AddImage(NewUI("Label", card.transform), Color.white, null, 1f);
        Place(cardLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(322, 122));
        AddShadow(cardLabel.gameObject, 4);
        TextMeshProUGUI cardText = Text("Genotype", cardLabel.transform, "Bb", 96, Black, FontStyles.Bold);
        Stretch(cardText.rectTransform);
        card.gameObject.SetActive(false);

        // Manager
        GameObject managerGO = new GameObject("BirdScanManager");
        BirdScanManager manager = managerGO.AddComponent<BirdScanManager>();
        manager.blueBirdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{LivingBirdsFolder}/resources/lb_blueJayHQ.prefab");
        manager.goldBirdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{LivingBirdsFolder}/resources/lb_goldFinchHQ.prefab");
        manager.arCamera = Camera.main;
        manager.planeFinder = Object.FindObjectOfType<Vuforia.PlaneFinderBehaviour>();
        manager.reticle = reticle;
        manager.scanUIRoot = root.gameObject;
        manager.headerText = header;
        manager.slots = slots;
        manager.detectedBadge = badge.gameObject;
        manager.takeSampleButton = button;
        manager.takeSampleLabel = buttonLabel;
        manager.flashOverlay = flash;
        manager.revealCard = card.rectTransform;
        manager.revealPortrait = cardPortrait;
        manager.revealGenotypeText = cardText;

        // Other screen-space canvases in the scene (e.g. the Punnett square UI) are hidden while scanning
        var otherCanvases = new System.Collections.Generic.List<GameObject>();
        foreach (Canvas other in Object.FindObjectsOfType<Canvas>())
            if (other.isRootCanvas && other != canvas && other.renderMode != RenderMode.WorldSpace)
                otherCanvases.Add(other.gameObject);
        manager.hideWhileScanning = otherCanvases.ToArray();

        Undo.RegisterCreatedObjectUndo(canvasGO, "Add Bird Scan");
        Undo.RegisterCreatedObjectUndo(managerGO, "Add Bird Scan");
        EditorSceneManager.MarkSceneDirty(managerGO.scene);
        Selection.activeGameObject = managerGO;

        if (manager.arCamera == null)
            Debug.LogWarning("BirdScanSetup: no MainCamera found; assign the AR camera on BirdScanManager.");
        if (Object.FindObjectOfType<EventSystem>() == null)
            Debug.LogWarning("BirdScanSetup: no EventSystem in this scene. That's fine if the scene is loaded additively next to one; otherwise add one so Take sample can be tapped.");
    }

    [MenuItem("ProteinSynth/Bird Scan/Convert Living Birds Materials To URP")]
    static void ConvertLivingBirdsMaterials()
    {
        Shader simpleLit = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (simpleLit == null)
        {
            Debug.LogError("BirdScanSetup: URP Simple Lit shader not found.");
            return;
        }

        int converted = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { LivingBirdsFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            // The feather particle material belongs to the unused death effect
            if (mat == null || mat.shader.name.StartsWith("Universal Render Pipeline") || path.Contains("Particle"))
                continue;

            bool cutout = mat.shader.name.Contains("Cutout");
            Texture tex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            float cutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f;

            Undo.RecordObject(mat, "Convert Living Birds Materials");
            mat.shader = simpleLit;
            mat.SetTexture("_BaseMap", tex);
            // The package tints its materials gray to suit built-in lighting; white reads better in AR
            mat.SetColor("_BaseColor", Color.white);
            if (cutout)
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", cutoff);
                mat.EnableKeyword("_ALPHATEST_ON");
            }
            EditorUtility.SetDirty(mat);
            converted++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"BirdScanSetup: converted {converted} Living Birds materials to URP Simple Lit.");
    }

    // ── Builders ───────────────────────────────────────────────────────────

    static BirdSampleSlot BuildSlot(Transform parent, int index, Sprite rounded)
    {
        Image bg = AddImage(NewUI($"Slot{index + 1}", parent), new Color(0.96f, 0.96f, 0.96f, 0.92f), rounded, 0.3f);
        AddShadow(bg.gameObject, 4);

        Image portrait = AddImage(NewUI("Portrait", bg.transform), Color.white, null, 1f);
        Stretch(portrait.rectTransform, 14, 14, 30, 10);
        portrait.preserveAspect = true;
        portrait.gameObject.SetActive(false);

        Image label = AddImage(NewUI("Label", bg.transform), Color.white, null, 1f);
        Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, -8), new Vector2(92, 56));
        AddShadow(label.gameObject, 3);
        TextMeshProUGUI text = Text("Genotype", label.transform, "", 36, Black, FontStyles.Bold);
        Stretch(text.rectTransform);
        label.gameObject.SetActive(false);

        BirdSampleSlot slot = bg.gameObject.AddComponent<BirdSampleSlot>();
        slot.background = bg;
        slot.portrait = portrait;
        slot.label = label.gameObject;
        slot.genotypeText = text;
        return slot;
    }

    // One arm of a reticle corner, pinned to that corner of the reticle rect
    static void AnchorBar(RectTransform reticle, Vector2 corner, Vector2 size)
    {
        Image bar = AddImage(NewUI("Corner", reticle), Color.white, null, 1f);
        bar.raycastTarget = false;
        RectTransform rt = bar.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = corner;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    static RectTransform Rect(GameObject go) => (RectTransform)go.transform;

    static Image AddImage(GameObject go, Color color, Sprite sprite, float pixelsPerUnitMultiplier)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        }
        return img;
    }

    static void AddShadow(GameObject go, float distance)
    {
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.18f);
        shadow.effectDistance = new Vector2(0, -distance);
    }

    static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color, FontStyles style)
    {
        TextMeshProUGUI tmp = NewUI(name, parent).AddComponent<TextMeshProUGUI>();
        tmp.text = value;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    // Anchor to a single point; position is the pivot's offset from that anchor
    static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, float left = 0, float right = 0, float bottom = 0, float top = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }
}
