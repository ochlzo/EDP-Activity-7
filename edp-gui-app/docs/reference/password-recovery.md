# Password Recovery

The desktop app supports email-based password recovery for site owner accounts.

Required environment variables:

```powershell
$env:GOOGLE_OAUTH_CLIENT_ID = "<client id>"
$env:GOOGLE_OAUTH_CLIENT_SECRET = "<client secret>"
$env:GOOGLE_OAUTH_REFRESH_TOKEN = "<refresh token>"
$env:GMAIL_SENDER_EMAIL = "<sender gmail address>"
```

For local development, you can put the same values in `.env` at the app root:

```text
GOOGLE_OAUTH_CLIENT_ID=<client id>
GOOGLE_OAUTH_CLIENT_SECRET=<client secret>
GOOGLE_OAUTH_REFRESH_TOKEN=<refresh token>
GMAIL_SENDER_EMAIL=<sender gmail address>
```

Run the app from this folder so it can find `.env`:

```powershell
dotnet run --project edp-gui-app.csproj
```

Process environment variables take priority over `.env` values.

The reset flow creates `site_owner_password_reset` automatically if it does not
exist. Reset codes expire after 15 minutes and are stored as hashes only.

Do not commit real Gmail credentials. If credentials were shared in chat,
rotate the client secret and refresh token before using the app outside local
development.
