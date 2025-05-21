using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using System.Collections;
using static System.String;

public class InputCommands : MonoBehaviour
{
  public GameObject inputPanel;
  public TMP_InputField input;
  public GameObject promptText;
  public CanvasGroup Avatar; 
//   public LogManager logManager; 
  public GameObject optionsListView; 

  void Start()
  {
    hide(true);
    TouchScreenKeyboard.hideInput = true;
    if (Avatar != null) {
      Avatar.alpha = 0; 
    }
    optionsListView.GetComponent<CanvasGroup>().blocksRaycasts = true; 
  }

  [YarnCommand("ToggleHideInput")]
  public void hide(bool hidden)
  {
    for (int j = 0; j < inputPanel.transform.childCount; j++)
    {
      inputPanel.transform.GetChild(j).gameObject.SetActive(!hidden);
    }

    if (Avatar != null) {
      if (hidden) {
      Avatar.alpha = 1; 
      } else {
      Avatar.alpha = 1; 
      }
    }
  }

  [YarnCommand("SetInputPrompt")]
  public void setPromptText(string prompt)
  {
    UnityEngine.Debug.Log("setting prompt to " + prompt);
    promptText.GetComponent<TextMeshProUGUI>().text = prompt;
  }

  [YarnCommand("RequestInput")]
  public void RequestInput(string prompt)
  {
    if (Avatar != null) {
      Avatar.alpha = 1; 
    }
    promptText.GetComponent<TextMeshProUGUI>().text = prompt;
    input.text = "";
    hide(false);
    
    // optionsListView.transform.Find("RoundedOptionView(Clone)").GetComponent<CanvasGroup>().interactable = false;
    // optionsListView.transform.Find("RoundedOptionView(Clone)").GetComponent<CanvasGroup>().blocksRaycasts = false; 
    
    StartCoroutine(WaitForInput()); 
    // Need to refresh layout after changing the prompt;
    // otherwise the text gets squished
    LayoutRebuilder.ForceRebuildLayoutImmediate(inputPanel.GetComponent<RectTransform>());
  }

  private IEnumerator WaitForInput() {
    // Need to delay focus slightly to work
    yield return new WaitForSeconds(0.01f);
    input.ActivateInputField();
    TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);

    // wait until input is something nonempty to enable the submit button
    while (IsNullOrWhiteSpace(input.text)) {
      yield return null; 
    }
    // optionsListView.transform.Find("RoundedOptionView(Clone)").GetComponent<CanvasGroup>().interactable = true; 
    // optionsListView.transform.Find("RoundedOptionView(Clone)").GetComponent<CanvasGroup>().blocksRaycasts = true; 
  }

  // TODO: this only works for strings atm
  [YarnCommand("SaveAndCloseInput")]
  public void SaveAndCloseInput(string variableName)
  {
    GlobalInMemoryVariableStorage.Instance.SetValue(variableName, input.text);
    hide(true);
    // optionsListView.transform.Find("RoundedOptionView(Clone)").GetComponent<CanvasGroup>().interactable = true; 
    // optionsListView.transform.Find("RoundedOptionView(Clone)").GetComponent<CanvasGroup>().blocksRaycasts = true; 
    if (Avatar != null) {
      Avatar.alpha = 0; 
    }

    // logManager.addPlayerMessageLog(input.text); 
  }
}

