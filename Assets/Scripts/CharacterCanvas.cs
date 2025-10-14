using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Compiler;
using Yarn.Unity;
using System.Threading;
using System;
using TMPro;

public class CharacterCanvas : MonoBehaviour
{
    public Image background;
    public Transform character;
    public Transform optionListView;

    private Vector3 originalCharacterPos;

    public GameObject thinkingIndicator; 
    public TMP_Text thinkingText;
    private string[] thinkingOptions = {
        "Thinking...",
        "Pondering...",
        "Contemplating...",
        "Processing...",
        "Reflecting...",
    };

    void Start() {
        originalCharacterPos = character.position;
        hide_thinking();
    }

    [YarnCommand("hide_canvas")]
    public void hide_canvas(bool hidden)
    {
        if (hidden) {
            this.GetComponent<CanvasGroup>().alpha = 0; 
        } else {
            this.GetComponent<CanvasGroup>().alpha = 1; 
        }

        // the gameobjects need to stay active or parameters in all the animators are reset
        // for (int j = 0; j < TwoCharacterPanel.transform.childCount; j++)
        // {
        //     TwoCharacterPanel.transform.GetChild(j).gameObject.SetActive(!hidden);
        // }
    }

    [YarnCommand("hide_background")]
    public void hide_background(bool hidden) {
        if (hidden) {
            background.enabled = false;
        } else {
            background.enabled = true;
        }
    }

    [YarnCommand("toggle_character_position")]
    public void toggle_character_position(bool corner) {
        if (corner) {
            // character.position = 
            character.Translate(new Vector3(-725.0f, -1250.0f, 0f));
            character.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); 
        } else {
            character.position = originalCharacterPos;
            character.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f); 
        }
    }

    // Shows AI thinking indicator
    [YarnCommand("show_thinking_indicator")]
    public void show_thinking() {
        if (thinkingIndicator!= null) {
            thinkingIndicator.GetComponent<Image>().color = new Color(1, 1, 1, 1); 
            int index = UnityEngine.Random.Range(0, thinkingOptions.Length);
            thinkingText.text = thinkingOptions[index];
        }
    }

    // Hides AI thinking indicator
    [YarnCommand("hide_thinking_indicator")]
    public void hide_thinking() {
        if (thinkingIndicator != null) {
            thinkingIndicator.GetComponent<Image>().color = new Color(1, 1, 1, 0); 
            thinkingText.text = "";
        }
    }

    // Hide everything except for the option buttons + darken background
    [YarnCommand("hide_static_elements")]
    public void hide_static_elements(bool hidden) {
        if (hidden) {
            background.color = new Color(0.45f, 0.45f, 0.45f, 1f);
            character.GetComponent<CanvasGroup>().alpha = 0;
            foreach (Transform child in optionListView)
            {
                if (child.name != "Black Option View(Clone)") { // name of the option buton component
                    child.GetComponent<CanvasGroup>().alpha = 0;
                } 
            }
        } else {
            background.color = Color.white;
            character.GetComponent<CanvasGroup>().alpha = 1;
            foreach (Transform child in optionListView)
            {
                if (child.name != "Black Option View(Clone)") { // name of the option buton component
                    child.GetComponent<CanvasGroup>().alpha = 1;
                } 
            }
        }
    }
}