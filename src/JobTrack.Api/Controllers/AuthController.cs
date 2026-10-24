using JobTrack.Api.Data;
using JobTrack.Api.Dtos;
using JobTrack.Api.Models;
using JobTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordService _passwords;
    private readonly TokenService _tokens;

    public AuthController(AppDbContext db, PasswordService passwords, TokenService tokens)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
    }

    /// <summary>Create an account and get a token back.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new ProblemDetails { Title = "This email is already registered.", Status = 409 });
        }

        var user = new User
        {
            Email = email,
            PasswordHash = _passwords.Hash(request.Password)
        };
        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two people registering the same email at the same moment: the database's unique index catches it.
            return Conflict(new ProblemDetails { Title = "This email is already registered.", Status = 409 });
        }

        var (token, expiresAt) = _tokens.CreateToken(user);
        return StatusCode(StatusCodes.Status201Created, new AuthResponse(token, expiresAt, user.Email));
    }

    /// <summary>Log in with email + password and get a token back.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such user" and "wrong password" so attackers can't tell which emails exist.
        if (user is null || !_passwords.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password.", Status = 401 });
        }

        var (token, expiresAt) = _tokens.CreateToken(user);
        return Ok(new AuthResponse(token, expiresAt, user.Email));
    }

    /// <summary>Who am I? Needs a valid token. Handy for checking that login works.</summary>
    [Authorize]
    [HttpGet("me")]
    public ActionResult<MeResponse> Me()
    {
        var id = int.Parse(User.FindFirst("uid")!.Value);
        var email = User.FindFirst("email")!.Value;
        return Ok(new MeResponse(id, email));
    }
}
