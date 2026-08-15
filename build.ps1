# Byte-order mark for UTF-8
$OutputEncoding = [System.Text.Encoding]::UTF8

<#
.SYNOPSIS
    KeyboardFixer Build Script
.DESCRIPTION
    Compiles KeyboardFixer.cs into a standalone Windows executable (.exe)
    using the built-in .NET Framework compiler — no Visual Studio needed.
.EXAMPLE
    .\build.ps1
    .\build.ps1 -Install
#>

param(
    [switch]$Install  # Pass -Install to also register it at Windows startup
)

$ErrorActionPreference = "Stop"

# ── Paths ────────────────────────────────────────────────────────────────────
$SrcFile  = Join-Path $PSScriptRoot "src\KeyboardFixer.cs"
$OutDir   = Join-Path $PSScriptRoot "bin"
$OutExe   = Join-Path $OutDir       "KeyboardFixer.exe"
$Compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

# ── Pre-checks ───────────────────────────────────────────────────────────────
if (-not (Test-Path $Compiler)) {
    Write-Error "❌ .NET Framework compiler not found at: $Compiler`nPlease install .NET Framework 4.x"
    exit 1
}
if (-not (Test-Path $SrcFile)) {
    Write-Error "❌ Source file not found: $SrcFile"
    exit 1
}

# ── Build ────────────────────────────────────────────────────────────────────
Write-Host "`n🔨 Building KeyboardFixer..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$args = @(
    "/target:winexe",
    "/optimize+",
    "/out:`"$OutExe`"",
    "`"$SrcFile`"",
    "/r:System.Windows.Forms.dll",
    "/r:System.Drawing.dll",
    "/r:System.dll"
)

& $Compiler @args
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Build failed. See errors above."
    exit 1
}

Write-Host "✅ Build successful  →  $OutExe" -ForegroundColor Green

# ── Optional: Install (startup + Desktop shortcut) ───────────────────────────
if ($Install) {
    Write-Host "`n📦 Installing..." -ForegroundColor Cyan

    # Copy to a permanent location
    $InstallDir = Join-Path $env:LOCALAPPDATA "KeyboardFixer"
    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
    Copy-Item $OutExe (Join-Path $InstallDir "KeyboardFixer.exe") -Force

    $InstalledExe = Join-Path $InstallDir "KeyboardFixer.exe"

    # Register in HKCU Run (auto-start with Windows)
    $RegPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    Set-ItemProperty -Path $RegPath -Name "KeyboardFixer" -Value "`"$InstalledExe`""

    # Create Desktop shortcut
    $WshShell  = New-Object -ComObject WScript.Shell
    $Shortcut  = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\KeyboardFixer.lnk")
    $Shortcut.TargetPath  = $InstalledExe
    $Shortcut.Description = "Arabic / English Keyboard Layout Fixer"
    $Shortcut.Save()

    Write-Host "✅ Installed to: $InstalledExe"    -ForegroundColor Green
    Write-Host "✅ Auto-start registered in Registry" -ForegroundColor Green
    Write-Host "✅ Desktop shortcut created"           -ForegroundColor Green
    Write-Host "`n🚀 Launching KeyboardFixer..." -ForegroundColor Yellow
    Start-Process $InstalledExe
}

Write-Host "`nDone! Press any key to close..." -ForegroundColor DarkGray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
