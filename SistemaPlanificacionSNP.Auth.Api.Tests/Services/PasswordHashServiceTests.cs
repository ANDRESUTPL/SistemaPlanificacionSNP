using FluentAssertions;
using SistemaPlanificacionSNP.Infrastructure.Services;

namespace SistemaPlanificacionSNP.Auth.Api.Tests.Services;

public class PasswordHashServiceTests
{
    private readonly PasswordHashService _service = new();

    [Fact]
    public void HashPassword_ShouldReturnHashDifferentFromInput()
    {
        var password = "MyS3cureP@ss!";

        var hash = _service.HashPassword(password);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);
    }

    [Fact]
    public void HashPassword_SameInputTwice_ShouldGenerateDifferentHashes()
    {
        var password = "MyS3cureP@ss!";

        var hash1 = _service.HashPassword(password);
        var hash2 = _service.HashPassword(password);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_WithMatchingPassword_ShouldReturnTrue()
    {
        var password = "MyS3cureP@ss!";
        var hash = _service.HashPassword(password);

        var result = _service.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithDifferentPassword_ShouldReturnFalse()
    {
        var password = "MyS3cureP@ss!";
        var hash = _service.HashPassword(password);

        var result = _service.VerifyPassword("WrongP@ss!", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_WithEmptyPassword_ShouldThrowArgumentException()
    {
        var action = () => _service.HashPassword(" ");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VerifyPassword_WithEmptyHash_ShouldThrowArgumentException()
    {
        var action = () => _service.VerifyPassword("valid", " ");

        action.Should().Throw<ArgumentException>();
    }
}
