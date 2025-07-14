# Outlook Integration Setup

## 🔄 **Migration Completed: Dummy Data → Real Outlook Emails**

The `InitializeDummyUserDataAsync` function in `UserService.cs` has been replaced with `InitializeRealOutlookDataAsync` that fetches real emails from Microsoft Outlook using the Microsoft Graph API.

## 📝 **API Keys Configuration**

### Current Keys (from example folder)
The following Microsoft Graph credentials are configured in `appsettings.json`:

```json
{
  "MicrosoftGraph": {
    "ClientId": "746a4de4-9a1e-4673-bf29-d784e5ab377f",
    "ClientSecret": "qCx8Q~w26zsfP13H~wX6VVBGSIUnKaQBGcOprbyn",
    "TenantId": "common",
    "Instance": "https://login.microsoftonline.com/",
    "GraphApiUrl": "https://graph.microsoft.com/"
  }
}
```

### 🔧 **Easy Key Replacement**
To replace with your own Azure App Registration:

1. **Open** `appsettings.json`
2. **Update** the `MicrosoftGraph` section with your credentials:
   - `ClientId`: Your App Registration Client ID
   - `ClientSecret`: Your App Registration Client Secret  
   - `TenantId`: Your Azure AD Tenant ID (or "common" for multi-tenant)

## 🧪 **Testing the Integration**

### Prerequisites
1. **Valid Access Token**: Users need a valid Microsoft Graph access token
2. **Permissions**: The token must have `Mail.Read` and `Mail.Send` permissions 🔄
3. **MailAccount**: User must have a MailAccount record with the access token stored

### Test Endpoints

#### 1. **Sync Real Outlook Emails**
```http
POST /api/outlook-test/sync-emails
Authorization: Bearer {your-jwt-token}
```
Replaces dummy data with real Outlook emails for the authenticated user.

#### 2. **Validate Access Token**
```http
POST /api/outlook-test/validate-token
Content-Type: application/json

"your-outlook-access-token-here"
```

#### 3. **Test Email Fetching**
```http
POST /api/outlook-test/fetch-emails-test
Content-Type: application/json

{
  "accessToken": "your-outlook-access-token-here",
  "maxEmails": 10
}
```

#### 4. **Send Simple Email** 🆕
```http
POST /api/outlook-test/send-email
Content-Type: application/json

{
  "accessToken": "your-outlook-access-token-here",
  "subject": "Test Email from Falcon",
  "body": "Hello! This is a test email sent via Microsoft Graph API.",
  "recipients": ["recipient@example.com"]
}
```

#### 5. **Send Detailed Email (CC/BCC)** 🆕
```http
POST /api/outlook-test/send-detailed-email
Content-Type: application/json

{
  "accessToken": "your-outlook-access-token-here",
  "subject": "Detailed Test Email",
  "body": "<h1>HTML Body</h1><p>This supports <strong>HTML</strong> content!</p>",
  "toRecipients": ["primary@example.com"],
  "ccRecipients": ["cc@example.com"],
  "bccRecipients": ["bcc@example.com"]
}
```

## 🔄 **How It Works**

### Old Flow (Dummy Data)
```
User Registration → Create MailAccount → Generate 20 fake emails → Save to DB
```

### New Flow (Real Outlook)
```
User Registration → Create MailAccount → Store Access Token → Fetch Real Emails → Save to DB
```

### Key Features
- ✅ **Real Email Fetching**: Uses Microsoft Graph API v5.85.0
- ✅ **Real Email Sending**: Send emails via Microsoft Graph API 🆕
- ✅ **Advanced Email Options**: Support for CC/BCC recipients 🆕
- ✅ **HTML Email Support**: Send rich HTML content in emails 🆕
- ✅ **Token Validation**: Checks if access tokens are still valid
- ✅ **Auto-Tagging**: Basic keyword-based email tagging
- ✅ **Duplicate Prevention**: Avoids fetching emails multiple times
- ✅ **Error Handling**: Comprehensive error logging and handling

## 🛠 **Code Changes Summary**

### New Files
- `Services/OutlookService.cs` - Microsoft Graph integration
- `Controllers/OutlookTestController.cs` - Test endpoints
- `OUTLOOK_INTEGRATION_README.md` - This file

### Modified Files
- `Services/UserService.cs` - Replaced dummy function
- `Program.cs` - Registered OutlookService
- `appsettings.json` - Added Microsoft Graph configuration

### Dependencies Used
- `Microsoft.Graph` v5.85.0 (already installed)
- `Microsoft.Kiota.Abstractions.Authentication` (for token provider)

## 🔐 **Security Notes**

1. **Access Tokens**: Store securely in MailAccount.Token field
2. **Token Expiry**: Tokens expire after 1 hour, implement refresh logic
3. **Permissions**: Only request minimum required permissions (Mail.Read)
4. **API Keys**: Keep Client Secret secure, consider using Azure Key Vault in production

## 🚀 **Next Steps**

1. **Test** with the provided access token from the example folder
2. **Replace** API keys with your own Azure App Registration
3. **Implement** OAuth2 flow for users to authenticate and get access tokens
4. **Add** refresh token logic for automatic token renewal
5. **Enhance** auto-tagging with AI/ML for better email categorization

---
**✅ Integration Complete!** The system now fetches real Outlook emails instead of generating dummy data. 