namespace Stego.Cli.Tests;

public sealed class HideRevealTests : IDisposable
{
    private readonly CliFixture _cli = new();

    public void Dispose() => _cli.Dispose();

    [Fact]
    public void Hide_then_reveal_text_round_trips()
    {
        string cover = _cli.CreatePng();
        string stego = _cli.PathOf("stego.png");

        CliResult hide = _cli.Run("hide", "-i", cover, "-o", stego, "-m", "Hola desde la CLI ✓");
        CliResult reveal = _cli.Run("reveal", "-i", stego);

        Assert.Equal(StegoCli.Success, hide.ExitCode);
        Assert.Equal(StegoCli.Success, reveal.ExitCode);
        Assert.Equal("Hola desde la CLI ✓", reveal.Output.TrimEnd());
        Assert.Equal(0, _cli.Passwords.Calls);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("4")]
    public void Hide_respects_bits_option(string bits)
    {
        string cover = _cli.CreatePng();
        string stego = _cli.PathOf("stego.png");

        _cli.Run("hide", "-i", cover, "-o", stego, "-m", "abc", "-b", bits);
        CliResult info = _cli.Run("info", "-i", stego);

        Assert.Contains($"a {bits} bit/canal", info.Output);
    }

    [Fact]
    public void Encrypted_round_trip_asks_for_password_with_confirmation_only_when_hiding()
    {
        string cover = _cli.CreatePng();
        string stego = _cli.PathOf("stego.png");

        _cli.Run("hide", "-i", cover, "-o", stego, "-m", "secreto", "--encrypt");
        Assert.True(_cli.Passwords.LastConfirm);

        CliResult reveal = _cli.Run("reveal", "-i", stego);

        Assert.Equal(StegoCli.Success, reveal.ExitCode);
        Assert.Equal("secreto", reveal.Output.TrimEnd());
        Assert.False(_cli.Passwords.LastConfirm);
        Assert.Equal(2, _cli.Passwords.Calls);
    }

    [Fact]
    public void Reveal_with_wrong_password_fails()
    {
        string cover = _cli.CreatePng();
        string stego = _cli.PathOf("stego.png");
        _cli.Run("hide", "-i", cover, "-o", stego, "-m", "secreto", "-e");

        _cli.Passwords.Enqueue(Guid.NewGuid().ToString("N"));
        CliResult reveal = _cli.Run("reveal", "-i", stego);

        Assert.Equal(StegoCli.Failure, reveal.ExitCode);
        Assert.Contains("Contraseña incorrecta", reveal.Error);
    }

    [Fact]
    public void Hide_file_and_reveal_to_file_preserves_bytes()
    {
        string cover = _cli.CreatePng(width: 128, height: 128);
        string stego = _cli.PathOf("stego.png");
        string secret = _cli.PathOf("secret.bin");
        string extracted = _cli.PathOf("extracted.bin");
        byte[] bytes = new byte[2_000];
        new Random(5).NextBytes(bytes);
        File.WriteAllBytes(secret, bytes);

        CliResult hide = _cli.Run("hide", "-i", cover, "-o", stego, "-f", secret);
        CliResult reveal = _cli.Run("reveal", "-i", stego, "-o", extracted);

        Assert.Equal(StegoCli.Success, hide.ExitCode);
        Assert.Equal(StegoCli.Success, reveal.ExitCode);
        Assert.Equal(bytes, File.ReadAllBytes(extracted));
    }

    [Fact]
    public void Reveal_binary_payload_without_output_suggests_output_option()
    {
        string cover = _cli.CreatePng();
        string stego = _cli.PathOf("stego.png");
        string secret = _cli.PathOf("secret.bin");
        File.WriteAllBytes(secret, [0xFF, 0xFE, 0x00, 0xC3]);
        _cli.Run("hide", "-i", cover, "-o", stego, "-f", secret);

        CliResult reveal = _cli.Run("reveal", "-i", stego);

        Assert.Equal(StegoCli.Failure, reveal.ExitCode);
        Assert.Contains("--output", reveal.Error);
    }

    [Fact]
    public void Hide_uses_default_output_name()
    {
        string cover = _cli.CreatePng("foto.png");

        CliResult hide = _cli.Run("hide", "-i", cover, "-m", "x");

        Assert.Equal(StegoCli.Success, hide.ExitCode);
        Assert.True(File.Exists(_cli.PathOf("foto.stego.png")));
    }

    [Fact]
    public void Hide_refuses_to_overwrite_without_force()
    {
        string cover = _cli.CreatePng();
        string stego = _cli.PathOf("stego.png");
        File.WriteAllText(stego, "no tocar");

        CliResult refused = _cli.Run("hide", "-i", cover, "-o", stego, "-m", "x");
        Assert.Equal(StegoCli.Failure, refused.ExitCode);
        Assert.Contains("--force", refused.Error);
        Assert.Equal("no tocar", File.ReadAllText(stego));

        CliResult forced = _cli.Run("hide", "-i", cover, "-o", stego, "-m", "x", "--force");
        Assert.Equal(StegoCli.Success, forced.ExitCode);
    }

    [Fact]
    public void Message_too_large_fails_without_creating_output()
    {
        string cover = _cli.CreatePng(width: 8, height: 8);
        string stego = _cli.PathOf("stego.png");

        CliResult hide = _cli.Run("hide", "-i", cover, "-o", stego, "-m", new string('x', 500));

        Assert.Equal(StegoCli.Failure, hide.ExitCode);
        Assert.Contains("capacidad", hide.Error);
        Assert.False(File.Exists(stego));
    }

    [Fact]
    public void Reveal_on_clean_image_fails()
    {
        string cover = _cli.CreatePng();

        CliResult reveal = _cli.Run("reveal", "-i", cover);

        Assert.Equal(StegoCli.Failure, reveal.ExitCode);
        Assert.Contains("firma", reveal.Error);
    }
}
