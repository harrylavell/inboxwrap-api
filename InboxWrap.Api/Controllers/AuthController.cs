using System.Text.Json;
using InboxWrap.Clients;
using InboxWrap.Models.Reponses;
using Microsoft.AspNetCore.Mvc;

namespace InboxWrap.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class AuthController : ControllerBase
{
    private const string AUTHORIZE_URI = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    private const string TOKEN_URI = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    private const string SCOPES_URI = "https://graph.microsoft.com/";
    private const string SCOPES = "Mail.Read Mail.ReadWrite offline_access openid profile email";

    private readonly ISecretsManagerClient _secretsManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ISecretsManagerClient secretsManager, ILogger<AuthController> logger)
    {
        _secretsManager = secretsManager;
        _logger = logger;
    }

    [HttpGet("login")]
    public async Task<IActionResult> Login()
    {
        try
        {
            string authUrl = string.Empty;

            string? clientId = await _secretsManager.GetSecretAsync("AzureAdClientId");
            string? redirectUri = await _secretsManager.GetSecretAsync("AzureAdRedirectUri");

            if (clientId == null || redirectUri == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            authUrl = AUTHORIZE_URI +
                $"?client_id={Uri.EscapeDataString(clientId)}" +
                $"&response_type=code" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&response_mode=query" +
                $"&scope={Uri.EscapeDataString(SCOPES_URI+SCOPES)}" +
                $"&state={Guid.NewGuid().ToString()}";

            return Ok(authUrl);
            //return Redirect(authUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string code, string state)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Authorization code is missing.");
            }

            string? clientId = await _secretsManager.GetSecretAsync("AzureAdClientId");
            string? clientSecret = await _secretsManager.GetSecretAsync("AzureAdClientSecret");
            string? redirectUri = await _secretsManager.GetSecretAsync("AzureAdRedirectUri");

            if (clientId == null || redirectUri == null || clientSecret == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            Dictionary<string, string> tokenRequestParameters = new()
            {
                { "client_id", clientId },
                { "scope", SCOPES_URI+SCOPES },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" },
                { "client_secret", clientSecret },
            };

            using HttpClient httpClient = new();
            HttpResponseMessage response = await httpClient.PostAsync(TOKEN_URI, new FormUrlEncodedContent(tokenRequestParameters));

            string? responseContent = await response.Content.ReadAsStringAsync();
            TokenResponse? tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
