using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BakeBeatAnalysis
{
    [MenuItem("Tools/Audio/Bake Selected Music Beat Data")]
    public static void BakeSelected()
    {
        var clip = Selection.activeObject as AudioClip;
        string input = clip != null ? AssetDatabase.GetAssetPath(clip) : "Assets/Data/BackOnTrack.mp3";
        string pcmPath = Path.GetFullPath("Temp/BeatBake.pcm");
        var start = new System.Diagnostics.ProcessStartInfo("ffmpeg",
            "-hide_banner -loglevel error -i \"" + Path.GetFullPath(input) + "\" -f f32le -ac 2 -ar 44100 -y \"" + pcmPath + "\"")
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        using (var process = System.Diagnostics.Process.Start(start))
        {
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0) throw new Exception(error);
        }
        BakePcm(pcmPath, "Assets/Resources/" + Path.GetFileNameWithoutExtension(input) + "Beat.bytes");
        AssetDatabase.Refresh();
    }

    public static void BakePcm(string pcmPath, string output)
    {
        const int frames = 4096, frequency = 44100;
        const float rate = 60f;
        byte[] bytes = File.ReadAllBytes(pcmPath);
        float[] pcm = new float[bytes.Length / 4];
        Buffer.BlockCopy(bytes, 0, pcm, 0, bytes.Length);
        int sourceFrames = pcm.Length / 2;
        int count = (int)Math.Ceiling(sourceFrames / (double)frequency * rate) + 1;
        var window = new double[frames];
        for (int i = 0; i < frames; i++)
        {
            double phase = 2 * Math.PI * i / (frames - 1);
            window[i] = 0.35875 - 0.48829 * Math.Cos(phase) + 0.14128 * Math.Cos(2 * phase) - 0.01168 * Math.Cos(3 * phase);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        using (var writer = new BinaryWriter(File.Create(output)))
        {
            writer.Write(0x42505431);
            writer.Write(rate);
            writer.Write(count);
            for (int frame = 0; frame < count; frame++)
            {
                int position = Math.Min(sourceFrames - 1, (int)(frame / rate * frequency));
                int offset = Math.Max(0, position - frames);
                double rmsL = 0, rmsR = 0;
                for (int i = 0; i < frames; i++)
                {
                    int index = Math.Min(sourceFrames - 1, offset + i) * 2;
                    rmsL += pcm[index] * pcm[index];
                    rmsR += pcm[index + 1] * pcm[index + 1];
                }
                double sumL = 0, sumR = 0, peakL = 0, peakR = 0;
                int first = Math.Max(1, (int)Math.Floor(40.0 * frames / frequency));
                int last = (int)Math.Ceiling(180.0 * frames / frequency);
                for (int bin = first; bin <= last; bin++)
                {
                    double step = 2 * Math.PI * bin / frames;
                    double sc = Math.Cos(step), ss = Math.Sin(step), oc = 1, os = 0;
                    double lr = 0, li = 0, rr = 0, ri = 0;
                    for (int i = 0; i < frames; i++)
                    {
                        int index = Math.Min(sourceFrames - 1, offset + i) * 2;
                        double c = oc * window[i], s = os * window[i];
                        lr += pcm[index] * c; li += pcm[index] * s;
                        rr += pcm[index + 1] * c; ri += pcm[index + 1] * s;
                        double next = oc * sc - os * ss;
                        os = os * sc + oc * ss; oc = next;
                    }
                    double l = Math.Sqrt(lr * lr + li * li) / frames;
                    double r = Math.Sqrt(rr * rr + ri * ri) / frames;
                    sumL += l; sumR += r; peakL = Math.Max(peakL, l); peakR = Math.Max(peakR, r);
                }
                writer.Write((float)Math.Sqrt(rmsL / frames));
                writer.Write((float)Math.Sqrt(rmsR / frames));
                writer.Write((float)(sumL / (last - first + 1)));
                writer.Write((float)(sumR / (last - first + 1)));
                writer.Write((float)peakL); writer.Write((float)peakR);
            }
        }
        Debug.Log("Baked volume-independent beat data: " + output);
    }
}