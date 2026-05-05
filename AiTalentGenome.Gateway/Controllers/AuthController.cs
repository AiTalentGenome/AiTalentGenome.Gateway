// AiTalentGenome.Gateway/Controllers/AuthController.cs
using AiTalentGenome.Contracts.Identity;
using AiTalentGenome.Gateway.DTOs.Requests;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace AiTalentGenome.Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IdentityService.IdentityServiceClient identityClient) : ControllerBase
{
    private readonly IdentityService.IdentityServiceClient _identityClient = identityClient;

    [HttpPost("exchange")]
    public async Task<IActionResult> Exchange([FromBody] ExchangeRequest request)
    {
        try
        {
            var response = await _identityClient.ExchangeHhCodeAsync(new ExchangeHhCodeRequest { Code = request.Code });
            
            if (response.IsActive && !string.IsNullOrEmpty(response.AccessToken))
            {
                SetAuthCookie(response.AccessToken);
            }
            
            return Ok(response);
        }
        catch (RpcException ex)
        {
            // Если код уже использован, прилетит StatusCode.InvalidArgument
            if (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
            {
                return BadRequest(new { error = ex.Status.Detail });
            }

            return StatusCode(500, new { error = "Ошибка сервиса идентификации" });
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("hh_access_token");
        return Ok(new { message = "Сессия завершена" });
    }
    
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        if (!Request.Cookies.TryGetValue("hh_access_token", out var token))
        {
            return Unauthorized(new { error = "Сессия отсутствует" });
        }

        try
        {
            var user = await _identityClient.GetUserInfoAsync(new GetUserInfoRequest 
            { 
                AccessToken = token 
            });

            return Ok(user);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unauthenticated)
        {
            return Unauthorized(new { error = "Токен недействителен" });
        }
    }

    private void SetAuthCookie(string accessToken)
    {
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            // ИСПРАВЛЕНИЕ: на локохосте Secure должен быть false
            Secure = !isDevelopment, 
            // ИСПРАВЛЕНИЕ: для разработки лучше использовать Lax, 
            // чтобы кука стабильнее передавалась между портами 8000 и 5000
            SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7),
            Path = "/"
        };

        Response.Cookies.Append("hh_access_token", accessToken, cookieOptions);
    }
}