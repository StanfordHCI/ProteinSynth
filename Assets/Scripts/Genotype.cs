/*
    Genotype.cs file: shared genetics helpers for the Punnett square lab (complete dominance, one gene).
    - Genotypes are stored as canonical strings: "BB", "Bb", "bb"
    - B = blue feathers (dominant), b = gold feathers (rare recessive)
    - Converts Vuforia card target names into canonical genotypes and builds Punnett squares from two parents
*/

using UnityEngine;

public static class Genotype
{
    public const string BB = "BB";
    public const string Bb = "Bb";
    public const string bb = "bb";

    public const string TraitDescription = "Feather color: B = blue (dominant), b = gold (recessive)";

    private static readonly char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

    // Convert a Vuforia target name into a canonical genotype.
    // Card targets are named with "_" after uppercase letters (target names are case-insensitive) and
    // a trailing number for duplicate cards, e.g. "B_B_", "B_b2", "bb1". The old A/a card names are
    // still accepted so the current printed cards keep working until they are reprinted.
    public static string FromTargetName(string targetName)
    {
        string name = targetName.TrimEnd(digits);
        switch (name)
        {
            case "B_B_":
            case "A_A_":
                return BB;
            case "B_b":
            case "A_a":
                return Bb;
            case "bb":
            case "aa":
                return bb;
            default:
                Debug.LogWarning($"Genotype: unknown card target name {targetName}");
                return name;
        }
    }

    // Combine two alleles with the dominant allele written first, e.g. ('b', 'B') -> "Bb"
    public static string FromAlleles(char a, char b)
    {
        return char.IsUpper(a) || !char.IsUpper(b) ? $"{a}{b}" : $"{b}{a}";
    }

    public static bool IsRecessivePhenotype(string genotype) => genotype == bb;

    public static string PhenotypeName(string genotype) => IsRecessivePhenotype(genotype) ? "Gold" : "Blue";

    // Build the 2x2 Punnett square for two parents. Parent 1's alleles run along the columns
    // (left -> right) and parent 2's alleles along the rows (top -> bottom), dominant allele first,
    // matching how PunnettSquareTracker sorts the cards on the paper grid.
    public static string[,] Cross(string parent1, string parent2)
    {
        string cols = SortedAlleles(parent1);
        string rows = SortedAlleles(parent2);

        string[,] square = new string[2, 2];
        for (int r = 0; r < 2; r++)
            for (int c = 0; c < 2; c++)
                square[r, c] = FromAlleles(cols[c], rows[r]);
        return square;
    }

    // Dominant allele first, e.g. "bB" -> "Bb"
    private static string SortedAlleles(string genotype) => FromAlleles(genotype[0], genotype[1]);
}
