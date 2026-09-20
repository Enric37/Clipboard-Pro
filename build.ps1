param([switch]$Installer)
$ErrorActionPreference = 'Stop'
dotnet restore ClipboardPro.sln
dotnet publish src/ClipboardPro/ClipboardPro.csproj -c Release -r win-x64 --self-contained false -o src/ClipboardPro/bin/Release/net8.0-windows/win-x64/publish
if ($Installer) {
  $innoCandidates = @(
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe',
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
  )
  $inno = $innoCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
  if (-not $inno) { throw 'Inno Setup 6 no está instalado. Instálalo y vuelve a ejecutar .\build.ps1 -Installer.' }
  & $inno installer/ClipboardPro.iss
}
