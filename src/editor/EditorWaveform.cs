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
    /// <summary>
    /// Analyzes an audio stream and extracts waveform data for both stereo channels
    /// </summary>
    public static WaveformData? AnalyzeAudioStream(AudioStream? audioStream, int targetHeight) {
        if (audioStream == null) return null;

        // Only support AudioStreamWav for now
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

        // Calculate number of samples based on format
        int sampleCount = format switch {
            AudioStreamWav.FormatEnum.Format8Bits => data.Length,
            AudioStreamWav.FormatEnum.Format16Bits => data.Length / 2,
            AudioStreamWav.FormatEnum.ImaAdpcm => data.Length * 2,
            _ => data.Length / 4 // Float format
        };

        // Adjust for stereo
        var totalSamples = isStereo ? sampleCount / 2 : sampleCount;
        var songLength = (double)totalSamples / mixRate;
        
        // Calculate samples per pixel to fit the waveform to the display height
        var samplesPerPixel = Math.Max(1, totalSamples / (targetHeight * 100));
        
        var waveformSamples = totalSamples / samplesPerPixel;
        var leftChannel = new float[waveformSamples];
        var rightChannel = new float[waveformSamples];

        // Extract and downsample waveform data
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
                } else {
                    if (sampleIndex >= data.Length) return (0f, 0f);
                    var value = (data[sampleIndex] - 128) / 128f;
                    return (value, value);
                }
            }

            case AudioStreamWav.FormatEnum.Format16Bits: {
                if (isStereo) {
                    var leftIdx = sampleIndex * 4;
                    var rightIdx = sampleIndex * 4 + 2;
                    if (leftIdx + 1 >= data.Length || rightIdx + 1 >= data.Length) return (0f, 0f);
                    var left = BitConverter.ToInt16(data, leftIdx) / 32768f;
                    var right = BitConverter.ToInt16(data, rightIdx) / 32768f;
                    return (left, right);
                } else {
                    var idx = sampleIndex * 2;
                    if (idx + 1 >= data.Length) return (0f, 0f);
                    var value = BitConverter.ToInt16(data, idx) / 32768f;
                    return (value, value);
                }
            }

            default:
                return (0f, 0f);
        }
    }

    /// <summary>
    /// Draws the stereo waveform on the right side of the editor, aligned with the grid
    /// </summary>
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

        // Calculate waveform position (right side of the screen)
        var waveformX = viewportWidth - EditorConstants.WaveformWidth - EditorConstants.WaveformMargin;
        
        // Draw background
        var bgRect = new Rect2(
            waveformX, 
            0, 
            EditorConstants.WaveformWidth, 
            viewportHeight
        );
        canvas.DrawRect(bgRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
        
        // Draw border
        canvas.DrawRect(bgRect, new Color(0.3f, 0.3f, 0.3f), false, 1);

        // Calculate the vertical scale based on zoom and pan
        // The waveform should align with the grid vertically
        var beatsPerSecond = chart.Bpm / 60.0;
        
        // Draw both channels
        var leftChannelX = waveformX + EditorConstants.WaveformPadding;
        var channelWidth = (EditorConstants.WaveformWidth - EditorConstants.WaveformPadding * 3) / 2;
        var rightChannelX = leftChannelX + channelWidth + EditorConstants.WaveformPadding;
        
        DrawChannel(canvas, waveformData.LeftChannel, leftChannelX, channelWidth, zoom, panY, beatsPerSecond, waveformData.SongLength, new Color(0.3f, 0.8f, 1f, 0.8f));
        DrawChannel(canvas, waveformData.RightChannel, rightChannelX, channelWidth, zoom, panY, beatsPerSecond, waveformData.SongLength, new Color(1f, 0.5f, 0.3f, 0.8f));
        
        // Draw labels
        var labelLeft = new Vector2(leftChannelX + channelWidth / 2 - 10, 10);
        var labelRight = new Vector2(rightChannelX + channelWidth / 2 - 10, 10);
        canvas.DrawString(ThemeDB.FallbackFont, labelLeft, "L", HorizontalAlignment.Center, -1, 12, Colors.White);
        canvas.DrawString(ThemeDB.FallbackFont, labelRight, "R", HorizontalAlignment.Center, -1, 12, Colors.White);
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
        
        // Calculate step size based on zoom - at higher zoom, draw more samples
        // At low zoom (50), skip every 10 samples
        // At medium zoom (100), skip every 5 samples
        // At high zoom (200+), draw every sample
        var skipFactor = Math.Max(1, (int)(500f / zoom));
        
        // Draw waveform bars aligned to the timeline
        for (var i = 0; i < channelData.Length; i += skipFactor) {
            var amplitude = channelData[i];
            if (amplitude <= 0.001f) continue; // Skip silent samples for performance

            // Calculate time position for this sample
            var timeInSeconds = (i / (double)channelData.Length) * songLength;
            var beat = timeInSeconds * beatsPerSecond;
            
            // Convert to screen Y position (aligned with the grid)
            var yPosition = -(float)beat * zoom + panY;
            
            // Only draw if visible on screen
            if (yPosition < -10 || yPosition > viewportHeight + 10) continue;

            // Draw amplitude bar
            var barWidth = amplitude * channelWidth * 0.95f;
            var centerX = channelX + channelWidth / 2;
            var barLeft = centerX - barWidth / 2;
            
            // At high zoom levels, make bars thicker so they're more visible and fill gaps
            var lineThickness = Math.Max(1.5f, Math.Min(5f, zoom / 40f));
            
            canvas.DrawLine(
                new Vector2(barLeft, yPosition),
                new Vector2(barLeft + barWidth, yPosition),
                color,
                lineThickness
            );
        }
    }
}

