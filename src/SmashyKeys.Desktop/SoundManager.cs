using System.IO;
using System.Media;

namespace SmashyKeys;

/// <summary>
/// Generates and plays simple synthesized sounds.
/// Creates sine wave beeps at various frequencies.
/// </summary>
public class SoundManager
{
    private readonly List<MemoryStream> _sounds = new();
    private readonly List<SoundPlayer> _players = new();
    private readonly Random _random = new();

    // Pleasant frequencies for toddler-friendly sounds (musical notes)
    private static readonly int[] Frequencies =
    {
        262,  // C4
        294,  // D4
        330,  // E4
        349,  // F4
        392,  // G4
        440,  // A4
        494,  // B4
        523,  // C5
        587,  // D5
        659,  // E5
    };

    public SoundManager()
    {
        // Pre-generate all sounds for instant playback
        foreach (var freq in Frequencies)
        {
            var wavData = GenerateWav(freq, 150); // 150ms duration
            var stream = new MemoryStream(wavData);
            _sounds.Add(stream);

            var player = new SoundPlayer(stream);
            player.Load();
            _players.Add(player);
        }

        Logger.Log($"SoundManager initialized with {_players.Count} sounds");
    }

    public void PlayRandomSound()
    {
        try
        {
            var index = _random.Next(_players.Count);
            _sounds[index].Position = 0; // Reset stream position
            _players[index].Play();
        }
        catch (Exception ex)
        {
            Logger.LogException("PlayRandomSound", ex);
        }
    }

    /// <summary>
    /// Generates a WAV file byte array containing a sine wave.
    /// </summary>
    private static byte[] GenerateWav(int frequency, int durationMs)
    {
        const int sampleRate = 44100;
        const int bitsPerSample = 16;
        const int channels = 1;

        int numSamples = (int)(sampleRate * durationMs / 1000.0);
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int dataSize = numSamples * blockAlign;

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // RIFF header
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize); // File size - 8
        writer.Write("WAVE"u8);

        // fmt chunk
        writer.Write("fmt "u8);
        writer.Write(16); // Chunk size
        writer.Write((short)1); // Audio format (PCM)
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);

        // data chunk
        writer.Write("data"u8);
        writer.Write(dataSize);

        // Generate sine wave with envelope (fade in/out)
        double amplitude = 0.3 * short.MaxValue; // 30% volume
        int fadeLength = numSamples / 10; // 10% fade

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            double sample = Math.Sin(2 * Math.PI * frequency * t);

            // Apply envelope
            double envelope = 1.0;
            if (i < fadeLength)
                envelope = (double)i / fadeLength;
            else if (i > numSamples - fadeLength)
                envelope = (double)(numSamples - i) / fadeLength;

            short value = (short)(sample * amplitude * envelope);
            writer.Write(value);
        }

        return ms.ToArray();
    }
}
