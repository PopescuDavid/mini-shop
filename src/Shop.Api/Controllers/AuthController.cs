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
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            return Unauthorized();

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            return Unauthorized();

        var token = tokenService.Create(user);
        return Ok(new LoginResponse(token.Token, token.ExpiresAt));
    }
}
