using System.CommandLine;

namespace Stego.Cli.Commands;

internal static class CommonOptions
{
    public static Option<FileInfo> Input(string description) =>
        new Option<FileInfo>("--input", "-i")
        {
            Description = description,
            Required = true,
        }.AcceptExistingOnly();
}
