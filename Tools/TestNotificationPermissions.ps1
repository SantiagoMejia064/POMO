param([string]$EditorDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $EditorDirectory) {
    $version = (Select-String -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value.Trim()
    $EditorDirectory = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor"
}
$dotnet = Join-Path $EditorDirectory 'Data/NetCoreRuntime/dotnet.exe'
$compiler = Join-Path $EditorDirectory 'Data/DotNetSdkRoslyn/csc.dll'
$runtimeConfig = Join-Path $EditorDirectory 'Data/DotNetSdkRoslyn/csc.runtimeconfig.json'
$frameworkVersion = (Get-Content -LiteralPath $runtimeConfig -Raw | ConvertFrom-Json).runtimeOptions.framework.version
$framework = Join-Path $EditorDirectory "Data/NetCoreRuntime/shared/Microsoft.NETCore.App/$frameworkVersion"
$output = Join-Path $projectRoot 'Logs/Sprint4'
[IO.Directory]::CreateDirectory($output) | Out-Null
$arguments = @('/nostdlib+', '/target:exe', '/langversion:9.0', '/define:UNITY_ANDROID', ('/out:"' + (Join-Path $output 'NotificationPermissionChecks.dll') + '"'))
Get-ChildItem -LiteralPath $framework -Filter *.dll | Where-Object {
    $_.Name -match '^(System\.|mscorlib\.dll|netstandard\.dll)' -and $_.Name -notlike '*Native*'
} | ForEach-Object { $arguments += '/reference:"' + $_.FullName + '"' }
$sources = @(
    'Tools/Tests/NotificationPermissionChecks.cs',
    'Tools/Tests/TaskDateChecks.cs',
    'Assets/Scripts/ServicesAdapters/Notifications/NotificationService.cs',
    'Assets/Scripts/Business/Tasks/TaskService.cs',
    'Assets/Scripts/Data/Repositories/TaskRepository.cs',
    'Assets/Scripts/Data/Persistence/JsonStorageService.cs',
    'Assets/Scripts/Data/DTOs/TaskDTO.cs',
    'Assets/Scripts/Data/Models/TaskModel.cs',
    'Assets/Scripts/Data/Models/RhythmReminderModel.cs',
    'Assets/Scripts/Shared/Utilities/DateUtils.cs',
    'Assets/Scripts/Shared/Constants/AppConstants.cs',
    'Assets/Scripts/Shared/Enums/TaskStatus.cs',
    'Assets/Scripts/Shared/Enums/TaskValidationResult.cs',
    'Assets/Scripts/Shared/Enums/TaskStatusTransitionResult.cs'
)
foreach ($source in $sources) { $arguments += '"' + (Join-Path $projectRoot $source) + '"' }
$responsePath = Join-Path $output 'permission-tests.rsp'
[IO.File]::WriteAllLines($responsePath, $arguments)
Copy-Item -LiteralPath $runtimeConfig -Destination (Join-Path $output 'NotificationPermissionChecks.runtimeconfig.json')
Push-Location -LiteralPath $projectRoot
try {
    & $dotnet $compiler ('@' + $responsePath)
    if ($LASTEXITCODE -ne 0) { throw 'No compilaron las comprobaciones de permisos.' }
    & $dotnet (Join-Path $output 'NotificationPermissionChecks.dll')
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las comprobaciones de permisos.' }
}
finally { Pop-Location }
