# Update Common.Engine namespace references

Write-Host "Updating GraphUserService.cs..."
Remove-Item "Common.Engine\Services\GraphUserService.cs" -Force
Move-Item "Common.Engine\Services\GraphUserService.UPDATED.cs" "Common.Engine\Services\GraphUserService.cs" -Force

Write-Host "Removing old files..."
Remove-Item "Common.Engine\Services\EnrichedUserInfo.cs" -ErrorAction SilentlyContinue
Remove-Item "Common.Engine\Services\UserCache\UserCacheConfig.cs" -ErrorAction SilentlyContinue
Remove-Item "Common.Engine\Services\UserCache\UserCacheModels.cs" -ErrorAction SilentlyContinue
Remove-Item "Common.Engine\CachedUserAndConversationData.cs" -ErrorAction SilentlyContinue

Write-Host "Updating namespace references in files..."

# Update all files that reference the moved classes
$filesToUpdate = @(
    "Common.Engine\Services\UserCache\IGraphUserCacheManager.cs",
    "Common.Engine\Services\UserCache\GraphUserCacheManager.cs",
    "Common.Engine\Services\UserCache\InMemoryUserCacheManager.cs",
    "Common.Engine\Services\UserCache\DeltaQueryService.cs",
    "Common.Engine\Services\UserCache\UserCacheStorageService.cs",
    "Common.Engine\Services\UserCache\CopilotStatsService.cs",
    "Common.Engine\BotConversationCache.cs",
    "Common.Engine\BotUserUtils.cs",
    "Common.Engine\Services\SmartGroupService.cs"
)

foreach ($file in $filesToUpdate) {
    if (Test-Path $file) {
        Write-Host "Processing $file..."
        $content = Get-Content $file -Raw
        
        # Add using statements if not present
        if ($content -notmatch "using Common.Engine.Models;") {
            $content = $content -replace "(namespace [^;]+;)", "using Common.Engine.Models;`n`n`$1"
        }
        if ($content -notmatch "using Common.Engine.Config;" -and $file -match "UserCache") {
            $content = $content -replace "(namespace [^;]+;)", "using Common.Engine.Config;`n`n`$1"
        }
        
        Set-Content $file -Value $content
    }
}

Write-Host "Done!"
