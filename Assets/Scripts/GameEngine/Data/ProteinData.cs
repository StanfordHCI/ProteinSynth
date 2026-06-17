using System.Collections.Generic;

namespace GameEngine.Data;

/// <summary>
/// Protein list and related constants.
/// Port of protein_selection.py.
/// </summary>
public static class ProteinData
{
    public static readonly List<string> ProteinsList = new()
    {
        "lactase", "hemoglobin", "insulin", "myosin",
        "keratin", "immunoglobulins", "tyrosinase", "cytokines"
    };
}
