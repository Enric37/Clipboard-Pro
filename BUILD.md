# Build

Requisito: SDK .NET 8 para Windows.

```powershell
dotnet restore ClipboardPro.sln
dotnet build ClipboardPro.sln -c Release
dotnet publish src/ClipboardPro/ClipboardPro.csproj -c Release -r win-x64 --self-contained false
```

Para generar el instalador per-user se necesita Inno Setup 6 y se ejecuta `./build.ps1 -Installer`. El instalador ofrece accesos directos, inicio opcional con Windows y pregunta si se deben conservar los datos al desinstalar.
