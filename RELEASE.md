# Checklist de release

1. Ejecutar `dotnet build ClipboardPro.sln -c Release` sin errores.
2. Probar el listener con texto, imagen y varios archivos; comprobar que los contenidos copiados desde una aplicación excluida no aparecen.
3. Comprobar `Ctrl+Shift+V`, Escape, navegación con flechas, Enter y restauración del foco.
4. Probar escala 100/125/150/200 %, suspensión/reanudación y reinicio de Explorer (el tray debe recrearse en la siguiente versión de mantenimiento).
5. Ejecutar el instalador y desinstalador en una cuenta sin privilegios de administrador.
6. Firmar tanto el ejecutable como el instalador antes de distribuirlos y publicar los hashes de release.

El actualizador no realiza red ni actualizaciones silenciosas. El canal de distribución debe validar una firma o hash antes de sustituir binarios.
