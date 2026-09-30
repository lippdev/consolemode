using System.Text;

namespace ConsoleMode.Services.Tv;

/// <summary>Accumulates bounded output from a remote ADB shell command.</summary>
public sealed class AdbShellOutput
{
    private readonly StringBuilder _text = new();
    private int _bytes;

    public void Append(byte[] data)
    {
        if (data.Length > AdbProtocol.MaxShellOutput - _bytes)
            throw new InvalidDataException("ADB: saÃ­da do comando excede o limite de 1 MiB");

        _bytes += data.Length;
        _text.Append(Encoding.UTF8.GetString(data));
    }

    public override string ToString() => _text.ToString();
}
