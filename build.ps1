param([switch]$Installer)
$ErrorActionPreference = 'Stop'
dotnet restore ClipboardPro.sln
dotnet publish src/ClipboardPro/ClipboardPro.csproj -c Release -r win-x64 --self-contained false -o src/ClipboardPro/bin/Release/net8.0-windows/win-x64/publish
if ($Installer) { & 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' installer/ClipboardPro.iss }
