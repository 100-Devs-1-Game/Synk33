using System;
using Godot;

namespace SYNK33.editor;

public class WaveformData {
    public float[] LeftChannel { get; set; } = [];
    public float[] RightChannel { get; set; } = [];
    public int SamplesPerPixel { get; set; }
    public double SongLength { get; set; }
}

public static class EditorWaveform {
    public static WaveformData? AnalyzeAudioStream(AudioStream? audioStream, int targetHeight) {
        if (audioStream == null) return null;

        if (audioStream is not AudioStreamWav wavStream) {
            GD.PrintErr("Waveform analysis only supports AudioStreamWav");
            return null;
        }

        var data = wavStream.Data;
        if (data == null || data.Length == 0) {
            GD.PrintErr("Audio stream has no data");
            return null;
        }

        var format = wavStream.Format;
        var isStereo = wavStream.Stereo;
        var mixRate = wavStream.MixRate;
        
        GD.Print($"Analyzing waveform: Format={format}, Stereo={isStereo}, MixRate={mixRate}, DataLength={data.Length}");

        int sampleCount = format switch {
            AudioStreamWav.FormatEnum.Format8Bits => data.Length,
            AudioStreamWav.FormatEnum.Format16Bits => data.Length / 2,
            AudioStreamWav.FormatEnum.ImaAdpcm => data.Length * 2,
            _ => data.Length / 4
        };

        var totalSamples = isStereo ? sampleCount / 2 : sampleCount;
        var songLength = (double)totalSamples / mixRate;
        var samplesPerPixel = Math.Max(1, totalSamples / (targetHeight * 100));
        var waveformSamples = totalSamples / samplesPerPixel;
        var leftChannel = new float[waveformSamples];
        var rightChannel = new float[waveformSamples];

        for (var i = 0; i < waveformSamples; i++) {
            float leftMax = 0f, rightMax = 0f;
            var startSample = i * samplesPerPixel;
            var endSample = Math.Min((i + 1) * samplesPerPixel, totalSamples);

            for (var s = startSample; s < endSample; s++) {
                var (left, right) = GetSampleValue(data, s, format, isStereo);
                leftMax = Math.Max(leftMax, Math.Abs(left));
                rightMax = Math.Max(rightMax, Math.Abs(right));
            }

            leftChannel[i] = leftMax;
            rightChannel[i] = isStereo ? rightMax : leftMax;
        }

        GD.Print($"Waveform analyzed: {waveformSamples} samples, {samplesPerPixel} samples/pixel, {songLength:F2}s");

        return new WaveformData {
            LeftChannel = leftChannel,
            RightChannel = rightChannel,
            SamplesPerPixel = samplesPerPixel,
            SongLength = songLength
        };
    }

    private static (float left, float right) GetSampleValue(byte[] data, int sampleIndex, AudioStreamWav.FormatEnum format, bool isStereo) {
        switch (format) {
            case AudioStreamWav.FormatEnum.Format8Bits: {
                if (isStereo) {
                    var leftIdx = sampleIndex * 2;
                    var rightIdx = sampleIndex * 2 + 1;
                    if (leftIdx >= data.Length || rightIdx >= data.Length) return (0f, 0f);
                    var left = (data[leftIdx] - 128) / 128f;
                    var right = (data[rightIdx] - 128) / 128f;
                    return (left, right);
                }
                if (sampleIndex >= data.Length) return (0f, 0f);
                var value = (data[sampleIndex] - 128) / 128f;
                return (value, value);
            }

            case AudioStreamWav.FormatEnum.Format16Bits: {
                if (isStereo) {
                    var leftIdx = sampleIndex * 4;
                    var rightIdx = sampleIndex * 4 + 2;
                    if (leftIdx + 1 >= data.Length || rightIdx + 1 >= data.Length) return (0f, 0f);
                    var left = BitConverter.ToInt16(data, leftIdx) / 32768f;
                    var right = BitConverter.ToInt16(data, rightIdx) / 32768f;
                    return (left, right);
                }
                var idx = sampleIndex * 2;
                if (idx + 1 >= data.Length) return (0f, 0f);
                var value = BitConverter.ToInt16(data, idx) / 32768f;
                return (value, value);
            }

            default:
                return (0f, 0f);
        }
    }

    public static void DrawWaveform(
        CanvasItem canvas,
        WaveformData? waveformData,
        SYNK33.chart.Chart chart,
        float zoom,
        float panY,
        float viewportWidth,
        float viewportHeight
    ) {
        if (waveformData == null) return;

        var waveformX = viewportWidth - EditorConstants.WaveformWidth - EditorConstants.WaveformMargin;
        
        var bgRect = new Rect2(waveformX, 0, EditorConstants.WaveformWidth, viewportHeight);
        canvas.DrawRect(bgRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
        canvas.DrawRect(bgRect, new Color(0.3f, 0.3f, 0.3f), false, 1);

        const float channelWidth = (EditorConstants.WaveformWidth - EditorConstants.WaveformPadding * 3) / 2;
        var beatsPerSecond = chart.Bpm / 60.0;
        var leftChannelX = waveformX + EditorConstants.WaveformPadding;
        var rightChannelX = leftChannelX + channelWidth + EditorConstants.WaveformPadding;
        
        DrawChannel(canvas, waveformData.LeftChannel, leftChannelX, channelWidth, zoom, panY, beatsPerSecond, waveformData.SongLength, new Color(0.3f, 0.8f, 1f, 0.8f));
        DrawChannel(canvas, waveformData.RightChannel, rightChannelX, channelWidth, zoom, panY, beatsPerSecond, waveformData.SongLength, new Color(1f, 0.5f, 0.3f, 0.8f));
    }

    private static void DrawChannel(
        CanvasItem canvas,
        float[] channelData,
        float channelX,
        float channelWidth,
        float zoom,
        float panY,
        double beatsPerSecond,
        double songLength,
        Color color
    ) {
        if (channelData.Length == 0) return;
        
        var viewportHeight = canvas.GetViewportRect().Size.Y;
        var skipFactor = CalculateSkipFactorForZoom(zoom);
        
        for (var i = 0; i < channelData.Length; i += skipFactor) {
            var amplitude = channelData[i];
            if (amplitude <= 0.001f) continue;

            var timeInSeconds = (i / (double)channelData.Length) * songLength;
            var beat = timeInSeconds * beatsPerSecond;
            var yPosition = -(float)beat * zoom + panY;
            
            if (yPosition < -10 || yPosition > viewportHeight + 10) continue;

            var barWidth = amplitude * channelWidth * 0.95f;
            var centerX = channelX + channelWidth / 2;
            var barLeft = centerX - barWidth / 2;
            var lineThickness = CalculateLineThicknessForZoom(zoom);
            
            canvas.DrawLine(
                new Vector2(barLeft, yPosition),
                new Vector2(barLeft + barWidth, yPosition),
                color,
                lineThickness
            );
        }
    }

    private static int CalculateSkipFactorForZoom(float zoom) 
        => Math.Max(1, (int)(500f / zoom));

    private static float CalculateLineThicknessForZoom(float zoom) 
        => Math.Max(1.5f, Math.Min(5f, zoom / 40f));
}

