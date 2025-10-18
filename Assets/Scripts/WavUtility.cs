using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static AudioClip ToAudioClip(byte[] wavFile, string name = "wav", int offsetSamples = 0)
    {
        try
        {
            using (var stream = new MemoryStream(wavFile))
            using (var reader = new BinaryReader(stream))
            {
                // Validate minimum file size
                if (wavFile.Length < 44)
                {
                    Debug.LogError($"WAV file too small: {wavFile.Length} bytes (minimum 44 required)");
                    return null;
                }

                // Debug: Log first 32 bytes to help identify format issues
                Debug.Log($"WAV file size: {wavFile.Length} bytes");
                string firstBytes = "";
                for (int i = 0; i < Math.Min(32, wavFile.Length); i++)
                {
                    firstBytes += wavFile[i].ToString("X2") + " ";
                }
                Debug.Log($"First 32 bytes: {firstBytes}");

                // Read RIFF header
                string riffHeader = new string(reader.ReadChars(4));
                if (riffHeader != "RIFF")
                {
                    Debug.LogError($"Invalid WAV file: missing RIFF header, found '{riffHeader}'");
                    return null;
                }

                int fileSize = reader.ReadInt32(); // File size
                Debug.Log($"RIFF file size field: {fileSize} (0x{fileSize:X8})");
                
                // Handle streaming WAV where file size is unknown (-1)
                if (fileSize == -1)
                {
                    Debug.Log("Detected streaming WAV with unknown file size (-1)");
                }
                
                string waveHeader = new string(reader.ReadChars(4));
                if (waveHeader != "WAVE")
                {
                    Debug.LogError($"Invalid WAV file: missing WAVE header, found '{waveHeader}'");
                    return null;
                }

                // Find format chunk
                string chunkID = new string(reader.ReadChars(4));
                while (chunkID != "fmt ")
                {
                    if (reader.BaseStream.Position >= reader.BaseStream.Length - 8)
                    {
                        Debug.LogError("WAV file corrupted: could not find format chunk");
                        return null;
                    }
                    int chunkSize = reader.ReadInt32();
                    reader.ReadBytes(chunkSize);
                    chunkID = new string(reader.ReadChars(4));
                }

                int formatChunkSize = reader.ReadInt32();
                reader.ReadInt16(); // Audio format
                int channels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // Byte rate
                reader.ReadInt16(); // Block align
                int bitsPerSample = reader.ReadInt16();

                // Validate audio parameters
                if (channels <= 0 || channels > 2)
                {
                    Debug.LogError($"Invalid channel count: {channels}");
                    return null;
                }
                if (sampleRate <= 0)
                {
                    Debug.LogError($"Invalid sample rate: {sampleRate}");
                    return null;
                }
                if (bitsPerSample != 8 && bitsPerSample != 16)
                {
                    Debug.LogError($"Unsupported bits per sample: {bitsPerSample}");
                    return null;
                }

                // Find "data" chunk
                chunkID = new string(reader.ReadChars(4));
                Debug.Log($"Looking for data chunk, found: '{chunkID}'");
                
                while (chunkID != "data")
                {
                    if (reader.BaseStream.Position >= reader.BaseStream.Length - 8)
                    {
                        Debug.LogError("WAV file corrupted: could not find data chunk");
                        return null;
                    }
                    int chunkSize = reader.ReadInt32();
                    Debug.Log($"Found chunk '{chunkID}' with size: {chunkSize}");
                    
                    if (chunkSize < 0 || reader.BaseStream.Position + chunkSize > reader.BaseStream.Length)
                    {
                        Debug.LogError($"Invalid chunk size: {chunkSize}");
                        return null;
                    }
                    reader.ReadBytes(chunkSize);
                    chunkID = new string(reader.ReadChars(4));
                }

                int dataSize = reader.ReadInt32();
                Debug.Log($"Data chunk size: {dataSize} (0x{dataSize:X8})");
                
                // Handle streaming WAV where data size might be unknown (-1) or invalid
                if (dataSize <= 0)
                {
                    if (dataSize == -1)
                    {
                        Debug.Log("Data chunk size is -1 (unknown), using remaining file data");
                        dataSize = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
                    }
                    else
                    {
                        Debug.LogError($"Invalid data size: {dataSize}");
                        return null;
                    }
                }
                
                if (reader.BaseStream.Position + dataSize > reader.BaseStream.Length)
                {
                    Debug.LogWarning($"Data size exceeds file size: {dataSize} bytes requested, {reader.BaseStream.Length - reader.BaseStream.Position} bytes available. Using available data.");
                    dataSize = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
                }

                byte[] data = reader.ReadBytes(dataSize);
                Debug.Log($"Reading {dataSize} bytes of audio data");

                float[] samples = ConvertToFloatArray(data, bitsPerSample);

                AudioClip audioClip = AudioClip.Create(name, samples.Length / channels, channels, sampleRate, false);
                audioClip.SetData(samples, offsetSamples);
                return audioClip;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing WAV file: {ex.Message}");
            return null;
        }
    }

    private static float[] ConvertToFloatArray(byte[] source, int bitsPerSample)
    {
        int bytesPerSample = bitsPerSample / 8;
        int sampleCount = source.Length / bytesPerSample;
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            int sampleInt = 0;

            if (bytesPerSample == 2)
            {
                sampleInt = BitConverter.ToInt16(source, i * bytesPerSample);
                samples[i] = sampleInt / 32768f;
            }
            else if (bytesPerSample == 1)
            {
                samples[i] = (source[i] - 128) / 128f;
            }
        }

        return samples;
    }
}