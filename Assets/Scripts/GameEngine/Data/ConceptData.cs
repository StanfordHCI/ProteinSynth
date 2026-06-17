using System.Collections.Generic;

namespace GameEngine.Data;

/// <summary>
/// Concept vocabulary data for protein synthesis tutoring.
/// Port of concept_utils.py constants.
/// </summary>
public static class ConceptData
{
    /// <summary>Advanced scientific terms the tutor should handle carefully</summary>
    public static readonly List<string> AdvancedConcepts = new()
    {
        "protein synthesis", "transcription", "translation",
        "ribosomes", "mRNA", "tRNA", "amino acids", "codon"
    };

    /// <summary>Foundational concepts that map to everyday language</summary>
    public static readonly HashSet<string> FoundationalConcepts = new()
    {
        "cell", "nucleus", "instruction", "building blocks", "growth", "jobs"
    };

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
