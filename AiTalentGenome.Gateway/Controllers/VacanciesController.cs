using AiTalentGenome.Contracts.Vacancies;
using Microsoft.AspNetCore.Mvc;

namespace AiTalentGenome.Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VacanciesController(VacancyService.VacancyServiceClient vacancyClient) : ControllerBase
{
    [HttpPost("sync")]
    public async Task<IActionResult> Sync()
    {
        // Извлекаем токен из куки, которую установил AuthController
        if (!Request.Cookies.TryGetValue("hh_access_token", out var token))
        {
            return Unauthorized(new { error = "Сессия отсутствует. Пожалуйста, войдите через HH." });
        }

        try
        {
            var response = await vacancyClient.SyncVacanciesWithHhAsync(new SyncVacanciesRequest 
            { 
                AccessToken = token 
            });

            return Ok(response);
        }
        catch (Grpc.Core.RpcException ex)
        {
            return StatusCode(500, new { error = "Ошибка при синхронизации", details = ex.Status.Detail });
        }
    }
}