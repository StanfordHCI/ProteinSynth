using System.Collections.Generic;

namespace GameEngine.Data;

/// <summary>
/// Student concept-language tracking. Vocabulary lists belong to each activity.
/// Port of concept_utils.py.
/// </summary>
public static class ConceptData
{
    /// <summary>Entry in the student's concept language dictionary</summary>
    public class PhraseEntry
    {
        public string Phrase { get; set; } = "";
        public string Source { get; set; } = "student"; // "student" or "default"
    }

    /// <summary>
    /// Merge pending phrase updates into the student concept language dictionary.
    /// Port of merge_phrase_updates from reflexion.py.
    /// </summary>
    public static void MergePhraseUpdates(
        Dictionary<string, List<PhraseEntry>> studentConceptLanguage,
        List<Models.PhraseUpdate>? updates)
    {
        if (updates == null) return;

        foreach (var update in updates)
        {
            var concept = update.Concept?.Trim();
            var phrase = update.Phrase?.Trim();
            if (string.IsNullOrEmpty(concept) || string.IsNullOrEmpty(phrase))
                continue;

            if (!studentConceptLanguage.ContainsKey(concept))
                studentConceptLanguage[concept] = new List<PhraseEntry>();

            studentConceptLanguage[concept].Add(new PhraseEntry
            {
                Phrase = phrase,
                Source = "student"
            });
        }
    }
}
