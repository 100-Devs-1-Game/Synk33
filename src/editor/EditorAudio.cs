using System;
using Godot;

namespace SYNK33.editor;

public static class EditorAudio {
    public static void CreateHitSound(AudioStreamPlayer player, AudioStream? userStream = null) {
        if (userStream != null) {
            GD.Print("Using user-provided tap sound stream for editor");
            player.Stream = userStream;
            player.VolumeDb = 0;
            return;
        }

        const double sampleHz = 44100.0;
        const double frequency = 800.0;
        const double duration = 0.05;
        const double amplitude = 0.5;

        var stream = CreateAudioStream(sampleHz, duration, false);
        var data = GenerateSineWaveWithDecay(sampleHz, frequency, duration, amplitude);
        
        stream.Data = data;
        player.Stream = stream;
        player.VolumeDb = 0;
        GD.Print("Created procedural tap sound for editor");
    }

    public static void CreateHoldSound(AudioStreamPlayer player, AudioStream? userStream = null) {
        if (userStream != null) {
            GD.Print("Using user-provided hold sound stream for editor");
            player.Stream = userStream;
            player.VolumeDb = -6;
            player.StreamPaused = false;
            return;
        }

        const double sampleHz = 44100.0;
        const double frequency = 400.0;
        const double duration = 1.0;
        const double amplitude = 0.15;

        var stream = CreateAudioStream(sampleHz, duration, true);
        var data = GenerateContinuousSineWave(sampleHz, frequency, duration, amplitude);
        
        stream.Data = data;
        player.Stream = stream;
        player.VolumeDb = -12;
        GD.Print("Created procedural hold sound for editor");
    }

    private static AudioStreamWav CreateAudioStream(double sampleHz, double duration, bool looping) {
        var stream = new AudioStreamWav {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = (int)sampleHz,
            Stereo = false
        };

        if (looping) {
            stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            stream.LoopBegin = 0;
            stream.LoopEnd = (int)(duration * sampleHz);
        }

        return stream;
    }

    private static byte[] GenerateSineWaveWithDecay(double sampleHz, double frequency, double duration, double amplitude) {
        var dataLength = (int)(duration * sampleHz);
        var data = new byte[dataLength * 2];

        for (var i = 0; i < dataLength; i++) {
            var value = Math.Sin(2.0 * Math.PI * frequency * i / sampleHz);
            var envelope = Math.Exp(-5.0 * i / dataLength);
            var sample = (short)(value * envelope * 32767 * amplitude);
            
            data[i * 2] = (byte)(sample & 0xFF);
            data[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return data;
    }

    private static byte[] GenerateContinuousSineWave(double sampleHz, double frequency, double duration, double amplitude) {
        var dataLength = (int)(duration * sampleHz);
        var data = new byte[dataLength * 2];

        for (var i = 0; i < dataLength; i++) {
            var value = Math.Sin(2.0 * Math.PI * frequency * i / sampleHz);
            var sample = (short)(value * 32767 * amplitude);
            
            data[i * 2] = (byte)(sample & 0xFF);
            data[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return data;
    }
}
