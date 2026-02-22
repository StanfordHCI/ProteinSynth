using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using TMPro;

public class TodoList : MonoBehaviour
{
    [Header("Prefab for a to-do item (Toggle + Text/TMP child)")]
    public GameObject toDoUI;

    [Header("Layout group containing the to-do items")]
    public VerticalLayoutGroup layoutGroup;

    [Header("Quest To-Do Texts")]
    public Dictionary<string, string> questTodos = new Dictionary<string, string>()
    {
        {"scan_nucleus", "Scan the nucleus with your camera."},
        {"arrange_cards", "Arrange codon cards."},
        {"finish_transcription", "Press \"Finish Transcription\" when done."},
        {"scan_ribosome", "Scan the ribosome with your camera."},
        {"select_amino", "Select the correct amino acids formed from your mRNA."}
    };

    // Track active todos to prevent duplicates
    private Dictionary<string, Toggle> activeTodos = new Dictionary<string, Toggle>();

    /// <summary>Saved instance of the prefab UI for arrange_cards so we can update its text as image target cards are added.</summary>
    private GameObject arrangeCardsTodoInstance;

    /// <summary>
    /// Force layout to immediately rebuild, ensuring proper spacing
    /// </summary>
    public void ReadjustLayout()
    {
        if (layoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
        }
    }

    private static readonly string ArrangeCardsLabelPrefix = "Arrange codon cards";

    /// <summary>
    /// Removes every todo item in the layout whose label starts with "Arrange codon cards"
    /// (with or without " (x/5)") so only one can exist. Cleans both layout and activeTodos.
    /// </summary>
    private void RemoveAllArrangeCardsItems()
    {
        if (layoutGroup == null) return;
        Transform parent = layoutGroup.transform;
        List<string> keysToRemove = new List<string>();
        List<GameObject> toDestroy = new List<GameObject>();
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            GameObject go = child.gameObject;
            string label = GetTodoLabel(go);
            if (string.IsNullOrEmpty(label) || !label.StartsWith(ArrangeCardsLabelPrefix)) continue;
            toDestroy.Add(go);
            foreach (var kvp in activeTodos)
            {
                if (kvp.Value != null && kvp.Value.transform.IsChildOf(go.transform))
                {
                    keysToRemove.Add(kvp.Key);
                    break;
                }
            }
        }
        foreach (string k in keysToRemove)
            activeTodos.Remove(k);
        foreach (GameObject go in toDestroy)
            Destroy(go);
        if (toDestroy.Count > 0)
        {
            arrangeCardsTodoInstance = null;
            ReadjustLayout();
        }
    }

    /// <summary>
    /// Gets the todo label from a prefab clone (searches all descendants, including inactive).
    /// </summary>
    private static string GetTodoLabel(GameObject item)
    {
        if (item == null) return null;
        TMP_Text[] tmps = item.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i] != null && !string.IsNullOrEmpty(tmps[i].text))
                return tmps[i].text;
        }
        Text[] texts = item.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && !string.IsNullOrEmpty(texts[i].text))
                return texts[i].text;
        }
        return null;
    }

    [YarnCommand("add_todo")]
    public void AddToDo(string todo)
    {
        if (!questTodos.ContainsKey(todo))
        {
            Debug.LogWarning($"Todo key '{todo}' not found in questTodos dictionary!");
            return;
        }

        // For arrange_cards: remove ALL existing items that show "Arrange codon cards" (any duplicate) from layout and dict
        if (todo == "arrange_cards")
            RemoveAllArrangeCardsItems();

        if (activeTodos.ContainsKey(todo))
        {
            Debug.Log($"Todo '{todo}' already exists in list.");
            return;
        }

        // Instantiate a new to-do UI element under the layout group parent
        GameObject newItem = Instantiate(toDoUI, layoutGroup.transform);
        Toggle toggle = newItem.GetComponentInChildren<Toggle>();

        // Set the label text (supports TMP or legacy Text)
        TMP_Text tmp = newItem.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = questTodos[todo];
        }
        else
        {
            Text text = newItem.GetComponentInChildren<Text>();
            if (text != null)
                text.text = questTodos[todo];
        }

        // Save instance for arrange_cards so we can update text as image target cards are added
        if (todo == "arrange_cards")
            arrangeCardsTodoInstance = newItem;

        // Add event listener for transparency update
        toggle.onValueChanged.AddListener(isOn =>
        {
            UpdateTransparency(newItem, isOn);
        });

        // Apply initial transparency based on current checked state
        UpdateTransparency(newItem, toggle.isOn);

        // Add to dictionary
        activeTodos.Add(todo, toggle);

        // Force an immediate layout rebuild to fix spacing
        ReadjustLayout();
    }

    [YarnCommand("checkoff_todo")]
    public void CheckoffToDo(string todo)
    {
        if (!activeTodos.ContainsKey(todo))
        {
            Debug.LogWarning($"Todo '{todo}' not found in active list!");
            return;
        }

        Toggle toggle = activeTodos[todo];
        toggle.isOn = !toggle.isOn; // toggles between checked/unchecked

        // Apply transparency immediately
        UpdateTransparency(toggle.transform.parent.gameObject, toggle.isOn);

        // Optional: visually refresh layout after toggling
        ReadjustLayout();
    }

    /// <summary>
    /// Updates the displayed text for a todo (e.g. "Arrange codon cards (3/5)" for arrange_cards).
    /// Uses the saved arrange_cards instance when available.
    /// </summary>
    public void UpdateTodoText(string todo, string text)
    {
        GameObject item = null;
        if (todo == "arrange_cards" && arrangeCardsTodoInstance != null)
            item = arrangeCardsTodoInstance;
        else if (activeTodos.ContainsKey(todo))
            item = GetTodoItemFromToggle(activeTodos[todo]);
        if (item == null) return;

        TMP_Text tmp = item.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
            tmp.text = text;
        else
        {
            Text legacyText = item.GetComponentInChildren<Text>();
            if (legacyText != null)
                legacyText.text = text;
        }
    }

    /// <summary>
    /// Sets the checked state of a todo and optionally applies transparency.
    /// </summary>
    public void SetTodoChecked(string todo, bool isChecked, bool applyTransparency = true)
    {
        if (!activeTodos.ContainsKey(todo)) return;
        Toggle toggle = activeTodos[todo];
        toggle.isOn = isChecked;
        if (applyTransparency)
        {
            GameObject item = todo == "arrange_cards" && arrangeCardsTodoInstance != null
                ? arrangeCardsTodoInstance
                : GetTodoItemFromToggle(toggle);
            if (item != null)
                UpdateTransparency(item, isChecked);
        }
    }

    /// <summary>
    /// Gets the root todo item GameObject that contains the given toggle (direct child of layout).
    /// </summary>
    private GameObject GetTodoItemFromToggle(Toggle toggle)
    {
        if (toggle == null || layoutGroup == null) return null;
        Transform t = toggle.transform;
        while (t != null && t.parent != layoutGroup.transform)
            t = t.parent;
        return t != null ? t.gameObject : null;
    }

    /// <summary>
    /// Updates the transparency of a to-do item based on whether it's checked.
    /// Only the parent's Image and the text named "Label" are affected.
    /// </summary>
    private void UpdateTransparency(GameObject item, bool isChecked)
    {
        float imageAlpha = isChecked ? 0.2f : 0.5f;
        float textAlpha = isChecked ? 0.5f : 1f; // Text stays 100% until checked off

        // Parent image only
        Image parentImage = item.GetComponent<Image>();
        if (parentImage != null)
        {
            Color c = parentImage.color;
            c.a = imageAlpha;
            parentImage.color = c;
        }

        // Text named "Label" only: full opacity unless checked off
        Transform labelTransform = null;
        foreach (Transform t in item.GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.name == "Label") { labelTransform = t; break; }
        }
        if (labelTransform != null)
        {
            TMP_Text tmp = labelTransform.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                Color c = tmp.color;
                c.a = textAlpha;
                tmp.color = c;
            }
            else
            {
                Text text = labelTransform.GetComponent<Text>();
                if (text != null)
                {
                    Color c = text.color;
                    c.a = textAlpha;
                    text.color = c;
                }
            }
        }
    }
}
