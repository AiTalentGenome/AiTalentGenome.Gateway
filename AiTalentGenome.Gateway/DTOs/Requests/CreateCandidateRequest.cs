namespace AiTalentGenome.Gateway.DTOs.Requests;

public record CreateCandidateRequest(
    string Name, 
    string Email, 
    string? Phone, 
    string? ResumeUrl, 
    string? CoverLetter);