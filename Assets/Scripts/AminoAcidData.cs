using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AminoAcidData : MonoBehaviour
{
    // Codon → Amino Acid abbreviation
    private readonly Dictionary<string, string> codonToAminoAcid = new Dictionary<string, string>()
    {
        // Phenylalanine
        { "UUU", "Phe" }, { "UUC", "Phe" },

        // Leucine
        { "UUA", "Leu" }, { "UUG", "Leu" },
        { "CUU", "Leu" }, { "CUC", "Leu" }, { "CUA", "Leu" }, { "CUG", "Leu" },

        // Isoleucine
        { "AUU", "Ile" }, { "AUC", "Ile" }, { "AUA", "Ile" },

        // Methionine (Start)
        { "AUG", "Met" },

        // Valine
        { "GUU", "Val" }, { "GUC", "Val" }, { "GUA", "Val" }, { "GUG", "Val" },

        // Serine
        { "UCU", "Ser" }, { "UCC", "Ser" }, { "UCA", "Ser" }, { "UCG", "Ser" },
        { "AGU", "Ser" }, { "AGC", "Ser" },

        // Proline
        { "CCU", "Pro" }, { "CCC", "Pro" }, { "CCA", "Pro" }, { "CCG", "Pro" },

        // Threonine
        { "ACU", "Thr" }, { "ACC", "Thr" }, { "ACA", "Thr" }, { "ACG", "Thr" },

        // Alanine
        { "GCU", "Ala" }, { "GCC", "Ala" }, { "GCA", "Ala" }, { "GCG", "Ala" },

        // Tyrosine
        { "UAU", "Tyr" }, { "UAC", "Tyr" },

        // Histidine
        { "CAU", "His" }, { "CAC", "His" },

        // Glutamine
        { "CAA", "Gln" }, { "CAG", "Gln" },

        // Asparagine
        { "AAU", "Asn" }, { "AAC", "Asn" },

        // Lysine
        { "AAA", "Lys" }, { "AAG", "Lys" },

        // Aspartic acid
        { "GAU", "Asp" }, { "GAC", "Asp" },

        // Glutamic acid
        { "GAA", "Glu" }, { "GAG", "Glu" },

        // Cysteine
        { "UGU", "Cys" }, { "UGC", "Cys" },

        // Tryptophan
        { "UGG", "Trp" },

        // Arginine
        { "CGU", "Arg" }, { "CGC", "Arg" }, { "CGA", "Arg" }, { "CGG", "Arg" },
        { "AGA", "Arg" }, { "AGG", "Arg" },

        // Glycine
        { "GGU", "Gly" }, { "GGC", "Gly" }, { "GGA", "Gly" }, { "GGG", "Gly" },

        // Stop codons
        { "UAA", "Stop" }, { "UAG", "Stop" }, { "UGA", "Stop" }
    };

    // Amino Acid abbreviation → Color
    private readonly Dictionary<string, Color> aminoAcidColors = new Dictionary<string, Color>()
    {
        { "Phe", new Color(0.95f, 0.6f, 0.6f) },   // soft rose
        { "Leu", new Color(0.9f, 0.5f, 0.3f) },    // burnt orange
        { "Ile", new Color(0.8f, 0.8f, 0.3f) },    // olive yellow
        { "Met", new Color(0.3f, 0.9f, 0.3f) },    // fresh green
        { "Val", new Color(0.7f, 0.8f, 0.4f) },    // moss green
        { "Ser", new Color(0.3f, 0.8f, 1.0f) },    // sky blue
        { "Pro", new Color(0.8f, 0.4f, 0.7f) },    // orchid pink
        { "Thr", new Color(1.0f, 0.4f, 0.4f) },    // coral red
        { "Ala", new Color(0.4f, 0.9f, 0.7f) },    // mint green
        { "Tyr", new Color(0.9f, 0.5f, 0.9f) },    // lavender
        { "His", new Color(0.7f, 0.3f, 0.6f) },    // plum
        { "Gln", new Color(0.5f, 0.4f, 0.9f) },    // violet
        { "Asn", new Color(0.5f, 0.7f, 1.0f) },    // powder blue
        { "Lys", new Color(0.7f, 0.5f, 1.0f) },    // lilac
        { "Asp", new Color(1.0f, 0.5f, 0.3f) },    // warm orange
        { "Glu", new Color(1.0f, 0.3f, 0.3f) },    // crimson red
        { "Cys", new Color(1.0f, 0.9f, 0.3f) },    // golden yellow
        { "Trp", new Color(0.3f, 0.2f, 0.8f) },    // deep indigo
        { "Arg", new Color(0.4f, 0.2f, 1.0f) },    // rich blue-violet
        { "Gly", new Color(0.3f, 0.6f, 1.0f) },    // cerulean blue
        { "Stop", Color.black }
    };

    /// <summary>
    /// Convert an anticodon (tRNA) to its complementary codon (mRNA).
    /// A ↔ U, G ↔ C.
    /// </summary>
    private string AnticodonToCodon(string anticodon)
    {
        anticodon = anticodon.ToUpper();
        char[] codon = new char[3];

        for (int i = 0; i < 3; i++)
        {
            switch (anticodon[i])
            {
                case 'A': codon[i] = 'U'; break;
                case 'U': codon[i] = 'A'; break;
                case 'G': codon[i] = 'C'; break;
                case 'C': codon[i] = 'G'; break;
                default: codon[i] = 'N'; break; // Unknown
            }
        }

        return new string(codon);
    }

    /// <summary>
    /// Returns the amino acid abbreviation and color for a given anticodon (e.g. "UAC" → "Met", color).
    /// </summary>
    public (string aminoAcid, Color color) GetAminoAcidFromAnticodon(string anticodon)
    {
        if (string.IsNullOrEmpty(anticodon) || anticodon.Length != 3)
            return (null, Color.white);

        string codon = AnticodonToCodon(anticodon);

        if (codonToAminoAcid.TryGetValue(codon, out string aa))
        {
            if (aminoAcidColors.TryGetValue(aa, out Color c))
                return (aa, c);
            else
                return (aa, Color.white);
        }

        return (null, Color.white);
    }

    /// <summary>
    /// Returns the amino acid abbreviation for a given codon (e.g "AUG" → "Met")
    /// </summary>
    public string GetAminoAcidNameFromCodon(string codon)
    {
        codon = codon.ToUpper();

        if (codonToAminoAcid.TryGetValue(codon, out string aa))
            return aa;

        return null;
    }

}
