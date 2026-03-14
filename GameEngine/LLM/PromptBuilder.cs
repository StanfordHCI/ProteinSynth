using System.Text.Json;
using GameEngine.Data;
using GameEngine.Models;

namespace GameEngine.LLM;

/// <summary>
/// Assembles the system prompt from template and game state.
/// Port of drafter_node prompt assembly from reflexion.py:179-199.
/// </summary>
public class PromptBuilder
{
    private readonly string _initialPromptTemplate;
    private readonly string _evalBaseText;
    private readonly string _criteriaFullText;
    private readonly string _criteriaRespOnlyText;
    private readonly string _reflectionStrictText;
    private readonly string _reflectionLenientText;

    public PromptBuilder(
        string initialPromptTemplate,
        string evalBaseText,
        string criteriaFullText,
        string criteriaRespOnlyText,
        string reflectionStrictText,
        string reflectionLenientText)
    {
        _initialPromptTemplate = initialPromptTemplate;
        _evalBaseText = evalBaseText;
        _criteriaFullText = criteriaFullText;
        _criteriaRespOnlyText = criteriaRespOnlyText;
        _reflectionStrictText = reflectionStrictText;
        _reflectionLenientText = reflectionLenientText;
    }

    /// <summary>
    /// Build the complete system prompt for the drafter LLM call.
    /// Port of drafter_node prompt assembly from reflexion.py.
    /// </summary>
    public string BuildSystemPrompt(
        string peerTutor,
        string personaDescription,
        string studentName,
        string studentGradeLevel,
        string? studentInterest,
        string? chosenProtein,
        List<Dictionary<string, string>> conversationHistory,
        string studentInput,
        string sceneDescription,
        List<string> unmetGoals,
        Dictionary<string, string> availableActions,
        Dictionary<string, List<ConceptData.PhraseEntry>> studentConceptLanguage,
        string? parameterSetting,
        List<string> reflections,
        string extraContext,
        EvalContextType evalCondition)
    {
        // Determine action goal
        var actionGoal = evalCondition == EvalContextType.ActNow
            ? "If there is an available action, mention the action and gently steer the student towards executing it"
            : "";

        // Build eval context
        var evalContext = EvalContextUtil.MakeEvaluatorContext(
            evalCondition,
            parameterSetting,
            ConceptData.AdvancedConcepts,
            _evalBaseText,
            _criteriaFullText,
            _criteriaRespOnlyText,
            _reflectionStrictText,
            _reflectionLenientText);

        // Format conversation history
        var historyLines = new List<string>();
        foreach (var msg in conversationHistory)
        {
            var role = msg.GetValueOrDefault("role", "user");
            var content = msg.GetValueOrDefault("content", "");
            historyLines.Add($"{role}: {content}");
        }
        var conversationHistoryStr = string.Join("\n", historyLines);

        // Perform template substitution
        var prompt = _initialPromptTemplate
            .Replace("{peer_tutor}", peerTutor)
            .Replace("{persona_description}", personaDescription)
            .Replace("{student_name}", studentName)
            .Replace("{student_grade_level}", studentGradeLevel)
            .Replace("{student_interest}", studentInterest ?? "")
            .Replace("{chosen_protein}", chosenProtein ?? "")
            .Replace("{conversation_history}", conversationHistoryStr)
            .Replace("{student_input}", studentInput)
            .Replace("{scene_description}", sceneDescription)
            .Replace("{unmet_goals}", JsonSerializer.Serialize(unmetGoals))
            .Replace("{action_goal}", actionGoal)
            .Replace("{available_actions}", JsonSerializer.Serialize(availableActions))
            .Replace("{student_concept_language}", JsonSerializer.Serialize(studentConceptLanguage))
            .Replace("{foundational_concepts_list}", JsonSerializer.Serialize(ConceptData.FoundationalConcepts))
            .Replace("{advanced_concepts_list}", JsonSerializer.Serialize(ConceptData.AdvancedConcepts))
            .Replace("{reflections}", string.Join("\n", reflections))
            .Replace("{extra_context}", extraContext)
            .Replace("{eval_context}", evalContext)
            .Replace("{PROTEINS_LIST}", JsonSerializer.Serialize(ProteinData.ProteinsList));

        return prompt;
    }
}
