# ParaVolley notification delivery setup

The Notifications settings page supports real SMTP email, BulkSMS SMS, and Firebase Cloud Messaging (FCM) push delivery.

Do not commit provider passwords, API tokens, service-account JSON, or other secrets to Git.

## Local web configuration

Store secrets with ASP.NET Core user-secrets from the repository root.

### Email / SMTP

Required keys:

- `EmailSettings:SmtpHost`
- `EmailSettings:SmtpPort`
- `EmailSettings:SenderEmail`
- `EmailSettings:SenderPassword`
- `EmailSettings:SenderName`
- `EmailSettings:EnableSsl`

Example key names only:

```powershell
dotnet user-secrets set "EmailSettings:SmtpHost" "YOUR_SMTP_HOST"
dotnet user-secrets set "EmailSettings:SmtpPort" "587"
dotnet user-secrets set "EmailSettings:SenderEmail" "YOUR_SENDER_EMAIL"
dotnet user-secrets set "EmailSettings:SenderPassword" "YOUR_SMTP_PASSWORD"
dotnet user-secrets set "EmailSettings:SenderName" "ParaVolley Mpumalanga"
dotnet user-secrets set "EmailSettings:EnableSsl" "true"
```

### BulkSMS SMS

ParaVolley uses the BulkSMS JSON REST API at `https://api.bulksms.com/v1/messages`.

Create an API token in BulkSMS under **Settings -> Developers -> API Tokens**. New BulkSMS accounts authenticate with an API token: the Token ID is used as the HTTP Basic username and the Token Secret as the password.

Required keys:

- `BulkSMS:TokenId`
- `BulkSMS:TokenSecret`

Optional key:

- `BulkSMS:From` — only configure this if BulkSMS has provided/approved a sender value for your account. If omitted, ParaVolley lets BulkSMS choose the applicable sender/route.

```powershell
dotnet user-secrets set "BulkSMS:TokenId" "YOUR_BULKSMS_TOKEN_ID"
dotnet user-secrets set "BulkSMS:TokenSecret" "YOUR_BULKSMS_TOKEN_SECRET"
```

Optional sender example:

```powershell
dotnet user-secrets set "BulkSMS:From" "YOUR_APPROVED_SENDER"
```

South African recipient numbers should be entered in international format, for example `+2782...` rather than `082...`.

BulkSMS provides a small number of free SMS credits for account testing after mobile-number activation. Real SMS delivery after those credits are used requires purchasing additional credits.

The previous Twilio settings are no longer used by ParaVolley and can be removed from local user-secrets after BulkSMS is working:

```powershell
dotnet user-secrets remove "Twilio:AccountSid"
dotnet user-secrets remove "Twilio:AuthToken"
dotnet user-secrets remove "Twilio:FromNumber"
```

### Firebase server-side push

Create a Firebase service account with Firebase Cloud Messaging access. Base64-encode the complete service-account JSON and store it as a secret.

Required keys:

- `Firebase:ProjectId`
- `Firebase:ServiceAccountJsonBase64`

Do not place the service-account JSON file in the repository.

## Android Firebase configuration

The Android app initializes Firebase programmatically, so `google-services.json` is not committed or required by the build.

Add these non-secret Firebase Android identifiers to your local `~/.gradle/gradle.properties` or project Gradle properties:

```properties
PARAVOLLEY_FIREBASE_APPLICATION_ID=YOUR_FIREBASE_ANDROID_APP_ID
PARAVOLLEY_FIREBASE_API_KEY=YOUR_FIREBASE_WEB_API_KEY
PARAVOLLEY_FIREBASE_PROJECT_ID=YOUR_FIREBASE_PROJECT_ID
PARAVOLLEY_FIREBASE_SENDER_ID=YOUR_FIREBASE_SENDER_ID
```

When all four are present, logged-in Player accounts automatically subscribe to the FCM topic `players`. Logging out unsubscribes the device.

## Scheduled event reminders

The web application runs an event-reminder background service every 15 minutes. Upcoming events are eligible for one reminder when they enter the 24-hour window before their scheduled South African date/time.

Delivery follows the `event_reminders` Email/SMS/Push switches in **Settings -> Notifications**. A persistent `EventReminderDispatches` ledger prevents the same event schedule from being sent repeatedly. If an event is rescheduled to a different date/time, the new schedule can receive a new reminder.

The dispatch ledger is created by the `AddEventReminderDispatches` EF Core migration when the application starts and runs `Database.Migrate()`. The ledger is operational notification state and is intentionally not part of the application entity model.

## Railway production configuration

Add the same web keys as Railway Variables. Keep Auto Deploy behavior unchanged. Never place secret values in `appsettings.json` or source control.

For BulkSMS in Railway, add:

- `BulkSMS__TokenId`
- `BulkSMS__TokenSecret`
- optionally `BulkSMS__From`

ASP.NET Core maps double underscores in environment-variable names to configuration colons.

## Testing

After the providers are configured:

1. Open **Settings -> Notifications** as Admin.
2. Confirm the provider badge changes to **Configured**.
3. Use **Send Test Email**, **Send Test SMS**, and **Send Test Push**.
4. For SMS, test with a real recipient number in international format such as `+27...`.
5. For push testing, install/run the Android debug app with the Firebase Gradle properties present and sign in as an approved Player.
6. Enable/disable event-channel preferences from the table and confirm the values persist after refresh.
7. To test scheduled reminders safely, set **Event Reminders** to Push only, create an Upcoming event whose scheduled date/time is within the next 24 hours, and keep the app running for at least 30 seconds. The reminder scheduler performs its first check shortly after startup and then every 15 minutes.
