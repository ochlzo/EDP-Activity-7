using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace edp_gui_app;

public sealed class GmailPasswordResetEmailSender : IEmailSender
{
    private static readonly HttpClient HttpClient = new();

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _refreshToken;
    private readonly string _senderEmail;

    private GmailPasswordResetEmailSender(
        string clientId,
        string clientSecret,
        string refreshToken,
        string senderEmail)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _refreshToken = refreshToken;
        _senderEmail = senderEmail;
    }

    public static GmailPasswordResetEmailSender? FromEnvironment()
    {
        var clientId = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET");
        var refreshToken = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_REFRESH_TOKEN");
        var senderEmail = Environment.GetEnvironmentVariable("GMAIL_SENDER_EMAIL");

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(refreshToken) ||
            string.IsNullOrWhiteSpace(senderEmail))
        {
            return null;
        }

        return new GmailPasswordResetEmailSender(clientId, clientSecret, refreshToken, senderEmail);
    }

    public async Task SendPasswordResetAsync(
        PasswordResetEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var rawMessage = BuildRawMessage(
            message.ToEmail,
            "Site Management password reset code",
            BuildPasswordResetBody(message));

        await SendRawMessageAsync(rawMessage, accessToken, cancellationToken);
    }

    public async Task SendDocumentRequestAsync(
        DocumentRequestEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var rawMessage = BuildRawMessage(
            message.ToEmail,
            "Document request form",
            DocumentRequestEmailBody.Build(message));

        await SendRawMessageAsync(rawMessage, accessToken, cancellationToken);
    }

    private static string BuildPasswordResetBody(PasswordResetEmailMessage message)
    {
        return string.Join("\r\n", [
            $"Your password reset code is: {message.Code}",
            "",
            "This code expires in 15 minutes.",
            "If you did not request a password reset, ignore this email."
        ]);
    }

    private static async Task SendRawMessageAsync(
        string rawMessage,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://gmail.googleapis.com/gmail/v1/users/me/messages/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new GmailSendRequest(rawMessage));

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = _refreshToken,
            ["grant_type"] = "refresh_token"
        };

        using var response = await HttpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(form),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<GmailTokenResponse>(cancellationToken);
        if (string.IsNullOrWhiteSpace(token?.AccessToken))
        {
            throw new InvalidOperationException("Gmail access token response was empty.");
        }

        return token.AccessToken;
    }

    private string BuildRawMessage(string toEmailValue, string subjectValue, string body)
    {
        var toEmail = SanitizeHeader(toEmailValue);
        var senderEmail = SanitizeHeader(_senderEmail);
        var subject = SanitizeHeader(subjectValue);
        var mime = string.Join("\r\n", [
            $"From: {senderEmail}",
            $"To: {toEmail}",
            $"Subject: {subject}",
            "MIME-Version: 1.0",
            "Content-Type: text/plain; charset=utf-8",
            "",
            body
        ]);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(mime))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string SanitizeHeader(string value)
    {
        return value.ReplaceLineEndings(string.Empty).Trim();
    }

    private sealed record GmailSendRequest([property: JsonPropertyName("raw")] string Raw);

    private sealed record GmailTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken);
}
