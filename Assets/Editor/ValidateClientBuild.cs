using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor.Build;
using UnityEngine;
using GameEngine.Services;

// Package managed-device configuration without putting credentials in versioned Assets.
public class ValidateClientBuild : BuildPlayerProcessor
{
    public override int callbackOrder => 0;
    public override void PrepareForBuild(BuildPlayerContext context)
    {
        foreach (var path in Directory.GetFiles(Application.streamingAssetsPath, "*.json", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            if (text.Contains("anthropic_api_key") || text.Contains("openai_api_key")
                || text.Contains("service_role") || text.Contains("supabase_service_key") || text.Contains("supabase_key"))
                throw new BuildFailedException("Keep credential sources in .local/config.json, outside versioned StreamingAssets: " + Path.GetFileName(path));
        }
        var config = JsonConvert.DeserializeObject<ClientConfiguration>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "client-settings.json")))
            ?? new ClientConfiguration();
        var privateDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../.local"));
        var privateConfig = Path.Combine(privateDirectory, "config.json");
        if (File.Exists(privateConfig)) config.ApplyOverrides(File.ReadAllText(privateConfig));
        if (string.IsNullOrWhiteSpace(config.anthropic_api_key)) config.anthropic_api_key = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(config.openai_api_key)) config.openai_api_key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!Uri.TryCreate(config.supabase_url, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            throw new BuildFailedException("Configure an HTTPS supabase_url before building.");
        try { ClientConfiguration.ValidateSupabaseKey(config.SupabaseKey); config.ValidateProviderKeys(); }
        catch (Exception e) { throw new BuildFailedException(e.Message); }
        var generatedDirectory = Path.Combine(privateDirectory, "build");
        Directory.CreateDirectory(generatedDirectory);
        var generatedConfig = Path.Combine(generatedDirectory, "managed-client-settings.json");
        File.WriteAllText(generatedConfig, JsonConvert.SerializeObject(config));
        context.AddAdditionalPathToStreamingAssets(generatedConfig, "managed-client-settings.json");
    }
}
