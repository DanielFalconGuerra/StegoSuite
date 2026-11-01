namespace Stego.Cli.Tests;

public sealed class ValidationAndInfoTests : IDisposable
{
    private readonly CliFixture _cli = new();

    public void Dispose() => _cli.Dispose();

    [Fact]
    public void Hide_requires_message_or_file()
    {
        string cover = _cli.CreatePng();

        CliResult result = _cli.Run("hide", "-i", cover);

        Assert.NotEqual(StegoCli.Success, result.ExitCode);
        Assert.Contains("--message o --file", result.Error);
    }

    [Fact]
    public void Hide_rejects_message_and_file_together()
    {
        string cover = _cli.CreatePng();
        string secret = _cli.PathOf("secret.txt");
        File.WriteAllText(secret, "x");

        CliResult result = _cli.Run("hide", "-i", cover, "-m", "x", "-f", secret);

        Assert.NotEqual(StegoCli.Success, result.ExitCode);
        Assert.Contains("--message o --file", result.Error);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("3")]
    [InlineData("8")]
    public void Hide_rejects_unsupported_bits(string bits)
    {
        string cover = _cli.CreatePng();

        CliResult result = _cli.Run("hide", "-i", cover, "-m", "x", "-b", bits);

        Assert.NotEqual(StegoCli.Success, result.ExitCode);
        Assert.Contains("--bits", result.Error);
    }

    [Fact]
    public void Missing_input_file_is_rejected()
    {
        CliResult result = _cli.Run("info", "-i", _cli.PathOf("no-existe.png"));

        Assert.NotEqual(StegoCli.Success, result.ExitCode);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public void Info_on_clean_image_reports_capacity_and_no_message()
    {
        string cover = _cli.CreatePng(width: 100, height: 100);

        CliResult info = _cli.Run("info", "-i", cover);

        Assert.Equal(StegoCli.Success, info.ExitCode);
        Assert.Contains("100x100", info.Output);
        Assert.Contains("1 bit/canal: 3735 bytes", info.Output); // (30000 - 120) / 8
        Assert.Contains("no detectado", info.Output);
    }

    [Fact]
    public void Info_describes_encrypted_message_without_asking_for_password()
    {
        string cover = _cli.CreatePng();
        string stego = _cli.PathOf("stego.png");
        _cli.Run("hide", "-i", cover, "-o", stego, "-m", "hola", "-e", "-b", "2");
        int callsAfterHide = _cli.Passwords.Calls;

        CliResult info = _cli.Run("info", "-i", stego);

        Assert.Contains("Mensaje:   sí", info.Output);
        Assert.Contains("a 2 bit/canal, cifrado", info.Output);
        Assert.Equal(callsAfterHide, _cli.Passwords.Calls);
    }
}
