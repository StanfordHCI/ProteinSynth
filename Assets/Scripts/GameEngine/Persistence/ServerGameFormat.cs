using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameEngine.Activities;
using GameEngine.Data;
using GameEngine.Models;
using GameEngine.Systems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GameEngine.Persistence;

// The Python server's JSON contract, plus a Unity extension for exact UI resume.
public static class ServerGameFormat
{
    private static JsonSerializer SnakeSerializer() => JsonSerializer.Create(new JsonSerializerSettings {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    });

    public static string Serialize(Checkpoint checkpoint, IActivity activity)
    {
        checkpoint.Validate(checkpoint.ParticipantId);
        var state = JsonConvert.DeserializeObject<SessionState>(checkpoint.SessionJson)!;
        var serializer = SnakeSerializer();
        var states = new JObject();
        var actions = new ActionSystem(activity);
        foreach (var pair in state.AllStates)
        {
            pair.Value.UpdateActions(actions);
            var value = JObject.FromObject(pair.Value, serializer);
            value["available"] = JObject.FromObject(pair.Value.Available);
            value["unavailable"] = JObject.FromObject(pair.Value.Unavailable);
            states[pair.Key] = value;
        }
        var current = state.AllStates[state.CurrentStateId];
        var result = JObject.FromObject(new {
            finished_scenes = checkpoint.FinishedScenes,
            logging_id = checkpoint.SessionId, messages = state.Messages,
            participant_id = state.ParticipantId, username = state.Username, grade_level = state.GradeLevel,
            curr_game_state_id = state.CurrentStateId, all_states = states, step = state.Step,
            condition = checkpoint.Condition, peer_tutor = state.PeerTutor, persona_description = state.PersonaDescription,
            student_profile = new { name = state.Username, grade_level = state.GradeLevel, student_interest = state.StudentInterest },
            current_scene_id = state.CurrentStateId,
            scene_description = current.Description, available_actions = current.Available,
            student_concept_language = JObject.FromObject(state.StudentConceptLanguage, serializer),
            parameter_setting = state.ParameterSetting, reflections = state.Reflections, no_reviser = true,
            unity_checkpoint = checkpoint
        });
        activity.ExportServerFields(state, result);
        return result.ToString(Formatting.None);
    }

    public static Checkpoint Deserialize(string json, string participant, IActivity activity)
    {
        var value = JObject.Parse(json);
        if (value["unity_checkpoint"] != null)
        {
            var saved = value["unity_checkpoint"]!.ToObject<Checkpoint>()
                ?? throw new InvalidDataException("Saved game has an empty Unity checkpoint.");
            saved.Validate(participant);
            saved.EnsureActivity(activity);
            return saved;
        }
        // Import original Python JSON saves when no local checkpoint is available.
        var serializer = SnakeSerializer();
        var states = value["all_states"]?.ToObject<Dictionary<string, GameState>>(serializer)
            ?? throw new InvalidDataException("Saved game is missing its states.");
        var state = new SessionState {
            Username = (string?)value["username"] ?? "", GradeLevel = (string?)value["grade_level"] ?? "",
            PeerTutor = (string?)value["peer_tutor"] ?? "Yari", PersonaDescription = (string?)value["persona_description"] ?? "",
            ParticipantId = (string?)value["participant_id"] ?? "", Step = (int?)value["step"] ?? 0,
            CurrentStateId = (string?)value["curr_game_state_id"] ?? "", AllStates = states,
            Messages = value["messages"]?.ToObject<List<Dictionary<string, string>>>() ?? new(),
            StudentInterest = (string?)value["student_profile"]?["student_interest"],
            StudentConceptLanguage = value["student_concept_language"]?.ToObject<Dictionary<string, List<ConceptData.PhraseEntry>>>(serializer) ?? new(),
            Reflections = value["reflections"]?.ToObject<List<string>>() ?? new(),
            ParameterSetting = (string?)value["parameter_setting"] ?? "strict"
        };
        activity.ImportServerFields(value, state);
        if (!activity.StateIds.Contains(state.CurrentStateId))
            throw new InvalidDataException("This saved game uses a scene not supported by the Unity tutoring client.");
        var checkpoint = new Checkpoint {
            ParticipantId = state.ParticipantId, SessionId = (string?)value["logging_id"] ?? "", ActivityId = activity.Id,
            SessionJson = JsonConvert.SerializeObject(state), Condition = (string?)value["condition"] ?? "treatment",
            FinishedScenes = value["finished_scenes"]?.ToObject<Dictionary<string, bool>>() ?? new(),
            Phase = activity.IsCompleted(states) ? "completed" : "conversation"
        };
        checkpoint.Validate(participant);
        return checkpoint;
    }
}
