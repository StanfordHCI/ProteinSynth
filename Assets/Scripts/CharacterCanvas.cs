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

    private Vector3 originalCharacterPos;

    void Start() {
        originalCharacterPos = character.position;
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
            character.Translate(new Vector3(420.0f, -950.0f, 0f));
        } else {
            character.position = originalCharacterPos;
        }
    }
}