using System;
using System.IO;

namespace SmartDocQA.Web;

public static class DotEnv
{
    public static void Load()
    {
        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        string? envPath = null;

        // Traverse up to find .env file
        while (directory != null)
        {
            var testPath = Path.Combine(directory.FullName, ".env");
            if (File.Exists(testPath))
            {
                envPath = testPath;
                break;
            }
            directory = directory.Parent;
        }

        if (envPath == null)
        {
            // Also try current directory
            var currentTest = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(currentTest))
            {
                envPath = currentTest;
            }
        }

        if (envPath == null)
        {
            Console.WriteLine(".env file not found.");
            return;
        }

        Console.WriteLine($"Loading environment variables from: {envPath}");

        foreach (var line in File.ReadAllLines(envPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var val = parts[1].Trim();

            // Strip quotes if any
            if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
            {
                val = val[1..^1];
            }

            Environment.SetEnvironmentVariable(key, val);
        }
    }
}
