# Local PostgreSQL panel for FeriaAcademica (Windows).
# Checks that PostgreSQL is installed (offers to install it with winget), and gives a menu
# to start/stop it, inspect tables, copy the connection string and update dotnet user-secrets.

$ErrorActionPreference = 'Stop'

$Db       = 'feria_academica'
$PgHost   = 'localhost'
$Port     = 5432
$PgUser   = 'postgres'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$ApiProj  = Join-Path $RepoRoot 'src\FeriaAcademica.Api'
$script:Password = $null

# ---------- helpers ----------

function Read-Secret([string]$prompt) {
    $secure = Read-Host $prompt -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

function Get-Password {
    if (-not $script:Password) { $script:Password = Read-Secret "PostgreSQL password for user '$PgUser'" }
    return $script:Password
}

function Get-PgBin {
    $cmd = Get-Command psql.exe -ErrorAction SilentlyContinue
    if ($cmd) { return Split-Path $cmd.Source }
    $dirs = Get-ChildItem "$env:ProgramFiles\PostgreSQL" -Directory -ErrorAction SilentlyContinue |
            Sort-Object { [int]($_.Name -replace '\D', '') } -Descending
    foreach ($d in $dirs) {
        $bin = Join-Path $d.FullName 'bin'
        if (Test-Path (Join-Path $bin 'psql.exe')) { return $bin }
    }
    return $null
}

function Get-PgService {
    Get-Service -Name 'postgresql*' -ErrorAction SilentlyContinue | Select-Object -First 1
}

function Test-PgRunning {
    $bin = Get-PgBin
    if (-not $bin) { return $false }
    & (Join-Path $bin 'pg_isready.exe') -h $PgHost -p $Port -q | Out-Null
    return ($LASTEXITCODE -eq 0)
}

function Invoke-Admin([string]$command) {
    $principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Invoke-Expression $command
    } else {
        Write-Host 'Windows will ask for administrator permission...'
        Start-Process powershell -Verb RunAs -Wait -ArgumentList '-NoProfile', '-Command', $command
    }
}

function Invoke-Psql([string[]]$psqlArgs) {
    $bin = Get-PgBin
    $env:PGPASSWORD = Get-Password
    try { & (Join-Path $bin 'psql.exe') -h $PgHost -p $Port -U $PgUser @psqlArgs }
    finally { Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue }
}

function Get-ConnectionString([bool]$mask) {
    $pw = if ($mask) { '********' } elseif ($script:Password) { $script:Password } else { Get-Password }
    return "Host=$PgHost;Port=$Port;Database=$Db;Username=$PgUser;Password=$pw"
}

function Assert-Installed {
    if (-not (Get-PgBin)) {
        Write-Host 'PostgreSQL is not installed. Use option [8] to install it.' -ForegroundColor Yellow
        return $false
    }
    return $true
}

function Assert-Running {
    if (-not (Assert-Installed)) { return $false }
    if (-not (Test-PgRunning)) {
        Write-Host 'PostgreSQL is stopped. Use option [1] to start it.' -ForegroundColor Yellow
        return $false
    }
    return $true
}

# ---------- actions ----------

function Install-Postgres {
    if (Get-PgBin) { Write-Host 'PostgreSQL is already installed.'; return }
    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
        Write-Host 'winget was not found. Download PostgreSQL from https://www.postgresql.org/download/windows/' -ForegroundColor Yellow
        return
    }
    Write-Host 'This installs PostgreSQL 17 (about 300 MB) and creates the user "postgres".'
    Write-Host 'Use only letters and numbers in the password.'
    $pw = Read-Secret 'Choose a password for the postgres user'
    if (-not $pw) { Write-Host 'Cancelled: the password cannot be empty.'; return }
    $override = "--mode unattended --unattendedmodeui none --superpassword $pw --serverport $Port"
    winget install -e --id PostgreSQL.PostgreSQL.17 --accept-package-agreements --accept-source-agreements --override $override
    if (Get-PgBin) {
        $script:Password = $pw
        Write-Host 'PostgreSQL installed. Use option [6] to save the connection string in dotnet user-secrets.' -ForegroundColor Green
    } else {
        Write-Host 'The installation did not finish. Run this script again or install it manually.' -ForegroundColor Yellow
    }
}

function Start-Pg {
    if (-not (Assert-Installed)) { return }
    if (Test-PgRunning) { Write-Host 'PostgreSQL is already running.'; return }
    $svc = Get-PgService
    if (-not $svc) { Write-Host 'PostgreSQL service not found.' -ForegroundColor Yellow; return }
    Invoke-Admin "Start-Service -Name '$($svc.Name)'"
    Start-Sleep -Seconds 3
    if (Test-PgRunning) { Write-Host 'PostgreSQL is running.' -ForegroundColor Green } else { Write-Host 'It did not start.' -ForegroundColor Yellow }
}

function Stop-Pg {
    if (-not (Assert-Installed)) { return }
    $svc = Get-PgService
    if (-not $svc) { Write-Host 'PostgreSQL service not found.' -ForegroundColor Yellow; return }
    Invoke-Admin "Stop-Service -Name '$($svc.Name)' -Force"
    Start-Sleep -Seconds 2
    if (Test-PgRunning) { Write-Host 'It is still running.' -ForegroundColor Yellow } else { Write-Host 'PostgreSQL stopped.' }
}

function Show-Tables {
    if (-not (Assert-Running)) { return }
    Invoke-Psql @('-d', $Db, '-c', '\dt')
}

function Open-Psql {
    if (-not (Assert-Running)) { return }
    Invoke-Psql @('-d', $Db)
}

function Copy-ConnectionString {
    if (-not (Assert-Installed)) { return }
    Set-Clipboard -Value (Get-ConnectionString $false)
    Write-Host 'Connection string copied to the clipboard.' -ForegroundColor Green
}

function Update-UserSecrets {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Host 'dotnet was not found. Install the .NET SDK 10 first.' -ForegroundColor Yellow
        return
    }
    if (-not (Test-Path $ApiProj)) { Write-Host "Project not found: $ApiProj" -ForegroundColor Yellow; return }
    $new = Read-Secret "New password for '$PgUser' (press Enter to use the current one)"
    if ($new) { $script:Password = $new }
    $cs = Get-ConnectionString $false
    dotnet user-secrets set 'ConnectionStrings:Default' $cs --project $ApiProj
    if ($LASTEXITCODE -eq 0) {
        Write-Host 'dotnet user-secrets updated.' -ForegroundColor Green
        Write-Host (Get-ConnectionString $true)
    }
}

function Reset-Database {
    if (-not (Assert-Running)) { return }
    $ok = Read-Host "This deletes ALL data in $Db. Type YES to confirm"
    if ($ok -ceq 'YES') {
        Invoke-Psql @('-d', 'postgres', '-c', "DROP DATABASE IF EXISTS $Db WITH (FORCE);")
        Write-Host 'Database deleted. It is recreated with migrations the next time you run the API.'
    } else {
        Write-Host 'Cancelled.'
    }
}

# ---------- first run check ----------

if (-not (Get-PgBin)) {
    Write-Host 'PostgreSQL was not found on this computer.' -ForegroundColor Yellow
    $answer = Read-Host 'Install it now with winget? (y/n)'
    if ($answer -match '^[yYsS]') { Install-Postgres }
}

# ---------- menu ----------

while ($true) {
    Clear-Host
    $installed = [bool](Get-PgBin)
    $status = if (-not $installed) { 'NOT INSTALLED' } elseif (Test-PgRunning) { 'RUNNING' } else { 'STOPPED' }

    Write-Host '=============================================='
    Write-Host '  FeriaAcademica - PostgreSQL local'
    Write-Host '=============================================='
    Write-Host "  Status: $status"
    Write-Host ''
    Write-Host '  Connection string:'
    Write-Host "  $(Get-ConnectionString $true)"
    Write-Host ''
    Write-Host '  [1] Start PostgreSQL         Start-Service postgresql*'
    Write-Host '  [2] Stop PostgreSQL          Stop-Service postgresql*'
    Write-Host "  [3] List tables              psql -d $Db -c '\dt'"
    Write-Host "  [4] Open psql (interactive)  psql -d $Db"
    Write-Host '  [5] Copy connection string'
    Write-Host '  [6] Update dotnet user-secrets'
    Write-Host '  [7] Delete and recreate the database (resets all data)'
    Write-Host '  [8] Install PostgreSQL (winget)'
    Write-Host '  [q] Quit'
    Write-Host ''
    $opt = Read-Host '  Choose an option'
    Write-Host ''

    try {
        switch ($opt) {
            '1' { Start-Pg }
            '2' { Stop-Pg }
            '3' { Show-Tables }
            '4' { Open-Psql }
            '5' { Copy-ConnectionString }
            '6' { Update-UserSecrets }
            '7' { Reset-Database }
            '8' { Install-Postgres }
            { $_ -in 'q', 'Q' } { exit 0 }
            default { Write-Host 'Invalid option.' }
        }
    } catch {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }

    Write-Host ''
    Read-Host '  Press Enter to go back to the menu' | Out-Null
}
