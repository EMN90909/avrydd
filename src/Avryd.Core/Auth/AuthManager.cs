using Avryd.Core.Models;
using Avryd.Core.Settings;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Avryd.Core.Auth;

public class AuthManager
{
    private const string ApiBase = "https://avryd.onrender.com/api";
    private const string OAuthCallbackPort = "7891";
    private const string OAuthCallbackUrl = $"http://localhost:{OAuthCallbackPort}/oauth/callback";

    private readonly SettingsManager _settings;
    private readonly HttpClient _http;
    private TaskCompletionSource<string?>? _oauthTcs;

    public event EventHandler<UserProfile>? LoginSuccess;
    public event EventHandler<string>? LoginFailed;
    public event EventHandler? LoggedOut;

    public bool IsActivated => _settings.IsActivated;
    public UserProfile? CurrentUser => _settings.Profile;

    public AuthManager(SettingsManager settings)
    {
        _settings = settings;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<bool> BeginOAuthAsync(string provider)
    {
        // provider: "google", "facebook", "microsoft"
        _oauthTcs = new TaskCompletionSource<string?>();

        // Start local callback listener
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{OAuthCallbackPort}/oauth/callback/");
        try { listener.Start(); } catch { return false; }

        var authUrl = $"{ApiBase}/auth/{provider}?redirect_uri={Uri.EscapeDataString(OAuthCallbackUrl)}&hardware_id={HardwareId.Get()}";
        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

        // Wait for callback (up to 5 minutes)
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        try
        {
            var ctx = await listener.GetContextAsync().WaitAsync(cts.Token);
            var query = ctx.Request.Url?.Query ?? "";
            var token = ParseQueryParam(query, "token");
            var error = ParseQueryParam(query, "error");

            // Send response to browser
            var responseHtml = token != null
                ? "<html><body><h2>Avryd: Login successful! You may close this window.</h2></body></html>"
                : "<html><body><h2>Avryd: Login failed. Please return to the app.</h2></body></html>";
            var bytes = Encoding.UTF8.GetBytes(responseHtml);
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.ContentType = "text/html";
            await ctx.Response.OutputStream.WriteAsync(bytes);
            ctx.Response.Close();

            if (token != null)
            {
                return await ValidateSessionTokenAsync(token);
            }
            else
            {
                LoginFailed?.Invoke(this, error ?? "Authentication failed");
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            LoginFailed?.Invoke(this, "Login timed out");
            return false;
        }
        finally
        {
            listener.Stop();
        }
    }

    public async Task<bool> ValidateProductKeyAsync(string email, string productKey)
    {
        try
        {
            var hwId = HardwareId.Get();
            var payload = JsonConvert.SerializeObject(new
            {
                email,
                product_key = productKey,
                hardware_id = hwId
            });

            var response = await _http.PostAsync($"{ApiBase}/activate",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ActivationResponse>(content);

            if (response.IsSuccessStatusCode && result?.Success == true)
            {
                var activation = new ActivationInfo
                {
                    HardwareId = hwId,
                    ProductKey = productKey,
                    Email = email,
                    SessionToken = result.Token ?? "",
                    ActivatedAt = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow,
                    EncryptedToken = result.Token ?? ""
                };
                _settings.SaveActivation(activation);

                var profile = new UserProfile
                {
                    Email = email,
                    ProductKey = productKey,
                    IsActivated = true,
                    ActivationDate = DateTime.UtcNow,
                    HardwareId = hwId,
                    SessionToken = result.Token ?? "",
                    TokenExpiry = DateTime.UtcNow.AddDays(30)
                };
                _settings.SaveProfile(profile);
                LoginSuccess?.Invoke(this, profile);
                return true;
            }
            else
            {
                LoginFailed?.Invoke(this, result?.Message ?? "Invalid product key");
                return false;
            }
        }
        catch (Exception ex)
        {
            LoginFailed?.Invoke(this, $"Network error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ValidateSessionTokenAsync(string token)
    {
        try
        {
            var hwId = HardwareId.Get();
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _http.GetAsync($"{ApiBase}/user/me?hardware_id={hwId}");
            if (!response.IsSuccessStatusCode) { LoginFailed?.Invoke(this, "Token validation failed"); return false; }

            var content = await response.Content.ReadAsStringAsync();
            var profile = JsonConvert.DeserializeObject<UserProfile>(content);
            if (profile == null) { LoginFailed?.Invoke(this, "Invalid user data"); return false; }

            profile.SessionToken = token;
            profile.HardwareId = hwId;
            profile.IsActivated = true;

            var activation = new ActivationInfo
            {
                HardwareId = hwId,
                SessionToken = token,
                Email = profile.Email,
                ActivatedAt = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow,
                EncryptedToken = token
            };
            _settings.SaveActivation(activation);
            _settings.SaveProfile(profile);
            LoginSuccess?.Invoke(this, profile);
            return true;
        }
        catch (Exception ex)
        {
            LoginFailed?.Invoke(this, ex.Message);
            return false;
        }
    }

    public async Task<bool> CheckSessionValidAsync()
    {
        if (!_settings.IsActivated || _settings.Activation == null) return false;
        var expiry = _settings.Profile?.TokenExpiry;
        if (expiry.HasValue && expiry.Value < DateTime.UtcNow)
        {
            return await RefreshSessionAsync();
        }
        return true;
    }

    private async Task<bool> RefreshSessionAsync()
    {
        try
        {
            var token = _settings.Activation?.SessionToken ?? "";
            var hwId = HardwareId.Get();
            var payload = JsonConvert.SerializeObject(new { token, hardware_id = hwId });
            var response = await _http.PostAsync($"{ApiBase}/auth/refresh",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ActivationResponse>(content);
                if (result?.Success == true && _settings.Activation != null)
                {
                    _settings.Activation.SessionToken = result.Token ?? token;
                    _settings.Activation.LastSeen = DateTime.UtcNow;
                    _settings.SaveActivation(_settings.Activation);
                    if (_settings.Profile != null)
                    {
                        _settings.Profile.TokenExpiry = DateTime.UtcNow.AddDays(30);
                        _settings.SaveProfile(_settings.Profile);
                    }
                    return true;
                }
            }
            return false;
        }
        catch { return false; }
    }

    public void Logout()
    {
        _settings.ClearActivation();
        LoggedOut?.Invoke(this, EventArgs.Empty);
    }

    private static string? ParseQueryParam(string query, string key)
    {
        if (string.IsNullOrEmpty(query)) return null;
        var clean = query.TrimStart('?');
        foreach (var part in clean.Split('&'))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0] == key)
                return Uri.UnescapeDataString(kv[1]);
        }
        return null;
    }

    private class ActivationResponse
    {
        [JsonProperty("success")] public bool Success { get; set; }
        [JsonProperty("token")] public string? Token { get; set; }
        [JsonProperty("message")] public string? Message { get; set; }
    }
}
