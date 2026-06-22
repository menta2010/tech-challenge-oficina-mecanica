namespace Oficina.Application.Identidade;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, DateTime ExpiraEm, string Username, string Role);
