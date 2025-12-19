using Blazored.LocalStorage;
using SmartOnFhirApp.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartOnFhirApp.Services;

public class SmartAuthService
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;

    private const string TokenStorageKey = "smart_token";
    private const string PatientStorageKey = "smart_patient";
    private const string IssStorageKey = "smart_iss";

    public SmartAuthService(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
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

        // 授權後強制清除舊的病患選擇，確保進入病患清單
        await _localStorage.RemoveItemAsync(PatientStorageKey);

        await _localStorage.SetItemAsync(IssStorageKey, fhirBaseUrl);
    }

    public async Task ClearStoredDataAsync()
    {
        await _localStorage.RemoveItemAsync(TokenStorageKey);
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

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetStoredAccessTokenAsync();
        return !string.IsNullOrEmpty(token);
    }
}
