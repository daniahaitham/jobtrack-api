using JobTrack.Api.Services;

namespace JobTrack.Tests;

public class PasswordServiceTests
{
    private readonly PasswordService _service = new();

    [Fact]
    public void Correct_password_verifies()
    {
        var hash = _service.Hash("my-secret-pass");
        Assert.True(_service.Verify("my-secret-pass", hash));
    }

    [Fact]
    public void Wrong_password_is_rejected()
    {
        var hash = _service.Hash("my-secret-pass");
        Assert.False(_service.Verify("another-pass", hash));
    }

    [Fact]
    public void Same_password_gives_different_hashes_because_of_the_salt()
    {
        Assert.NotEqual(_service.Hash("same-pass-123"), _service.Hash("same-pass-123"));
    }

    [Fact]
    public void Hash_never_contains_the_plain_password()
    {
        Assert.DoesNotContain("my-secret-pass", _service.Hash("my-secret-pass"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("1.2")]
    [InlineData("abc.def.ghi")]
    public void Garbage_stored_hash_returns_false_instead_of_crashing(string garbage)
    {
        Assert.False(_service.Verify("anything", garbage));
    }
}
