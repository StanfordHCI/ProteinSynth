using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

// Keep the current response visible until the next generated clip is available.
public class AudioReadyLineView : LineView
{
    [SerializeField] private MessageQueueCommands messageQueue;
    private Button advanceButton;
    private TMPro.TMP_Text buttonLabel;
    private string normalLabel;

    private bool CanAdvance => messageQueue == null || messageQueue.CanContinueResponse;

    public override void RunLine(LocalizedLine dialogueLine, System.Action onDialogueLineFinished)
    {
        RefreshContinueButton();
        base.RunLine(dialogueLine, onDialogueLineFinished);
    }

    private void Update()
    {
        RefreshContinueButton();
    }

    private void RefreshContinueButton()
    {
        if (advanceButton == null && continueButton != null)
        {
            advanceButton = continueButton.GetComponent<Button>();
            buttonLabel = continueButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (buttonLabel != null) normalLabel = buttonLabel.text;
        }

        if (advanceButton != null)
        {
            advanceButton.interactable = CanAdvance;
            if (buttonLabel != null) buttonLabel.text = !CanAdvance ? "Preparing audio…"
                : messageQueue != null && messageQueue.NextIsTextOnly ? "Next (text only)" : normalLabel;
        }
    }

    public override void UserRequestedViewAdvancement()
    {
        // Check again at click time, including keyboard and direct calls.
        if (CanAdvance)
        {
            base.UserRequestedViewAdvancement();
        }
    }
}
