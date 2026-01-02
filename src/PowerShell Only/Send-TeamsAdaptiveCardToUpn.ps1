
<# 
.SYNOPSIS
    Sends an Adaptive Card to a specific user's UPN in Microsoft Teams via Microsoft Graph (app-only).

.PARAMETER TenantId
    Entra ID tenant ID (GUID).

.PARAMETER ClientId
    App registration (client) ID (GUID).

.PARAMETER ClientSecret
    App client secret (use a secure method in production; e.g., Azure Key Vault).

.PARAMETER TargetUpn
    Target user's UPN/email (e.g., sam.betts@contoso.com).

.PARAMETER AdaptiveCardJsonPath
    Path to a JSON file containing the Adaptive Card payload (schema 1.5 or below).

.PARAMETER CachePath
    Optional path to a local JSON file to cache userId→chatId mappings to avoid duplicate chats.

.PARAMETER UseDefaultCard
    Switch to send a simple default card if no JSON file is provided.

.NOTES
    Required Graph application permissions: Chat.Create, Chat.ReadWrite, ChatMessage.Send (admin consent required).
    Tested with Microsoft.Graph PowerShell SDK (v1.x) using v1.0 profile.
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$TenantId,

    [Parameter(Mandatory=$true)]
    [string]$ClientId,

    [Parameter(Mandatory=$true)]
    [string]$ClientSecret,

    [Parameter(Mandatory=$true)]
    [string]$TargetUpn,

    [Parameter(Mandatory=$false)]
    [string]$AdaptiveCardJsonPath,

    [Parameter(Mandatory=$false)]
    [string]$CachePath = ".\chatCache.json",

    [switch]$UseDefaultCard
)

function Ensure-GraphModule {
    if (-not (Get-Module -ListAvailable -Name Microsoft.Graph)) {
        Write-Host "Installing Microsoft.Graph module..." -ForegroundColor Cyan
        Install-Module Microsoft.Graph -Scope CurrentUser -Force -ErrorAction Stop
    }
    Import-Module Microsoft.Graph -ErrorAction Stop
    Select-MgProfile -Name "v1.0"
}

function Connect-AppOnly {
    param(
        [string]$TenantId,
        [string]$ClientId,
        [string]$ClientSecret
    )
    $secureSecret = ConvertTo-SecureString $ClientSecret -AsPlainText -Force
    Write-Host "Connecting to Microsoft Graph (app-only)..." -ForegroundColor Cyan
    Connect-MgGraph -TenantId $TenantId -ClientId $ClientId -ClientSecret $secureSecret -NoWelcome -ErrorAction Stop
}

function Get-UserByUpn {
    param([string]$Upn)
    # Get-MgUser supports UPN directly
    try {
        $user = Get-MgUser -UserId $Upn -ErrorAction Stop
        return $user
    } catch {
        throw "User not found or inaccessible for UPN '$Upn'. Ensure the UPN is correct and app permissions are consented. Details: $($_.Exception.Message)"
    }
}

function Load-Cache {
    param([string]$Path)
    if (Test-Path -LiteralPath $Path) {
        try {
            return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
        } catch {
            Write-Warning "Failed to read cache at '$Path'. Starting with an empty cache."
            return @{}
        }
    }
    return @{}
}

function Save-Cache {
    param(
        [hashtable]$Cache,
        [string]$Path
    )
    ($Cache | ConvertTo-Json -Depth 6) | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-OrCreateOneOnOneChat {
    param(
        [string]$UserId,
        [hashtable]$Cache,
        [string]$CachePath
    )

    if ($Cache.ContainsKey($UserId)) {
        Write-Host "Using cached chatId: $($Cache[$UserId])" -ForegroundColor DarkGreen
        return $Cache[$UserId]
    }

    Write-Host "Creating one-on-one chat with userId $UserId..." -ForegroundColor Cyan
    # For oneOnOne: include the target user; the app is the other participant implicitly.
    $member = @{
        "@odata.type"    = "#microsoft.graph.aadUserConversationMember"
        roles            = @("owner")
        "user@odata.bind"= "https://graph.microsoft.com/v1.0/users('$UserId')"
    }

    try {
        $chat = New-MgChat -ChatType oneOnOne -Members @($member) -ErrorAction Stop
        $chatId = $chat.Id
        Write-Host "Created chatId: $chatId" -ForegroundColor Green
        $Cache[$UserId] = $chatId
        Save-Cache -Cache $Cache -Path $CachePath
        return $chatId
    } catch {
        throw "Failed to create one-on-one chat. Confirm Chat.Create and Chat.ReadWrite app permissions have admin consent. Details: $($_.Exception.Message)"
    }
}

function Get-CardContent {
    param(
        [string]$AdaptiveCardJsonPath,
        [switch]$UseDefaultCard
    )

    if ($AdaptiveCardJsonPath) {
        if (-not (Test-Path -LiteralPath $AdaptiveCardJsonPath)) {
            throw "AdaptiveCardJsonPath '$AdaptiveCardJsonPath' does not exist."
        }
        try {
            $jsonRaw = Get-Content -LiteralPath $AdaptiveCardJsonPath -Raw
            $cardObj = $jsonRaw | ConvertFrom-Json
            return $cardObj
        } catch {
            throw "Failed to parse Adaptive Card JSON file. Details: $($_.Exception.Message)"
        }
    }

    if ($UseDefaultCard) {
        # Simple, valid Adaptive Card (1.5) as default
        $defaultCard = @{
            type    = "AdaptiveCard"
            version = "1.5"
            body    = @(
                @{
                    type  = "TextBlock"
                    text  = "Notification"
                    size  = "Medium"
                    weight= "Bolder"
                },
                @{
                    type  = "TextBlock"
                    text  = "Your job completed successfully."
                    wrap  = $true
                },
                @{
                    type  = "FactSet"
                    facts = @(
                        @{ title = "Finished at"; value = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss 'UTC'K") }
                        @{ title = "Triggered by"; value = "Automation Service" }
                    )
                }
            ),
            actions = @(
                @{
                    type = "Action.OpenUrl"
                    title= "View details"
                    url  = "https://teams.microsoft.com/"
                }
            )
        }
        return $defaultCard
    }

    throw "No Adaptive Card provided. Supply -AdaptiveCardJsonPath or -UseDefaultCard."
}

function Send-AdaptiveCardToChat {
    param(
        [string]$ChatId,
        [object]$CardContent
    )

    # Minimal message body (required; card goes as attachment)
    $body = @{
        ContentType = "html"
        Content     = " "
    }

    # Attachment: Adaptive Card
    $attachment = @{
        id          = "1"
        contentType = "application/vnd.microsoft.card.adaptive"
        content     = $CardContent
        name        = "card"
    }

    try {
        Write-Host "Sending Adaptive Card to chatId $ChatId..." -ForegroundColor Cyan
        New-MgChatMessage -ChatId $ChatId -Body $body -Attachments @($attachment) -ErrorAction Stop | Out-Null
        Write-Host "Card sent successfully." -ForegroundColor Green
    } catch {
        throw "Failed to send message. Confirm ChatMessage.Send permission and consent. Details: $($_.Exception.Message)"
    }
}

# --- Main ---
try {
    Ensure-GraphModule
    Connect-AppOnly -TenantId $TenantId -ClientId $ClientId -ClientSecret $ClientSecret

    # Optional: show current profile and auth type
    $ctx = Get-MgContext
    Write-Host "Graph profile: $($ctx.Profile), AuthType: $($ctx.AuthType)" -ForegroundColor DarkGray

    $user   = Get-UserByUpn -Upn $TargetUpn
    $cache  = Load-Cache -Path $CachePath
    $chatId = Get-OrCreateOneOnOneChat -UserId $user.Id -Cache $cache -CachePath $CachePath

    $card   = Get-CardContent -AdaptiveCardJsonPath $AdaptiveCardJsonPath -UseDefaultCard:$UseDefaultCard
    Send-AdaptiveCardToChat -ChatId $chatId -CardContent $card
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
