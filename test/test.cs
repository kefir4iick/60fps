using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection; 
using System.Threading;
using Xunit;
using AudioSynthApp;
using NAudio.CoreAudioApi;
using NAudio.Wave;

public class FpsLoopTests
{
    [Fact]
    public void FpsTest()
    {
        Console.WriteLine("=== WASAPI Diagnostic Info ===");
        try
        {
            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                var defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                Console.WriteLine($"Default audio device: {defaultDevice.FriendlyName}");
                Console.WriteLine($"State: {defaultDevice.State}");
                Console.WriteLine($"Shared mode supported: {defaultDevice.AudioClient.IsFormatSupported(AudioClientShareMode.Shared, new WaveFormat(44100, 16, 1))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WASAPI diagnostic failed: {ex.Message}");
        }
        
        var synth = new AudioSynthesizer();
        
        var audioOutput = synth.GetType()
                             .GetField("_audioOutput", BindingFlags.NonPublic | BindingFlags.Instance)
                             .GetValue(synth);

        var realDriver = audioOutput?.GetType()
                                   .GetField("_waveOut", BindingFlags.NonPublic | BindingFlags.Instance)
                                   .GetValue(audioOutput)
                                   ?.GetType()
                                   .Name;

        Console.WriteLine($"driver: {realDriver ?? "Unknown"}");
        
        const int targetFps = 60;
        const int testDurationMs = 5000; 
        var frameTimes = new double[targetFps * 5]; 
        int frameCount = 0;
        var stopwatch = new Stopwatch();

        var loop = new FpsBasedLoop(
            callback: () =>
            {
                if (stopwatch.IsRunning && frameCount < frameTimes.Length)
                {
                    frameTimes[frameCount++] = stopwatch.Elapsed.TotalMilliseconds;
                    stopwatch.Restart();
                }
            },
            targetFps: targetFps
        );

        stopwatch.Start();
        loop.Start();
        Thread.Sleep(testDurationMs);
        loop.Stop();

        var relevantFrames = frameTimes
            .Skip(10)
            .Where(t => t > 0)
            .Take(frameCount - 10)
            .ToArray();

        var fpsValues = relevantFrames.Select(t => 1000 / t).ToArray();
        var minFps = fpsValues.Min();
        var avgFps = fpsValues.Average();
        var maxFps = fpsValues.Max();

        Console.WriteLine($"fps stats {testDurationMs} ms:");
        Console.WriteLine($"- average: {avgFps:F2}");
        Console.WriteLine($"- min: {minFps:F2}");
        Console.WriteLine($"- max: {maxFps:F2}");

        Assert.InRange(avgFps, targetFps - 5, targetFps + 5);
    }
}
