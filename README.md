# Teams Adaptive Card Management Application

A Microsoft Teams bot application that enables administrators to create, manage, and publish adaptive cards to groups of users. This solution provides a web-based management interface for creating adaptive card templates and a Teams bot for delivering those cards to users.

## Features

- ?? **Template Management**: Create and edit adaptive card templates with JSON payloads
- ?? **Azure Storage Only**: Lightweight solution using Azure Table Storage for metadata and Blob Storage for JSON payloads (no SQL database required)
- ?? **Teams Bot Integration**: Deliver adaptive cards directly to users via Teams bot conversations
- ?? **Message Logging**: Track message delivery status and recipients
- ?? **Authentication**: Supports both Teams SSO and MSAL authentication
- ?? **Modern UI**: React-based web interface with Fluent UI components
- ?? **Scalable Storage**: Blob storage handles large JSON payloads (no 64KB table storage limit)
- ? **No Database Required**: Pure Azure Storage solution for minimal infrastructure overhead



## Technology Stack

### Backend
- **.NET 10**: Modern C# application framework
- **ASP.NET Core**: Web API and hosting
- **Azure Table Storage**: Metadata and reference storage (lightweight, no SQL database required)
- **Azure Blob Storage**: JSON payload storage (unlimited size)
- **Microsoft Bot Framework**: Teams bot integration
- **Microsoft Graph API**: User and Teams data access

### Frontend
- **React 18**: Modern UI library
- **TypeScript**: Type-safe JavaScript
- **Fluent UI**: Microsoft's design system
- **Vite**: Fast build tool
- **Azure MSAL**: Authentication library
- **Teams JS SDK**: Teams integration

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 18+](https://nodejs.org/)
- [Azure Subscription](https://azure.microsoft.com/free/)
- [Microsoft 365 Developer Tenant](https://developer.microsoft.com/microsoft-365/dev-program) (optional for development)

## Project Structure

The solution is located in the `src/Full` directory. All projects (Web.Server, Common.Engine, Common.DataUtils, and UnitTests) are within this subdirectory.

## Azure Resources Required

1. **Azure Storage Account**: For table storage (metadata) and blob storage (JSON payloads) - **No SQL database needed**
2. **Azure AD App Registration**: For authentication and bot identity
3. **Azure Bot Service**: For Teams bot functionality (optional)
4. **Application Insights**: For telemetry (optional)

### Storage Account Configuration

The storage account needs both:
- **Table Storage**: Enabled for storing template metadata and message logs
- **Blob Storage**: Enabled with a container named `message-templates` (automatically created by the application)

**Note**: This solution uses only Azure Storage (tables + blobs) for all data persistence. No SQL database, Entity Framework, or other database infrastructure is required, making it extremely lightweight and cost-effective.

## Teams Bot Setup

This section covers creating a Teams bot, configuring the required Microsoft Graph permissions, and setting up the bot messaging endpoint.

### 1. Create a Bot in Teams Developer Portal

1. Navigate to the [Teams Developer Portal](https://dev.teams.microsoft.com/)
2. Sign in with your Microsoft 365 account
3. Go to **Tools** ? **Bot management** in the left navigation
4. Click **+ New Bot**
5. Enter a name for your bot (e.g., "Office Nudge Bot")
6. Click **Add**
7. Once created, note down the **Bot ID** - this is your `MicrosoftAppId`
8. Click on your newly created bot to open its settings
9. Under **Client secrets**, click **Add a client secret**
10. Copy and securely store the generated secret - this is your `MicrosoftAppPassword`

> ?? **Important**: The client secret is only shown once. Store it securely immediately.

### 2. Configure Graph Permissions for the Bot's Entra ID App

When you create a bot in the Teams Developer Portal, an Entra ID (Azure AD) app registration is automatically created. You need to add the required Microsoft Graph permissions to this app.

1. Go to the [Azure Portal](https://portal.azure.com/)
2. Navigate to **Microsoft Entra ID** ? **App registrations**
3. Search for your bot by name or Bot ID (the app will have the same name as your bot)
4. Click on the app registration to open it
5. Go to **API permissions** in the left menu
6. Click **+ Add a permission**
7. Select **Microsoft Graph** ? **Application permissions**
8. Add the following permissions:
   - `User.Read.All` - Required for reading user information and statistics
   - `TeamsActivity.Send` - Required for sending activity feed notifications
   - `TeamsAppInstallation.ReadWriteForUser.All` - Required for the bot to install itself to user conversations and send messages

9. After adding all permissions, click **Grant admin consent for [Your Tenant]**
10. Confirm by clicking **Yes**

> ?? **Note**: All Application permissions require admin consent. Without granting admin consent, the bot will not be able to send messages or access user information.

#### Verifying Permissions

After granting consent, your API permissions should show a green checkmark next to each permission indicating "Granted for [Your Tenant]".

| Permission | Type | Description |
|------------|------|-------------|
| `User.Read.All` | Application | Read all users' full profiles |
| `TeamsActivity.Send` | Application | Send activity feed notifications |
| `TeamsAppInstallation.ReadWriteForUser.All` | Application | Manage Teams app installations for users |

### 3. Configure Bot Messaging Endpoint (After Deployment)

Once your application is deployed to Azure App Service, you need to configure the bot's messaging endpoint so Teams knows where to send messages.

1. Deploy your application to Azure App Service (see [Deployment](#deployment) section)
2. Note your App Service URL (e.g., `https://your-app-name.azurewebsites.net`)
3. Go back to the [Teams Developer Portal](https://dev.teams.microsoft.com/)
4. Navigate to **Tools** ? **Bot management**
5. Click on your bot to open its settings
6. Under **Configure** ? **Endpoint address**, enter:
   ```
   https://your-app-name.azurewebsites.net/api/messages
   ```
7. Click **Save**

> ?? **Tip**: The messaging endpoint must be HTTPS and publicly accessible. Azure App Service provides this by default.

#### Testing the Bot Connection

After configuring the endpoint:
1. Open Microsoft Teams
2. Search for your bot by name in the search bar
3. Start a conversation with the bot
4. The bot should respond, confirming the connection is working

If the bot doesn't respond:
- Verify the endpoint URL is correct
- Check that the App Service is running
- Review Application Insights logs for errors
- Ensure `MicrosoftAppId` and `MicrosoftAppPassword` are correctly configured in your App Service settings

## Configuration

This section covers configuring the application for local development and production. Before proceeding, ensure you have completed the [Teams Bot Setup](#teams-bot-setup) section to create your bot and configure the required permissions.

> :memo: **Note**: When you create a bot in the Teams Developer Portal, an Entra ID (Azure AD) app registration is automatically created. This same app registration is used for both the bot identity and Microsoft Graph API access. You will use the Bot ID as your `MicrosoftAppId` / `GraphConfig:ClientId` and the bot client secret as your `MicrosoftAppPassword` / `GraphConfig:ClientSecret`.

### 1. Backend Configuration (User Secrets)

**?? Important**: For local development, use **User Secrets** to store sensitive configuration. Never commit secrets to source control.

#### Setting Up User Secrets

Navigate to the Web.Server project directory and initialize user secrets:

```bash
cd Web/Web.Server
dotnet user-secrets init
```

Then add your configuration values:

```bash
# Graph API Configuration
dotnet user-secrets set "GraphConfig:ClientId" "your-app-registration-client-id"
dotnet user-secrets set "GraphConfig:ClientSecret" "your-app-registration-client-secret"
dotnet user-secrets set "GraphConfig:TenantId" "your-tenant-id"

# Storage Connection
dotnet user-secrets set "ConnectionStrings:Storage" "DefaultEndpointsProtocol=https;AccountName=yourstorageaccount;AccountKey=your-storage-key;EndpointSuffix=core.windows.net"

# Bot Configuration
dotnet user-secrets set "MicrosoftAppId" "your-bot-app-id"
dotnet user-secrets set "MicrosoftAppPassword" "your-bot-app-password"

# Optional: Application Insights
dotnet user-secrets set "APPLICATIONINSIGHTS_CONNECTION_STRING" "InstrumentationKey=your-key;IngestionEndpoint=https://..."

# Development Settings
dotnet user-secrets set "DevMode" "true"
dotnet user-secrets set "TestUPN" "your-test-user@yourtenant.onmicrosoft.com"
```

#### Configuration Structure Reference

Your secrets will follow this structure (stored securely outside your project directory):

```json
{
  "GraphConfig": {
    "ClientId": "your-app-registration-client-id",
    "ClientSecret": "your-app-registration-client-secret",
    "TenantId": "your-tenant-id"
  },
  "WebAuthConfig": {
    "ClientId": "your-app-registration-client-id",
    "ClientSecret": "your-app-registration-client-secret",
    "TenantId": "your-tenant-id",
    "Authority": "https://login.microsoftonline.com/organizations"
  },
  "ConnectionStrings": {
    "Storage": "UseDevelopmentStorage=true"
  },
  "MicrosoftAppId": "your-bot-app-id",
  "MicrosoftAppPassword": "your-bot-app-password",
  "DevMode": true
}
```

**Note**: The application uses a simplified configuration structure. Only `GraphConfig`, `ConnectionStrings:Storage`, and bot credentials are required. The `WebAuthConfig` section is not needed as authentication is handled through Microsoft Graph API and Teams bot framework.

### 3. Frontend Configuration

Create `Web/web.client/.env.local`:

```env
VITE_MSAL_CLIENT_ID=your-app-registration-client-id
VITE_MSAL_AUTHORITY=https://login.microsoftonline.com/your-tenant-id
VITE_MSAL_SCOPES=api://your-app-registration-client-id/access_as_user
VITE_TEAMSFX_START_LOGIN_PAGE_URL=https://your-domain.com/auth-start.html
```

### 4. Production Configuration

For production deployments to Azure App Service:

1. **Azure App Service**: Configure application settings in the Azure Portal under Configuration ? Application Settings
2. **Azure Key Vault**: For enhanced security, store secrets in Azure Key Vault and reference them in your application settings:
   ```
   @Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/StorageConnectionString/)
   ```
3. **Managed Identity**: Use system-assigned or user-assigned managed identities to access Azure resources without storing credentials

## Installation & Setup

### 1. Clone the Repository

```bash
git clone https://github.com/sambetts/office-nudge.git
cd office-nudge/src/Full
```

### 2. Restore Backend Dependencies

```bash
dotnet restore
```

### 3. Install Frontend Dependencies

```bash
cd Web/web.client
npm install
cd ../..
```

### 4. Build the Solution

```bash
dotnet build
```

### 5. Build the Frontend

```bash
cd Web/web.client
npm run build
cd ../..
```

## Running the Application

### Development Mode

**Backend:**
```bash
cd Web/Web.Server
dotnet run
```

The API will be available at `https://localhost:5001` (or configured port)

**Frontend:**
```bash
cd Web/web.client
npm run dev
```

The React app will be available at `http://localhost:5173`

### Production Mode

Build and run the backend (frontend is served from backend):

```bash
cd Web/Web.Server
dotnet publish -c Release -o ./publish
cd publish
dotnet Web.Server.dll
```

## Usage

### Managing Adaptive Card Templates

1. **Access the Application**: Navigate to the web application and authenticate
2. **Navigate to Templates**: Go to the "Message Templates" page
3. **Create a Template**:
   - Click "New Template"
   - Enter a template name
   - Paste your adaptive card JSON payload
   - Click "Create"

**Example Adaptive Card JSON:**

```json
{
  "type": "AdaptiveCard",
  "version": "1.3",
  "body": [
    {
      "type": "TextBlock",
      "text": "Welcome Message",
      "size": "Large",
      "weight": "Bolder"
    },
    {
      "type": "TextBlock",
      "text": "This is a sample adaptive card that can be sent to Teams users.",
      "wrap": true
    },
    {
      "type": "ActionSet",
      "actions": [
        {
          "type": "Action.OpenUrl",
          "title": "Learn More",
          "url": "https://adaptivecards.io/"
        }
      ]
    }
  ]
}
```

4. **Edit/Delete Templates**: Use the action buttons in the templates table
5. **View Template**: Click the eye icon to preview the JSON

### Sending Messages to Users

Messages can be sent via the Teams bot using the stored templates. The bot will:
1. Retrieve the template metadata from Azure Table Storage
2. Download the JSON payload from Azure Blob Storage
3. Parse the adaptive card JSON
4. Send the card to specified users or channels
5. Log the delivery status

**Storage Details:**
- Template metadata (name, creator, dates) is stored in Azure Table Storage for fast queries
- JSON payloads are stored in Azure Blob Storage (`message-templates` container) to handle large adaptive cards
- Each template's blob is named `{templateId}.json`
- Table storage references the blob URL for retrieval

### Viewing Message Logs

Navigate to the message logs section to view:
- When messages were sent
- Which template was used
- Recipient information
- Delivery status (Sent, Failed, Pending)

## API Endpoints

### Message Template Management

- `GET /api/MessageTemplate/GetAll` - Get all templates
- `GET /api/MessageTemplate/Get/{id}` - Get specific template
- `GET /api/MessageTemplate/GetJson/{id}` - Get template JSON payload
- `POST /api/MessageTemplate/Create` - Create new template
- `PUT /api/MessageTemplate/Update/{id}` - Update template
- `DELETE /api/MessageTemplate/Delete/{id}` - Delete template

### Message Logging

- `POST /api/MessageTemplate/LogSend` - Log a message send event
- `GET /api/MessageTemplate/GetLogs` - Get all message logs
- `GET /api/MessageTemplate/GetLogsByTemplate/{templateId}` - Get logs for specific template

## Deployment

### Azure App Service

1. **Create an Azure App Service** (Windows or Linux, .NET 10)
2. **Configure Application Settings** in Azure Portal (Configuration ? Application Settings):
   - Add all the settings from the user secrets structure above
   - Use Azure Key Vault references for sensitive values (recommended)
3. **Deploy using Azure CLI**:

```bash
# Build and publish
dotnet publish Web/Web.Server/Web.Server.csproj -c Release -o ./publish

# Deploy to Azure App Service (replace with your app name)
az webapp deploy --resource-group myResourceGroup --name myAppName --src-path ./publish
```

Or deploy using Visual Studio:
- Right-click on `Web.Server` project ? Publish

### Azure Static Web Apps (Frontend Only)

If separating frontend and backend:

```bash
cd Web/web.client
npm run build
# Deploy the dist folder to Azure Static Web Apps
```

### Using Azure Key Vault (Recommended for Production)

1. Create an Azure Key Vault
2. Add secrets to Key Vault:
   - `GraphClientSecret`
   - `StorageConnectionString`
   - `BotAppPassword`
   - `ApplicationInsightsConnectionString`
3. Enable Managed Identity on your App Service
4. Grant the managed identity access to Key Vault (Access Policies or RBAC)
5. Reference secrets in Application Settings:
   ```
   GraphConfig__ClientSecret=@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/GraphClientSecret/)
   ConnectionStrings__Storage=@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/StorageConnectionString/)
   MicrosoftAppPassword=@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/BotAppPassword/)
   ```

## Security Considerations

- **Never commit secrets to source control** - Use user secrets for development, Key Vault for production
- **Use Azure Key Vault** to store all sensitive configuration values in production
- **Enable Managed Identity** for Azure resources to avoid storing credentials
- **Implement proper RBAC** for template management and Azure resource access
- **Validate all adaptive card JSON** before storage to prevent injection attacks
- **Use HTTPS only** for all communications
- **Regularly rotate secrets** - Client secrets, storage keys, and bot passwords
- **Enable Azure Storage firewall** rules to restrict access to trusted networks
- **Use SAS tokens** with limited permissions and expiration times when possible
- **Monitor and audit** access logs using Application Insights and Azure Monitor
- **Implement rate limiting** on API endpoints to prevent abuse

### Security Best Practices

1. **Development Environment**:
   - Use `dotnet user-secrets` for local development
   - Never use production credentials locally
   - Keep `.env.local` files out of version control (already in .gitignore)

2. **Production Environment**:
   - Store all secrets in Azure Key Vault
   - Use managed identities for Azure service authentication
   - Enable Azure AD authentication for storage accounts
   - Configure network security groups and firewall rules
   - Enable logging and monitoring for all resources

3. **Application Security**:
   - Validate and sanitize all user inputs
   - Implement proper authorization checks in controllers
   - Use CORS policies to restrict frontend access
   - Keep all NuGet packages and npm dependencies updated

## Troubleshooting

### Common Issues

**Authentication Errors:**
- Verify Azure AD app registration configuration
- Check redirect URIs match your deployment
- Ensure API permissions are granted (admin consent)

**Graph API Permission Errors:**
If you see errors like "Current authenticated context is not valid for this request" or "Insufficient privileges":
1. **Verify Application Permissions** in Azure Portal:
   - Go to Azure Active Directory ? App registrations ? Your app
   - Click **API permissions**
   - Ensure all required permissions are listed:
     - `User.Read.All` (Application)
     - `TeamsActivity.Send` (Application)
     - `TeamsAppInstallation.ReadWriteForUser.All` (Application)
2. **Grant Admin Consent**:
   - Click **Grant admin consent for [Your Tenant]**
   - Wait a few minutes for changes to propagate
3. **Verify Tenant ID**:
   - Ensure your `TenantId` in configuration doesn't have extra characters
   - Check `dotnet user-secrets list` to verify the value
4. **Test Graph Connection**:
   - Navigate to `/api/Diagnostics/TestGraphConnection` to verify connectivity
   - Check logs for detailed error messages

**Storage Connection Errors:**
- Verify storage account connection string
- Check storage account firewall rules
- Ensure storage account has both Table and Blob services enabled
- Verify the `message-templates` blob container exists or can be created

**Bot Not Responding:**
- Verify bot app registration and credentials
- Check bot is properly installed in Teams
- Ensure `TeamsAppInstallation.ReadWriteForUser.All` permission is granted
- Review Application Insights logs
