using UnityEngine;
using Supabase;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine.Networking;

public class SupabaseManager : MonoBehaviour
{
    public static SupabaseManager Instance;

    public Client Client;

    [Header("Supabase")]
    [SerializeField]
    private string supabaseUrl =
        "https://igcfiiiflropjisbdkmj.supabase.co";

    [SerializeField]
    private string supabaseAnonKey =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImlnY2ZpaWlmbHJvcGppc2Jka21qIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODA0NTA1MTEsImV4cCI6MjA5NjAyNjUxMX0.G92-H6Dxb-jJTb-MAGNTQad5iA7MW64Do4SGTcpIwFw";

    async void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        var options = new SupabaseOptions
        {
            AutoConnectRealtime = false
        };

        Client = new Client(
            supabaseUrl,
            supabaseAnonKey,
            options
        );

        await Client.InitializeAsync();

        Debug.Log("Supabase Connected");
    }

    public async Task<StudentRecord> GetStudent(
        string studentCode)
    {
        var response = await Client
            .From<StudentRecord>()
            .Filter(
                "student_code",
                Supabase.Postgrest.Constants.Operator.Equals,
                studentCode
            )
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<Texture2D> DownloadStudentImage(
        string imageKey)
    {
        string imageUrl =
            Client.Storage
                .From("student-images")
                .GetPublicUrl(imageKey);

        using UnityWebRequest request =
            UnityWebRequestTexture.GetTexture(
                imageUrl);

        var operation =
            request.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();

        if (request.result !=
            UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
            return null;
        }

        return DownloadHandlerTexture
            .GetContent(request);
    }

    public async Task<Texture2D> DownloadImage(string imagePath) {
        // Get public URL from Supabase Storage
        string url = Client.Storage
            .From("drawings")
            .GetPublicUrl(imagePath);

        using UnityWebRequest request =
            UnityWebRequestTexture.GetTexture(url);

        var op = request.SendWebRequest();

        while (!op.isDone)
            await Task.Yield();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Image download failed: " + request.error);
            return null;
        }

        return DownloadHandlerTexture.GetContent(request);
    }

}