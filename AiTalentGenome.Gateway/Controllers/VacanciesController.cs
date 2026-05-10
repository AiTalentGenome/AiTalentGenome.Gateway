using AiTalentGenome.Contracts.Vacancies;
using AiTalentGenome.Gateway.DTOs.Requests;
using AiTalentGenome.Gateway.DTOs.Responses;
using Google.Protobuf;
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
    
    [HttpPost("{id}/sync-applications")]
    public async Task<IActionResult> SyncApplications(string id)
    {
        // Извлекаем токен из куки
        if (!Request.Cookies.TryGetValue("hh_access_token", out var token))
        {
            return Unauthorized(new { error = "Сессия отсутствует" });
        }

        try
        {
            var response = await vacancyClient.SyncApplicationsAsync(new SyncApplicationsRequest 
            { 
                VacancyId = id,
                AccessToken = token 
            });

            return Ok(response);
        }
        catch (RpcException ex)
        {
            return StatusCode(500, new { error = "Ошибка синхронизации откликов", details = ex.Status.Detail });
        }
    }
    
    [HttpPost("{id}/candidates")]
    public async Task<IActionResult> AddCandidate(string id, [FromBody] CreateCandidateRequest request)
    {
        try
        {
            var response = await vacancyClient.AddManualCandidateAsync(new AddManualCandidateRequest
            {
                VacancyId = id,
                CandidateName = request.Name,
                CandidateEmail = request.Email,
                CandidatePhone = request.Phone,
                ResumeUrl = request.ResumeUrl,
                CoverLetter = request.CoverLetter
            });

            return Ok(response);
        }
        catch (RpcException ex)
        {
            return StatusCode(500, new { error = "Ошибка добавления кандидата", details = ex.Status.Detail });
        }
    }
    
    /// <summary>
    /// Создание вакансии на основе загруженного файла (PDF/DOCX)
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadVacancy(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Файл не предоставлен" });

        if (!Request.Cookies.TryGetValue("hh_access_token", out var token))
            return Unauthorized(new { error = "Сессия отсутствует" });

        try
        {
            // 1. Читаем файл в массив байт
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            // 2. Вызываем VacancyService
            var response = await vacancyClient.CreateVacancyFromFileAsync(new UploadFileRequest
            {
                FileContent = ByteString.CopyFrom(ms.ToArray()),
                Extension = Path.GetExtension(file.FileName),
                AccessToken = token
            });

            return Ok(response);
        }
        catch (RpcException ex)
        {
            return StatusCode(500, new { error = "Ошибка при парсинге вакансии", details = ex.Status.Detail });
        }
    }

    /// <summary>
    /// Добавление кандидата путем загрузки файла резюме
    /// </summary>
    [HttpPost("{id}/candidates/upload")]
    public async Task<IActionResult> UploadCandidate(string id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Файл резюме не предоставлен" });

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var response = await vacancyClient.AddCandidateFromFileAsync(new UploadCandidateFileRequest
            {
                VacancyId = id,
                FileContent = ByteString.CopyFrom(ms.ToArray()),
                Extension = Path.GetExtension(file.FileName)
            });

            return Ok(response);
        }
        catch (RpcException ex)
        {
            return StatusCode(500, new { error = "Ошибка при обработке резюме", details = ex.Status.Detail });
        }
    }
}