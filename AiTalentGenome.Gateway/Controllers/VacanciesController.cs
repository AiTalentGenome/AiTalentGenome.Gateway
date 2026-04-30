using AiTalentGenome.Contracts.Vacancies;
using Grpc.Core;
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
    
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] bool onlyActive = true)
    {
        try
        {
            var response = await vacancyClient.GetVacanciesAsync(new GetVacanciesRequest 
            { 
                OnlyActive = onlyActive 
            });
    
            return Ok(response.Vacancies);
        }
        catch (RpcException ex)
        {
            return StatusCode(500, new { error = "Ошибка получения списка вакансий", details = ex.Status.Detail });
        }
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var response = await vacancyClient.GetVacancyByIdAsync(new GetVacancyByIdRequest { Id = id });
            return Ok(response);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return NotFound(new { error = ex.Status.Detail });
        }
        catch (RpcException ex)
        {
            return StatusCode(500, new { error = "Ошибка при получении вакансии", details = ex.Status.Detail });
        }
    }
}