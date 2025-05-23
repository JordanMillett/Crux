#r "System.Diagnostics.Process"
#r "System.IO.Compression.FileSystem"
#r "System"

using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Diagnostics;

string docsPath = "CruxDocs/docs";
string tocPath = Path.Combine(docsPath, "toc.yml");

Console.WriteLine("Populating toc.yml from CruxDocs/docs .md files");

System.Collections.Generic.List<string> allFiles = Directory
    .EnumerateFiles(docsPath, "*.md", SearchOption.TopDirectoryOnly)
    .Select(Path.GetFileName)
    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
    .ToList();

using (StreamWriter writer = new StreamWriter(tocPath, false))
{
    foreach (string file in allFiles)
    {
        string filename = file;
        string nameWithoutExt = Path.GetFileNameWithoutExtension(filename);

        string title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(nameWithoutExt.Replace("-", " "));

        writer.WriteLine($"- name: {title}");
        writer.WriteLine($"  href: {filename}");
    }
}

Console.WriteLine("Building docfx metadata");
RunCommand("docfx", "metadata CruxDocs/docfx.json");

Console.WriteLine("Serving docfx");
RunCommand("docfx", "CruxDocs/docfx.json --serve");

void RunCommand(string fileName, string args)
{
    ProcessStartInfo startInfo = new ProcessStartInfo
    {
        FileName = fileName,
        Arguments = args,

        UseShellExecute = false,
        CreateNoWindow = false,
        RedirectStandardOutput = false,
        RedirectStandardError = false
    };

    using Process process = Process.Start(startInfo);
    process?.WaitForExit();
}
