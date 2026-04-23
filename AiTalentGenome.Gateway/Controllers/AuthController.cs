// AiTalentGenome.Gateway/Controllers/AuthController.cs
using AiTalentGenome.Contracts.Identity;
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
            // 1. Вызов микросервиса Identity по gRPC
            var response = await _identityClient.ExchangeHhCodeAsync(new ExchangeHhCodeRequest 
            { 
                Code = request.Code 
            });

            if (!response.IsActive)
            {
                return StatusCode(403, new { error = response.ErrorMessage });
            }

            // 2. Установка защищенной куки
            SetAuthCookie(response.AccessToken);

            return Ok(new 
            { 
                message = "Авторизация прошла успешно", 
                user = response.User 
            });
        }
        catch (Exception ex)
        {
            // Здесь можно добавить логгирование
            return BadRequest(new { error = "Ошибка при обмене кода: " + ex.Message });
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
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,    // Защита от XSS
            Secure = true,      // Только через HTTPS
            SameSite = SameSiteMode.Strict, // Защита от CSRF
            Expires = DateTime.UtcNow.AddDays(7),
            Path = "/"
        };

        Response.Cookies.Append("hh_access_token", accessToken, cookieOptions);
    }
}

public record ExchangeRequest(string Code);