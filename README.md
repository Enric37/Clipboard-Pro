# Clipboard Pro

Aplicación de escritorio nativa para Windows 10/11 que guarda localmente el historial del portapapeles. No hay cuentas, servidor, telemetría de clips ni navegador embebido.

## Ejecutar

```powershell
dotnet run --project src/ClipboardPro/ClipboardPro.csproj
```

El atajo predeterminado es `Ctrl+Shift+V`; `Win+V` se deja intacto para Windows. Al cerrar el panel la aplicación continúa en la bandeja. Usa **Salir** en la bandeja para terminarla por completo.

## Compilar y empaquetar

```powershell
.\build.ps1
.\build.ps1 -Installer
```

El segundo comando requiere Inno Setup 6. Los datos residen en `%LOCALAPPDATA%\Clipboard Pro\ClipboardPro`, no junto al ejecutable.

## Capacidades incluidas

- Listener de portapapeles basado en `AddClipboardFormatListener`: no hay polling cuando está inactiva.
- Texto, HTML, RTF, enlaces, código y colores; imágenes persistidas como PNG + thumbnail; archivos/carpetas por rutas y metadatos, sin copiar sus bytes.
- SQLite con WAL, FTS5, índices, deduplicación configurable, favoritos y clips fijados.
- Búsqueda con cancelación/debounce, lista virtualizada y paginación interna.
- Pegar con restauración del foco, bandeja, arranque con Windows, pausa, exclusiones y heurística local de contenido sensible.

Consulta [ARCHITECTURE.md](ARCHITECTURE.md), [BUILD.md](BUILD.md) y [RELEASE.md](RELEASE.md) para los detalles operativos.
