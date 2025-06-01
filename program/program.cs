using System;
using System.Threading;
using NAudio.Wave;

class AudioSynth
{
    private const int TargetFps = 60;
    private const double TargetFrameTime = 1000.0 / TargetFps;
    
    private bool _isRunning;
    private Thread _audioThread;
    private IWavePlayer _waveOut;
    private BufferedWaveProvider _waveProvider;
    private double _phase;
    private double _frequency = 440.0;
    private int _sampleRate = 44100;
    private int _bufferSize;

    public AudioSynth()
    {
        _waveProvider = new BufferedWaveProvider(new WaveFormat(_sampleRate, 16, 1))
        {
            BufferDuration = TimeSpan.FromMilliseconds(500) 
        };

        try
        {
            _waveOut = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 100);
        }
        catch
        {
            try
            {
                _waveOut = new DirectSoundOut();
            }
            catch
            {
                _waveOut = new WaveOutEvent();
            }
        }

        _waveOut.Init(_waveProvider);
        _bufferSize = _sampleRate / TargetFps;
    }

    public void Start()
    {
        if (_isRunning) return;
        
        _isRunning = true;
        _waveOut.Play();
        _audioThread = new Thread(RunAudioLoop)
        {
            Priority = ThreadPriority.Highest
        };
        _audioThread.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _audioThread?.Join();
        _waveOut.Stop();
        _waveOut.Dispose();
    }

    public void SetFrequency(double freq)
    {
        _frequency = freq;
    }

    private void RunAudioLoop()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        double previousTime = stopwatch.Elapsed.TotalMilliseconds;
        
        while (_isRunning)
        {
            double currentTime = stopwatch.Elapsed.TotalMilliseconds;
            double elapsedTime = currentTime - previousTime;
            previousTime = currentTime;
            
            if (_waveProvider.BufferedDuration.TotalMilliseconds < 300) 
            {
                GenerateAudioFrame();
            }
            
            double frameTime = stopwatch.Elapsed.TotalMilliseconds - currentTime;
            if (frameTime < TargetFrameTime)
            {
                int sleepTime = (int)(TargetFrameTime - frameTime);
                Thread.Sleep(sleepTime);
            }
        }
    }

    private void GenerateAudioFrame()
    {
        byte[] buffer = new byte[_bufferSize * 2];
        
        for (int i = 0; i < _bufferSize; i++)
        {
            double sample = Math.Sin(_phase) * 0.9;
            short pcm = (short)(sample * short.MaxValue);
            buffer[i * 2] = (byte)(pcm & 0xFF);
            buffer[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            
            _phase += 2 * Math.PI * _frequency / _sampleRate;
            if (_phase > 2 * Math.PI) _phase -= 2 * Math.PI;
        }
        
        _waveProvider.AddSamples(buffer, 0, buffer.Length);
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("Q - 400 hz | W - 500 hz | ESC - exit");

        var synth = new AudioSynth();
        synth.Start();
        
        while (true)
        {
            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.Q:
                    synth.SetFrequency(400.0);
                    Console.WriteLine("frequency: 400 hz");
                    break;
                case ConsoleKey.W:
                    synth.SetFrequency(500.0);
                    Console.WriteLine("frequency: 500 hz");
                    break;
                case ConsoleKey.E:
                    synth.Stop();
                    return;
            }
        }
    }
}
