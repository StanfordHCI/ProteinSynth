using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Dialogue;
using UnityEngine;
using Yarn.Unity;

// Typed presentation adapter: each line owns its speech state and clip identity.
public class MessageQueueCommands : MonoBehaviour
{
    public AudioSource audioSource;
    private DialogueTurn turn;
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private GameManager Manager => GetComponent<GameManager>();
    private GlobalInMemoryVariableStorage Vars => GlobalInMemoryVariableStorage.Instance;

    public bool CanContinueResponse => turn == null || turn.CanContinue;
    public bool NextIsTextOnly => turn != null && !turn.Finished && turn.Cursor + 1 < turn.Lines.Count
        && turn.Lines[turn.Cursor + 1].Speech == SpeechState.Failed;

    public void Present(DialogueTurn response)
    {
        ReleaseClips(); turn = response;
        SetOptions(response.Options);
        GetComponent<LastLineScroll>()?.SetMessageList(new Queue<string>(response.Lines.Select(line => line.DisplayText)));
    }

    public void Clear() { ReleaseClips(); turn = null; SetOptions(new List<string>()); }

    public void SetOptions(List<string> options)
    {
        for (int i = 0; i < 5; i++) Vars.SetValue("$gptOption" + (i + 1), i < options.Count ? options[i] : "");
    }

    public void CompleteSpeech(DialogueTurn expectedTurn, DialogueLine line, AudioClip clip)
    {
        if (turn != expectedTurn || !turn.Lines.Contains(line) || line.Speech != SpeechState.Pending)
        { if (clip != null) Destroy(clip); return; }
        if (clip != null) { clips[line.Id] = clip; line.Speech = SpeechState.Ready; }
        else line.Speech = SpeechState.Failed;
    }

    [YarnCommand("run_response")]
    public void RunResponse()
    {
        if (turn == null || turn.Finished) { Vars.SetValue("$gptResponse", ""); return; }
        if (!CanContinueResponse) throw new InvalidOperationException("Next line is still preparing speech.");
        if (turn.Cursor >= 0 && turn.Cursor < turn.Lines.Count)
        {
            var previous = turn.Lines[turn.Cursor];
            if (clips.TryGetValue(previous.Id, out var previousClip)) { if (audioSource != null) audioSource.Stop(); Destroy(previousClip); clips.Remove(previous.Id); }
        }
        turn.Cursor++;
        if (turn.Cursor < turn.Lines.Count)
        {
            Vars.SetValue("$gptResponse", turn.Lines[turn.Cursor].DisplayText);
            Manager.SaveProgress();
            return;
        }
        turn.Finished = true;
        var node = ActionNode(turn.Action);
        if (node != null)
        {
            Manager.BeginAction(turn.Action);
            Vars.SetValue("$actionNode", node); Vars.SetValue("$gptResponse", "ACTION");
        }
        else { Vars.SetValue("$gptResponse", ""); Manager.SaveProgress(); }
    }

    [YarnCommand("wait_for_message")]
    public IEnumerator WaitForMessage()
    {
        // Used only after await_response succeeds; speech always resolves or fails.
        while (turn != null && !CanContinueResponse) yield return null;
    }

    [YarnCommand("wait_for_audio")]
    public IEnumerator WaitForAudio()
    {
        while (turn != null && turn.Cursor >= 0 && turn.Cursor < turn.Lines.Count
            && !turn.Lines[turn.Cursor].CanPresent) yield return null;
    }

    [YarnCommand("play_voiceover")]
    public void PlayVoiceover()
    {
        if (audioSource == null || turn == null || turn.Cursor < 0 || turn.Cursor >= turn.Lines.Count) return;
        var line = turn.Lines[turn.Cursor];
        audioSource.Stop();
        if (clips.TryGetValue(line.Id, out var clip) && clip != null) { audioSource.clip = clip; audioSource.Play(); }
    }

    public static string ActionNode(string action)
    {
        if (action == "ENCOURAGE_STUDENT_AND_BID_THEM_FAREWELL") return "EndGame";
        if (action == "TO_PROTEIN_SYNTHESIS_LAB") return "ProteinSynthesisLab";
        const string prefix = "TO_PROTEIN_SYNTHESIS_LAB_";
        if (action != null && action.StartsWith(prefix))
        {
            var suffix = action.Substring(prefix.Length).ToLowerInvariant();
            if (GameEngine.Data.ProteinData.ProteinsList.Contains(suffix))
                return "ProteinSynthesisLab" + char.ToUpperInvariant(suffix[0]) + suffix.Substring(1);
        }
        return null;
    }

    private void ReleaseClips()
    {
        if (audioSource != null) { audioSource.Stop(); audioSource.clip = null; }
        foreach (var clip in clips.Values) if (clip != null) Destroy(clip);
        clips.Clear();
    }
    private void OnDestroy() => ReleaseClips();
}
