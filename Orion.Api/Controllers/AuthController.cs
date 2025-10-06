using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Orion.Api.Controllers;

public record LoginRequest(string Username, string Password);

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        // --- In a real app, you would validate the user against a database. ---
        // For this example, we'll accept a hardcoded user.
        if (loginRequest.Username != "testuser" || loginRequest.Password != "password123")
        {
            return Unauthorized("Invalid credentials.");
        }

        // --- If credentials are valid, generate a JWT ---
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // The "claims" are pieces of information about the user.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, loginRequest.Username), // "sub" is a standard claim for the subject (user identifier)
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // "jti" is a unique identifier for the token
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(60), // Token is valid for 60 minutes
            signingCredentials: credentials);

        var generatedToken = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new { token = generatedToken });
    }
}