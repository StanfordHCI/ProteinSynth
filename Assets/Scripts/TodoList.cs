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

    [YarnCommand("add_todo")]
    public void AddToDo(string todo)
    {
        if (!questTodos.ContainsKey(todo))
        {
            Debug.LogWarning($"Todo key '{todo}' not found in questTodos dictionary!");
            return;
        }

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

        // Add event listener for transparency update
        toggle.onValueChanged.AddListener(isOn =>
        {
            UpdateTransparency(newItem, isOn);
        });

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
    /// Updates the transparency of a to-do item based on whether it's checked.
    /// </summary>
    private void UpdateTransparency(GameObject item, bool isChecked)
    {
        float alpha = isChecked ? 0.5f : 1f;

        // Apply to TMP or Text component
        TMP_Text tmp = item.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            Color c = tmp.color;
            c.a = alpha;
            tmp.color = c;
        }
        else
        {
            Text text = item.GetComponentInChildren<Text>();
            if (text != null)
            {
                Color c = text.color;
                c.a = alpha;
                text.color = c;
            }
        }

        // Optionally fade other UI elements like background or toggle graphic
        Graphic[] graphics = item.GetComponentsInChildren<Graphic>();
        foreach (Graphic g in graphics)
        {
            if (!(g is Text) && !(g is TMP_Text))
            {
                Color c = g.color;
                c.a = alpha;
                g.color = c;
            }
        }
    }
}
