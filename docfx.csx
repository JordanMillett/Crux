#r "System.Diagnostics.Process"
#r "System.IO.Compression.FileSystem"
#r "System"

using System.Globalization;

var docsPath = "CruxDocs/docs";
var tocPath = Path.Combine(docsPath, "toc.yml");

Console.WriteLine("Populating toc.yml from CruxDocs/docs .md files");

var allFiles = Directory
    .EnumerateFiles(docsPath, "*.md", SearchOption.TopDirectoryOnly)
    .Select(Path.GetFileName)
    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
    .ToList();

using (var writer = new StreamWriter(tocPath, false))
{
    foreach (var file in allFiles)
    {
        var filename = file;
        var nameWithoutExt = Path.GetFileNameWithoutExtension(filename);

        var title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(nameWithoutExt.Replace("-", " "));

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
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    process.OutputDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
    process.ErrorDataReceived += (sender, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();
}
