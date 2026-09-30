using System.Text;

namespace ConsoleMode.Services.Tv;

/// <summary>Reads process output to completion while retaining only a bounded prefix.</summary>
public static class CecOutputCapture
{
    public const int MaxCharacters = 32 * 1024;

    public static async Task<string> ReadBoundedAsync(TextReader reader, int maxCharacters, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentOutOfRangeException.ThrowIfNegative(maxCharacters);

        var output = new StringBuilder(Math.Min(maxCharacters, 4096));
        var buffer = new char[4096];
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), ct);
            if (read == 0) return output.ToString();

            var remaining = maxCharacters - output.Length;
            if (remaining > 0)
                output.Append(buffer, 0, Math.Min(read, remaining));
        }
    }
}
