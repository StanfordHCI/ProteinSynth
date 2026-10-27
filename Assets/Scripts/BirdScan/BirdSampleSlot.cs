/*
    BirdSampleSlot.cs: one square in the "Collect 6 samples" row
    - Empty: plain background. Filled: tinted background, bird illustration and a genotype label.
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BirdSampleSlot : MonoBehaviour
{
    public Image background;
    public Image portrait;
    public GameObject label;
    public TextMeshProUGUI genotypeText;

    public Color emptyColor = new Color(0.96f, 0.96f, 0.96f, 0.92f);

    public void Fill(Sprite illustration, string genotype, Color filledColor)
    {
        background.color = filledColor;
        portrait.sprite = illustration;
        portrait.gameObject.SetActive(illustration != null);
        genotypeText.text = genotype;
        label.SetActive(true);
    }

    public void Clear()
    {
        background.color = emptyColor;
        portrait.gameObject.SetActive(false);
        label.SetActive(false);
    }
}
