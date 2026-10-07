using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NotesClient.DTOs;
using NotesClient.Models;

namespace NotesClient.Services;

public class NotesApiService
{
    private readonly HttpClient _httpClient;

    private string? _token;

    public string? CurrentUsername { get; private set; }

    public string? CurrentRole { get; private set; }

    // Used only for controlling client-side UI visibility.
    public bool IsAdmin =>
        CurrentRole == "Admin" ||
        CurrentRole == "MainAdmin";

    // Used only for controlling client-side UI visibility.
    public bool IsMainAdmin =>
        CurrentRole == "MainAdmin";

    public event Action? SessionExpired;

    public NotesApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5140/")
        };
    }

    // =========================
    // AUTHENTICATION
    // =========================

    public async Task LoginAsync(
        string username,
        string password)
    {
        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var response = await _httpClient.PostAsJsonAsync(
            "auth/login",
            request);

        await EnsureSuccessAsync(
            response,
            handleUnauthorized: false);

        var loginResponse =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        if (loginResponse == null ||
            string.IsNullOrWhiteSpace(loginResponse.Token))
        {
            throw new HttpRequestException(
                "Login response did not contain a token.");
        }

        _token = loginResponse.Token;

        // Set the JWT for all authenticated requests.
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _token);

        // Read display information from the JWT.
        ReadUserInformationFromToken();
    }

    // =========================
    // NOTES
    // =========================

    public async Task<PagedNotesResponse> GetNotesAsync(
        string? search,
        int? categoryId,
        int page,
        int pageSize,
        string sort)
    {
        var url =
            $"notes/?page={page}" +
            $"&pageSize={pageSize}" +
            $"&sort={Uri.EscapeDataString(sort)}";

        if (!string.IsNullOrWhiteSpace(search))
        {
            url +=
                $"&search={Uri.EscapeDataString(search)}";
        }

        if (categoryId.HasValue)
        {
            url +=
                $"&categoryId={categoryId.Value}";
        }

        var response = await _httpClient.GetAsync(url);

        await EnsureSuccessAsync(response);

        return await response.Content
                   .ReadFromJsonAsync<PagedNotesResponse>()
               ?? new PagedNotesResponse();
    }

    public async Task<Note?> CreateNoteAsync(
        CreateNoteRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "notes/",
            request);

        await EnsureSuccessAsync(response);

        return await response.Content
            .ReadFromJsonAsync<Note>();
    }

    public async Task<Note?> UpdateNoteAsync(
        int id,
        UpdateNoteRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"notes/{id}",
            request);

        await EnsureSuccessAsync(response);

        return await response.Content
            .ReadFromJsonAsync<Note>();
    }

    public async Task DeleteNoteAsync(int id)
    {
        var response = await _httpClient.DeleteAsync(
            $"notes/{id}");

        await EnsureSuccessAsync(response);
    }

    // =========================
    // CATEGORIES
    // =========================

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var response = await _httpClient.GetAsync(
            "categories/");

        await EnsureSuccessAsync(response);

        return await response.Content
                   .ReadFromJsonAsync<List<Category>>()
               ?? new List<Category>();
    }

    public async Task<Category?> CreateCategoryAsync(
        string name)
    {
        var category = new Category
        {
            Name = name
        };

        var response = await _httpClient.PostAsJsonAsync(
            "categories/",
            category);

        await EnsureSuccessAsync(response);

        return await response.Content
            .ReadFromJsonAsync<Category>();
    }

    public async Task<Category?> UpdateCategoryAsync(
        int id,
        string name)
    {
        var category = new Category
        {
            Id = id,
            Name = name
        };

        var response = await _httpClient.PutAsJsonAsync(
            $"categories/{id}",
            category);

        await EnsureSuccessAsync(response);

        return await response.Content
            .ReadFromJsonAsync<Category>();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var response = await _httpClient.DeleteAsync(
            $"categories/{id}");

        await EnsureSuccessAsync(response);
    }

    // =========================
    // ADMIN
    // =========================

    public async Task<List<AdminUser>> GetAdminUsersAsync()
    {
        var response = await _httpClient.GetAsync(
            "admin/users");

        await EnsureSuccessAsync(response);

        return await response.Content
                   .ReadFromJsonAsync<List<AdminUser>>()
               ?? new List<AdminUser>();
    }

    public async Task<AdminUser?> ChangeUserRoleAsync(
        int id,
        string role)
    {
        var request = new
        {
            Role = role
        };

        var response = await _httpClient.PutAsJsonAsync(
            $"admin/users/{id}/role",
            request);

        await EnsureSuccessAsync(response);

        return await response.Content
            .ReadFromJsonAsync<AdminUser>();
    }

    public async Task DeleteUserAsync(int id)
    {
        var response = await _httpClient.DeleteAsync(
            $"admin/users/{id}");

        await EnsureSuccessAsync(response);
    }

    // =========================
    // REGISTRATION
    // =========================

    public async Task RegisterAsync(
        string username,
        string password)
    {
        var request = new RegisterRequest
        {
            Username = username,
            Password = password
        };

        var response = await _httpClient.PostAsJsonAsync(
            "auth/register",
            request);

        await EnsureSuccessAsync(
            response,
            handleUnauthorized: false);
    }

    // =========================
    // ERROR HANDLING
    // =========================

    private async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        bool handleUnauthorized = true)
    {
        if (response.IsSuccessStatusCode)
            return;

        // Handle an expired or invalid JWT.
        if (response.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized &&
            handleUnauthorized)
        {
            Logout();

            SessionExpired?.Invoke();

            throw new HttpRequestException(
                "Your session has expired.");
        }

        var responseText =
            await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new HttpRequestException(
                $"Request failed with status code " +
                $"{(int)response.StatusCode} " +
                $"({response.StatusCode}).");
        }

        try
        {
            using var document =
                JsonDocument.Parse(responseText);

            var root = document.RootElement;

            if (root.TryGetProperty(
                    "error",
                    out var errorProperty))
            {
                if (errorProperty.ValueKind ==
                    JsonValueKind.String)
                {
                    throw new HttpRequestException(
                        errorProperty.GetString()
                        ?? "Unknown API error.");
                }

                if (errorProperty.ValueKind ==
                    JsonValueKind.Array)
                {
                    var errors = errorProperty
                        .EnumerateArray()
                        .Where(error =>
                            error.ValueKind ==
                            JsonValueKind.String)
                        .Select(error =>
                            error.GetString())
                        .Where(error =>
                            !string.IsNullOrWhiteSpace(error))
                        .ToList();

                    if (errors.Count > 0)
                    {
                        throw new HttpRequestException(
                            string.Join(
                                "\n",
                                errors));
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Fall back to the raw response below.
        }

        throw new HttpRequestException(responseText);
    }

    // =========================
    // LOGOUT
    // =========================

    public void Logout()
    {
        _token = null;

        CurrentUsername = null;
        CurrentRole = null;

        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    // =========================
    // JWT INFORMATION
    // =========================

    private void ReadUserInformationFromToken()
    {
        if (string.IsNullOrWhiteSpace(_token))
            return;

        var parts = _token.Split('.');

        if (parts.Length != 3)
            return;

        var payload = parts[1];

        var padding = payload.Length % 4;

        if (padding > 0)
        {
            payload += new string(
                '=',
                4 - padding);
        }

        payload = payload
            .Replace('-', '+')
            .Replace('_', '/');

        try
        {
            var jsonBytes =
                Convert.FromBase64String(payload);

            var json =
                Encoding.UTF8.GetString(jsonBytes);

            using var document =
                JsonDocument.Parse(json);

            var root = document.RootElement;

            // Read the username claim.
            if (root.TryGetProperty(
                    "unique_name",
                    out var username))
            {
                CurrentUsername =
                    username.GetString();
            }
            else if (root.TryGetProperty(
                         "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
                         out var nameClaim))
            {
                CurrentUsername =
                    nameClaim.GetString();
            }

            // Read the role claim.
            if (root.TryGetProperty(
                    "role",
                    out var role))
            {
                CurrentRole =
                    role.GetString();
            }
            else if (root.TryGetProperty(
                         "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                         out var roleClaim))
            {
                CurrentRole =
                    roleClaim.GetString();
            }
        }
        catch (FormatException)
        {
            CurrentUsername = null;
            CurrentRole = null;
        }
        catch (JsonException)
        {
            CurrentUsername = null;
            CurrentRole = null;
        }
    }
}