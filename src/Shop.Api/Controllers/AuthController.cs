using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Dtos;
using Shop.Api.Entities;
using Shop.Api.Services;

namespace Shop.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(ShopDbContext db, IPasswordHasher<User> passwordHasher, ITokenService tokenService) : ControllerBase
{
    private static readonly User DummyUser = new() { Email = string.Empty, PasswordHash = string.Empty };

    // A real, precomputed hash so verification does the same PBKDF2 work whether or
    // not the user exists — otherwise response timing would leak which emails are
    // registered (user enumeration).
    private static readonly string DummyPasswordHash =
        new PasswordHasher<User>().HashPassword(DummyUser, "timing-attack-mitigation");

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        var verification = passwordHasher.VerifyHashedPassword(
            user ?? DummyUser, user?.PasswordHash ?? DummyPasswordHash, request.Password);

        if (user is null || verification == PasswordVerificationResult.Failed)
            return Unauthorized();

        var token = tokenService.Create(user);
        return Ok(new LoginResponse(token.Token, token.ExpiresAt));
    }
}
