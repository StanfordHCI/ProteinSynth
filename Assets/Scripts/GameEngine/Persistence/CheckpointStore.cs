using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using GameEngine.Dialogue;

namespace GameEngine.Persistence;

public class ResearchEvent
{
    public string id { get; set; } = Guid.NewGuid().ToString();
    public string session_id { get; set; } = "";
    public string participant_id { get; set; } = "";
    public string created_at { get; set; } = DateTime.UtcNow.ToString("o");
    public string kind { get; set; } = "turn";
    public object payload { get; set; } = new object();
}

public class Checkpoint
{
    public int Version { get; set; } = 1;
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string ParticipantId { get; set; } = "";
    public string SessionJson { get; set; } = "";
    public string Phase { get; set; } = "conversation";
    public string? LabAction { get; set; }
    public DialogueTurn? Turn { get; set; }
    public string? PendingInput { get; set; }
    public string Condition { get; set; } = "treatment";
    public Dictionary<string, bool> FinishedScenes { get; set; } = new() { ["stan"] = false, ["andy"] = false, ["tree"] = false };
    public List<ResearchEvent> Outbox { get; set; } = new();
    public List<ResearchEvent> ResearchLog { get; set; } = new();
    public PendingReflection? PendingReflection { get; set; }

    public Checkpoint Snapshot() => JsonConvert.DeserializeObject<Checkpoint>(JsonConvert.SerializeObject(this))!;

    public void Acknowledge(IEnumerable<string> ids)
    {
        var acknowledged = new HashSet<string>(ids);
        var archived = new HashSet<string>(ResearchLog.Select(item => item.id));
        foreach (var item in Outbox.Where(item => acknowledged.Contains(item.id)))
            if (archived.Add(item.id)) ResearchLog.Add(item);
        Outbox.RemoveAll(item => acknowledged.Contains(item.id));
    }

    public void Validate(string participant)
    {
        if (Version != 1 || ParticipantId != participant || !Guid.TryParse(SessionId, out _)
            || string.IsNullOrEmpty(SessionJson) || Outbox == null
            || (Phase != "conversation" && Phase != "lab" && Phase != "completed"))
            throw new InvalidDataException("Invalid or unsupported checkpoint; original file preserved.");
        var state = JsonConvert.DeserializeObject<GameEngine.Models.SessionState>(SessionJson);
        if (state == null || state.Version != 1 || state.ParticipantId != participant
            || state.AllStates == null || !state.AllStates.ContainsKey(state.CurrentStateId))
            throw new InvalidDataException("Checkpoint session is invalid; original file preserved.");
        if (Turn != null && (Turn.Lines == null || Turn.Options == null
            || Turn.Cursor < -1 || Turn.Cursor > Turn.Lines.Count))
            throw new InvalidDataException("Checkpoint dialogue is invalid; original file preserved.");
    }
}

public class PendingReflection
{
    public string TurnId { get; set; } = "";
    public string Critique { get; set; } = "";
}

public class CheckpointStore
{
    private readonly string directory;
    public CheckpointStore(string directory) { this.directory = directory; Directory.CreateDirectory(directory); }
    public string PathFor(string participant)
    {
        using var sha = SHA256.Create();
        var key = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(participant))).Replace("-", "");
        return Path.Combine(directory, key + ".json");
    }
    public void Save(Checkpoint checkpoint) => AtomicWrite(PathFor(checkpoint.ParticipantId), JsonConvert.SerializeObject(checkpoint));
    public Checkpoint? Load(string participant)
    {
        var path = PathFor(participant);
        if (!File.Exists(path)) return null;
        try { return Read(path, participant); }
        catch (Exception) when (File.Exists(path + ".bak")) { return Read(path + ".bak", participant); }
    }
    private static Checkpoint Read(string path, string participant)
    {
        var value = JsonConvert.DeserializeObject<Checkpoint>(File.ReadAllText(path));
        if (value == null) throw new InvalidDataException("Checkpoint is empty.");
        value.Validate(participant);
        return value;
    }
    public static void AtomicWrite(string path, string json)
    {
        var temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var data = Encoding.UTF8.GetBytes(json); stream.Write(data, 0, data.Length); stream.Flush(true);
        }
        if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
        else File.Move(temp, path);
    }
}
