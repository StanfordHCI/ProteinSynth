using System.Collections.Generic;
using GameEngine.Data;
using Newtonsoft.Json;

namespace GameEngine.Models;

public class SessionState
{
    public int Version { get; set; } = 1;
    public string Username { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public string PeerTutor { get; set; } = "Yari";
    public string PersonaDescription { get; set; } = "";
    public string ParticipantId { get; set; } = "";
    public int Step { get; set; }
    public string CurrentStateId { get; set; } = "";
    public Dictionary<string, GameState> AllStates { get; set; } = new();
    public List<Dictionary<string, string>> Messages { get; set; } = new();
    public Dictionary<string, List<ConceptData.PhraseEntry>> StudentConceptLanguage { get; set; } = new();
    public string? StudentInterest { get; set; }
    /// <summary>Activity-owned values, e.g. the chosen protein.</summary>
    public Dictionary<string, string> ActivityValues { get; set; } = new();
    /// <summary>Legacy version 1 field, read for migration only (see IActivity.UpgradeSession).</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ChosenProtein { get; set; }
    public List<string> Reflections { get; set; } = new();
    public string ParameterSetting { get; set; } = "strict";
}
