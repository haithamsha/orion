using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Orion.Api.Controllers;

namespace Orion.Api.Tests.Unit;

public class AuthControllerTests
{
    [Fact]
    public void Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // ARRANGE
        // 1. Mock the IConfiguration dependency
        var mockConfig = new Mock<IConfiguration>();
        var mockKeySection = new Mock<IConfigurationSection>();
        var mockIssuerSection = new Mock<IConfigurationSection>();
        var mockAudienceSection = new Mock<IConfigurationSection>();
        
        // Setup the mock to return our fake JWT values when asked for them.
        mockKeySection.Setup(x => x.Value).Returns("ThisIsMySuperSecretKeyForOrionApi12345!");
        mockIssuerSection.Setup(x => x.Value).Returns("OrionApi");
        mockAudienceSection.Setup(x => x.Value).Returns("OrionApiUsers");
        
        mockConfig.Setup(x => x["Jwt:Key"]).Returns("ThisIsMySuperSecretKeyForOrionApi12345!");
        mockConfig.Setup(x => x["Jwt:Issuer"]).Returns("OrionApi");
        mockConfig.Setup(x => x["Jwt:Audience"]).Returns("OrionApiUsers");
        mockConfig.Setup(x => x.GetSection("Jwt:Key")).Returns(mockKeySection.Object);
        mockConfig.Setup(x => x.GetSection("Jwt:Issuer")).Returns(mockIssuerSection.Object);
        mockConfig.Setup(x => x.GetSection("Jwt:Audience")).Returns(mockAudienceSection.Object);

        // 2. Create an instance of the controller with the mocked dependency
        var controller = new AuthController(mockConfig.Object);
        var loginRequest = new LoginRequest("testuser", "password123");

        // ACT
        // 3. Call the method we are testing
        var result = controller.Login(loginRequest);

        // ASSERT
        // 4. Verify the outcome
        var okResult = Assert.IsType<OkObjectResult>(result); // It should be an OK response
        var returnValue = okResult.Value;
        Assert.NotNull(returnValue);

        // Use reflection to get the 'token' property from the anonymous return object
        var tokenProperty = returnValue.GetType().GetProperty("token");
        Assert.NotNull(tokenProperty);
        
        var tokenValue = tokenProperty.GetValue(returnValue, null) as string;
        Assert.False(string.IsNullOrEmpty(tokenValue)); // The token should not be empty
    }

    [Fact]
    public void Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // ARRANGE
        var mockConfig = new Mock<IConfiguration>(); // No setup needed as it won't be used
        var controller = new AuthController(mockConfig.Object);
        var loginRequest = new LoginRequest("wronguser", "wrongpassword");

        // ACT
        var result = controller.Login(loginRequest);

        // ASSERT
        Assert.IsType<UnauthorizedObjectResult>(result); // It should be an Unauthorized response
    }
}