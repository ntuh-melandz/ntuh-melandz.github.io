using Blazored.LocalStorage;
using SmartOnFhirApp.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SmartOnFhirApp.Services;

public class SmartAuthService
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private const string TokenStorageKey = "smart_token";
    private const string RefreshTokenStorageKey = "smart_refresh_token";
    private const string ExpiresAtStorageKey = "smart_token_expires_at";
    private const string PatientStorageKey = "smart_patient";
    private const string IssStorageKey = "smart_iss";

    public SmartAuthService(ILocalStorageService localStorage, HttpClient httpClient, IConfiguration configuration)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<SmartConfiguration?> GetSmartConfigurationAsync(string fhirBaseUrl)
    {
        try
        {
            var configUrl = $"{fhirBaseUrl}/.well-known/smart-configuration";
            var response = await _httpClient.GetAsync(configUrl);

            if (response.IsSuccessStatusCode)
            {
                var config = await response.Content.ReadFromJsonAsync<SmartConfiguration>();
                return config;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching SMART configuration: {ex.Message}");
        }

        return null;
    }

    public async Task<string?> GetStoredAccessTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(TokenStorageKey);
    }

    public async Task<string?> GetStoredPatientIdAsync()
    {
        return await _localStorage.GetItemAsync<string>(PatientStorageKey);
    }

    public async Task<string?> GetStoredFhirBaseUrlAsync()
    {
        return await _localStorage.GetItemAsync<string>(IssStorageKey);
    }

    public async Task StoreTokenResponseAsync(TokenResponse tokenResponse, string fhirBaseUrl)
    {
        await _localStorage.SetItemAsync(TokenStorageKey, tokenResponse.AccessToken ?? "");
        
        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
        {
            await _localStorage.SetItemAsync(RefreshTokenStorageKey, tokenResponse.RefreshToken);
        }

        if (tokenResponse.ExpiresIn > 0)
        {
            var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            await _localStorage.SetItemAsync(ExpiresAtStorageKey, expiresAt);
        }

        // 授權後強制清除舊的病患選擇，確保進入病患清單
        await _localStorage.RemoveItemAsync(PatientStorageKey);

        await _localStorage.SetItemAsync(IssStorageKey, fhirBaseUrl);
    }

    public async Task ClearStoredDataAsync()
    {
        await _localStorage.RemoveItemAsync(TokenStorageKey);
        await _localStorage.RemoveItemAsync(RefreshTokenStorageKey);
        await _localStorage.RemoveItemAsync(ExpiresAtStorageKey);
        await _localStorage.RemoveItemAsync(PatientStorageKey);
        await _localStorage.RemoveItemAsync(IssStorageKey);
    }

    public async Task StorePatientIdAsync(string patientId)
    {
        await _localStorage.SetItemAsync(PatientStorageKey, patientId);
    }

    public async Task RemovePatientIdAsync()
    {
        await _localStorage.RemoveItemAsync(PatientStorageKey);
    }

    public async Task<TokenResponse?> ExchangeCodeForTokenAsync(
        string tokenEndpoint,
        string code,
        string redirectUri,
        string clientId)
    {
        try
        {
            var requestData = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", clientId }
            };

            var response = await _httpClient.PostAsync(
                tokenEndpoint,
                new FormUrlEncodedContent(requestData));

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
                return tokenResponse;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Token exchange failed: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exchanging code for token: {ex.Message}");
        }

        return null;
    }

    public string BuildAuthorizationUrl(
        string authorizationEndpoint,
        string clientId,
        string redirectUri,
        string scope,
        string state,
        string aud,
        string? launch = null)
    {
        var parameters = new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "client_id", clientId },
            { "redirect_uri", redirectUri },
            { "scope", scope },
            { "state", state },
            { "aud", aud }
        };

        if (!string.IsNullOrEmpty(launch))
        {
            parameters["launch"] = launch;
        }

        var queryString = string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        return $"{authorizationEndpoint}?{queryString}";
    }

    public async Task<bool> IsTokenExpiredAsync()
    {
        try
        {
            var expiresAt = await _localStorage.GetItemAsync<DateTime?>(ExpiresAtStorageKey);
            if (expiresAt.HasValue)
            {
                // Add a buffer of 5 minutes to refresh before it actually expires
                return DateTime.UtcNow.AddMinutes(5) >= expiresAt.Value;
            }
        }
        catch
        {
            // If we can't parse the date, assume it's not expired or handle gracefully
        }
        return false;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var clientId = _configuration["Fhir:ClientId"] ?? "ntuh_fhir_app";
            var refreshToken = await _localStorage.GetItemAsync<string>(RefreshTokenStorageKey);
            var fhirBaseUrl = await GetStoredFhirBaseUrlAsync();

            if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(fhirBaseUrl))
            {
                return false;
            }

            var config = await GetSmartConfigurationAsync(fhirBaseUrl);
            if (config == null || string.IsNullOrEmpty(config.TokenEndpoint))
            {
                return false;
            }

            var requestData = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "client_id", clientId }
            };

            var response = await _httpClient.PostAsync(
                config.TokenEndpoint,
                new FormUrlEncodedContent(requestData));

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
                if (tokenResponse != null)
                {
                    await StoreTokenResponseAsync(tokenResponse, fhirBaseUrl);
                    return true;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Token refresh failed: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing token: {ex.Message}");
        }

        return false;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetStoredAccessTokenAsync();
        return !string.IsNullOrEmpty(token);
    }
}
