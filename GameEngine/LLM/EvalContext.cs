namespace GameEngine.LLM;

/// <summary>
/// Evaluation context for determining which criteria to include in the prompt.
/// Port of EvalContext from schemas.py and eval_context.py.
/// </summary>
public enum EvalContextType
{
    /// <summary>There is at least one unmet goal remaining</summary>
    PursuingGoal = 1,

    /// <summary>All goals met, available actions to pursue</summary>
    ActNow = 2,

    /// <summary>All goals met, all available actions taken</summary>
    JustResponsiveness = 3
}

/// <summary>
/// Utilities for evaluation context determination and prompt assembly.
/// Port of eval_context.py and eval_prompt.py.
/// </summary>
public static class EvalContextUtil
{
    /// <summary>
    /// Determine the evaluation context based on unmet goals and available actions.
    /// Port of determine_eval_context() from utils/eval_context.py.
    /// </summary>
    public static EvalContextType DetermineEvalContext(
        List<string> unmetGoals,
        Dictionary<string, string> availableActions)
    {
        if (unmetGoals.Count > 0)
            return EvalContextType.PursuingGoal;
        if (availableActions.Count > 0)
            return EvalContextType.ActNow;
        return EvalContextType.JustResponsiveness;
    }

    /// <summary>
    /// Build the evaluation context text for the prompt.
    /// Port of make_evaluator_context() from utils/eval_prompt.py.
    /// </summary>
    /// <param name="evalCondition">Current eval context type</param>
    /// <param name="parameterSetting">"strict" or "lenient"</param>
    /// <param name="vocabList">Advanced concepts vocabulary list</param>
    /// <param name="evalBaseText">Contents of EVAL_BASE.txt</param>
    /// <param name="criteriaFullText">Contents of CRITERIA_FULL.txt</param>
    /// <param name="criteriaRespOnlyText">Contents of CRITERIA_RESP_ONLY.txt</param>
    /// <param name="reflectionStrictText">Contents of REFLECTION_STRICT.txt</param>
    /// <param name="reflectionLenientText">Contents of REFLECTION_LENIENT.txt</param>
    public static string MakeEvaluatorContext(
        EvalContextType evalCondition,
        string? parameterSetting,
        List<string>? vocabList,
        string evalBaseText,
        string criteriaFullText,
        string criteriaRespOnlyText,
        string reflectionStrictText,
        string reflectionLenientText)
    {
        var criteriaBlock = evalCondition == EvalContextType.JustResponsiveness
            ? criteriaRespOnlyText
            : criteriaFullText;

        var vocabStr = vocabList != null ? string.Join(", ", vocabList) : "";

        string reflectionBlock;
        switch (parameterSetting)
        {
            case "strict":
                reflectionBlock = reflectionStrictText.Replace("{vocab_list}", vocabStr);
                break;
            case "lenient":
                reflectionBlock = reflectionLenientText.Replace("{vocab_list}", vocabStr);
                break;
            default:
                reflectionBlock = "";
                break;
        }

        return evalBaseText
            .Replace("{criteria_block}", criteriaBlock)
            .Replace("{reflection_block}", reflectionBlock);
    }
}
