using UnityEngine;
using System.IO;
using System;

public static class WavUtility
{
    const int HEADER_SIZE = 44;

    public static byte[] FromAudioClip(AudioClip clip, out int length, bool trim = true)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        if (trim)
        {
            int trimIndex = samples.Length;
            for (int i = samples.Length - 1; i >= 0; i--)
            {
                if (Mathf.Abs(samples[i]) > 0.0001f)
                {
                    trimIndex = i + 1;
                    break;
                }
            }

            if (trimIndex != samples.Length)
            {
                Array.Resize(ref samples, trimIndex);
            }
        }

        byte[] wav = ConvertAudioClipDataToInt16ByteArray(samples);
        byte[] header = GetWavHeader(wav.Length, clip.channels, clip.frequency);
        byte[] result = new byte[header.Length + wav.Length];
        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        Buffer.BlockCopy(wav, 0, result, header.Length, wav.Length);

        length = result.Length;
        return result;
    }

    private static byte[] GetWavHeader(int dataLength, int channels, int sampleRate)
    {
        int byteRate = sampleRate * channels * 2;
        int fileSize = dataLength + HEADER_SIZE - 8;

        using (MemoryStream stream = new MemoryStream(HEADER_SIZE))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(fileSize);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16); // Subchunk1Size
            writer.Write((ushort)1); // AudioFormat PCM
            writer.Write((ushort)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((ushort)(channels * 2)); // BlockAlign
            writer.Write((ushort)16); // BitsPerSample
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(dataLength);
            return stream.ToArray();
        }
    }

    private static byte[] ConvertAudioClipDataToInt16ByteArray(float[] data)
    {
        byte[] bytes = new byte[data.Length * 2];
        int rescaleFactor = 32767;

        for (int i = 0; i < data.Length; i++)
        {
            short value = (short)(data[i] * rescaleFactor);
            byte[] byteArr = BitConverter.GetBytes(value);
            bytes[i * 2] = byteArr[0];
            bytes[i * 2 + 1] = byteArr[1];
        }

        return bytes;
    }
}
