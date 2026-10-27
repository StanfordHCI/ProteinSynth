/*
    BirdSamples.cs: genotypes the student has sampled, kept across scene loads
    - Filled by BirdScanManager; the Punnett square step can pick parents from it
    - Exposed to Yarn as bird_samples_collected() and bird_sample_genotype(index)
*/

using System.Collections.Generic;
using Yarn.Unity;

public static class BirdSamples
{
    public static readonly List<string> Genotypes = new List<string>();

    [YarnFunction("bird_samples_collected")]
    public static int Collected() => Genotypes.Count;

    // 0-based index; returns "" when out of range so Yarn scripts can check safely
    [YarnFunction("bird_sample_genotype")]
    public static string GenotypeAt(int index)
    {
        return index >= 0 && index < Genotypes.Count ? Genotypes[index] : "";
    }
}
