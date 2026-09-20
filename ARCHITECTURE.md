# Arquitectura

`ClipboardCaptureService` es el único consumidor de notificaciones de Windows. Crea una ventana de mensajes invisible y usa `AddClipboardFormatListener`; por tanto el proceso duerme hasta que Windows entrega un evento. Las lecturas usan un máximo de tres reintentos cortos porque el portapapeles puede estar bloqueado temporalmente por otra aplicación.

`ClipDatabase` mantiene conexiones SQLite breves en WAL. La tabla `clips` y la tabla FTS5 se mantienen mediante triggers. Las búsquedas se ejecutan fuera del hilo de UI; el panel sólo recibe lotes de 100 elementos y WPF recicla los contenedores visibles.

Las imágenes se codifican una vez en disco, junto a una miniatura; no se dejan bitmaps decodificados en memoria. Los archivos se representan exclusivamente por rutas, y se comprueba su disponibilidad al mostrarlos.

Los servicios de proceso son: `ClipboardCaptureService`, `HotkeyService`, `SettingsService`, `ClipDatabase` y el icono de bandeja. No se crean workers ni temporizadores periódicos. La limpieza se realiza durante el inicio y no cada minuto.

Toda identidad y ruta de producto está en `Branding.cs`. Las opciones serializadas están en `Models/AppSettings.cs`.
