using UnityEngine;
using UnityEngine.UI;

public class QRManager : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;

    async void Start()
    {
        string scannedCode = "STUDENT_001";

        // 1. Get student from database
        StudentRecord student =
            await SupabaseManager.Instance
                .GetStudent(scannedCode);

        if (student == null)
        {
            Debug.LogError("NOT FOUND");
            return;
        }

        Debug.Log($"Found: {student.Name}");

        // 2. Download image from Supabase Storage
        Texture2D texture =
            await SupabaseManager.Instance
                .DownloadImage(student.ImagePath);

        if (texture == null)
        {
            Debug.LogError("Image download failed");
            return;
        }

        // 3. Apply texture directly
        targetRenderer.material.mainTexture = texture;

        Debug.Log("Artwork applied successfully");
    }
}