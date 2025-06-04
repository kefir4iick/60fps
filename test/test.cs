using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;
using AudioSynthApp;

public class FpsLoopTests
{
    [Fact]
    public void FpsTest()
    {
        var synthesizer = new AudioSynthesizer();
        Console.WriteLine($"driver: {GetAudioDriverName(synthesizer)}");
        
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
    private string GetAudioDriverName(AudioSynthesizer synth)
    {
        var field = typeof(AudioSynthesizer).GetField("_audioOutput", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        var audioOutput = field?.GetValue(synth);
        return audioOutput?.GetType().Name.Replace("Wrapper", "") ?? "Unknown";
    }
}
