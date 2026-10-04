using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GameEngine.Dialogue;

public enum SpeechState { Pending, Ready, Failed, Skipped }

public class DialogueLine
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Speaker { get; set; } = "";
    public string Text { get; set; } = "";
    public SpeechState Speech { get; set; } = SpeechState.Pending;
    public bool CanPresent => Speech != SpeechState.Pending;
    public string DisplayText => Speaker + ": " + Text;
}

public class DialogueTurn
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public List<DialogueLine> Lines { get; set; } = new();
    public List<string> Options { get; set; } = new();
    public string? Action { get; set; }
    // Index of the currently visible line. Kept on save so resume replays it.
    public int Cursor { get; set; } = -1;
    public bool Finished { get; set; }
    public bool CanContinue => Finished || Cursor + 1 >= Lines.Count || Lines[Cursor + 1].CanPresent;

    public static DialogueTurn Create(string message, string defaultSpeaker, string? action)
    {
        var turn = new DialogueTurn { Action = action };
        var speaker = defaultSpeaker;
        foreach (var raw in message.Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("->")) { turn.Options.Add(line.Substring(2).Trim()); continue; }
            // Split speakers before sentences; the same line objects drive speech.
            foreach (var part in Regex.Split(line, @"(?<=[.!?])\s+(?=[\p{L}][\p{L} ]{0,30}::)"))
            {
                var text = part;
                var separator = text.IndexOf("::", StringComparison.Ordinal);
                if (separator >= 0) { speaker = text.Substring(0, separator).Trim(); text = text.Substring(separator + 2).Trim(); }
                foreach (var sentence in Regex.Split(text, @"(?<=[.!?])\s+"))
                    if (!string.IsNullOrWhiteSpace(sentence))
                        turn.Lines.Add(new DialogueLine { Speaker = speaker, Text = sentence.Trim() });
            }
        }
        return turn;
    }

    public void PrepareResume()
    {
        if (Cursor >= 0) Cursor--; // Replay the interrupted line, never skip unseen text.
        foreach (var line in Lines) line.Speech = SpeechState.Pending;
    }
}
