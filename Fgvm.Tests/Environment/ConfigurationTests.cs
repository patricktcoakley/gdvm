using Fgvm.Environment;
using Fgvm.Error;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Fgvm.Tests.Environment;

public class ConfigurationTests
{
    private static string GenerateTestToken(string prefix = "ghp")
    {
        var remainingLength = 40 - prefix.Length - 1; // -1 for the underscore
        var hexString = RandomNumberGenerator.GetHexString(remainingLength, true);
        return $"{prefix}_{hexString}";
    }

    private static string GenerateInvalidToken(string prefix, char invalidChar, int position = 20)
    {
        var validToken = GenerateTestToken(prefix);
        var chars = validToken.ToCharArray();
        chars[4 + position] = invalidChar; // Skip the "ghp_" prefix
        return new string(chars);
    }

    [Fact]
    public void ValidateConfiguration_ValidToken_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["github:token"] = GenerateTestToken()
            })
            .Build();

        var exception = Record.Exception(() => Configuration.ValidateConfiguration(config));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateConfiguration_NoToken_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var exception = Record.Exception(() => Configuration.ValidateConfiguration(config));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateConfiguration_EmptyToken_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["github:token"] = ""
            })
            .Build();

        var exception = Record.Exception(() => Configuration.ValidateConfiguration(config));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("xyz")]
    [InlineData("token")]
    public void ValidateConfiguration_InvalidPrefix_ThrowsConfigurationException(string prefix)
    {
        var token = GenerateTestToken(prefix);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["github:token"] = token
            })
            .Build();

        var ex = Assert.Throws<ConfigurationException>(() => Configuration.ValidateConfiguration(config));
        Assert.Equal("GitHub token should start with 'ghp_', 'gho_', 'ghu_', 'ghs_', or 'ghr_' prefix", ex.Message);
    }

    [Theory]
    [InlineData("ghp_short")]
    [InlineData("ghp_")]
    public void ValidateConfiguration_InvalidLength_ThrowsConfigurationException(string token)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["github:token"] = token
            })
            .Build();

        var ex = Assert.Throws<ConfigurationException>(() => Configuration.ValidateConfiguration(config));
        Assert.Equal("GitHub token should be exactly 40 characters long", ex.Message);
    }

    [Theory]
    [InlineData('@')]
    [InlineData('#')]
    [InlineData(' ')]
    [InlineData('-')]
    public void ValidateConfiguration_InvalidCharacters_ThrowsConfigurationException(char invalidChar)
    {
        var token = GenerateInvalidToken("ghp", invalidChar);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["github:token"] = token
            })
            .Build();

        var ex = Assert.Throws<ConfigurationException>(() => Configuration.ValidateConfiguration(config));
        Assert.Equal("GitHub token contains invalid characters", ex.Message);
    }

    [Theory]
    [InlineData("ghp")]
    [InlineData("gho")]
    [InlineData("ghu")]
    [InlineData("ghs")]
    [InlineData("ghr")]
    public void ValidateConfiguration_ValidTokens_DoesNotThrow(string prefix)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["github:token"] = GenerateTestToken(prefix)
            })
            .Build();

        var exception = Record.Exception(() => Configuration.ValidateConfiguration(config));
        Assert.Null(exception);
    }
}
