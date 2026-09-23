using System.Diagnostics;
using System.Text;

namespace ConsoleMode.Services;

public static class ProcessRunner
{
    public static int Run(string fileName, IEnumerable<string> arguments, int timeoutMs = 20000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };
        process.Start();
        if (!process.WaitForExit(timeoutMs))
        {
            try { process.Kill(true); } catch { /* ignore */ }
            throw new TimeoutException($"{Path.GetFileName(fileName)} expirou: {string.Join(' ', arguments)}");
        }
        return process.ExitCode;
    }

    public static string RunCapture(string fileName, IEnumerable<string> arguments, int timeoutMs = 8000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };
        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(timeoutMs))
        {
            try { process.Kill(true); } catch { /* ignore */ }
            throw new TimeoutException($"{Path.GetFileName(fileName)} expirou: {string.Join(' ', arguments)}");
        }

        stdout = stdout.Trim();
        if (process.ExitCode != 0 && stdout != "OK")
        {
            var msg = !string.IsNullOrWhiteSpace(stderr) ? stderr.Trim() : stdout;
            if (string.IsNullOrWhiteSpace(msg)) msg = $"exit {process.ExitCode}";
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} falhou ({string.Join(' ', arguments)}): {msg}");
        }

        return stdout;
    }

    public static void StartDetached(string fileName, string? arguments = null)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments ?? "",
            UseShellExecute = true
        });
    }
}

public static class CsvReader
{
    public static List<Dictionary<string, string>> Read(string path)
    {
        var rows = new List<Dictionary<string, string>>();
        if (!File.Exists(path)) return rows;

        using var reader = new StreamReader(path, Encoding.UTF8);
        var headerLine = reader.ReadLine();
        if (headerLine is null) return rows;
        var headers = Split(headerLine);
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = Split(line);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                row[headers[i]] = i < cols.Count ? cols[i] : "";
            }
            rows.Add(row);
        }
        return rows;
    }

    public static string Get(this Dictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var v) ? v : "";

    private static List<string> Split(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }
}
