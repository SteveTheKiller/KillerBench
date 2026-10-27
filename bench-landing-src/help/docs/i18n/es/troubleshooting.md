# Solución de problemas

## Una acción dice que está bloqueada

Un resultado bloqueado significa que la acción solicitada no pudo continuar. Lee el mensaje y el diario para ver si se detuvo antes del envío o después de un paso anterior. No supongas que una operación completada a medias se revirtió. Los motivos de requisitos previos más habituales son estos.

- **Se requiere acceso de administrador.** Cierra KillerBench y vuelve a iniciarlo con **Ejecutar como administrador**.
- **No debe haber un reinicio pendiente.** Windows tiene un reinicio pendiente. Reinicia el equipo y vuelve a ejecutar la acción.
- **Hay una directiva administrada.** La directiva de grupo o la administración de dispositivos controla la configuración. Cámbiala allí.
- **Las evidencias cambiaron.** Algo cambió entre la primera comprobación y la comprobación final. Vuelve a ejecutar la herramienta para leer el nuevo estado.

## Una solicitud anterior necesita conciliación

KillerBench encontró un registro pendiente de una solicitud interrumpida. Vuelve a ejecutar la misma acción. Comprueba qué ocurrió e informa del resultado real sin repetir la solicitud. En la línea de comandos, usa el comando `-recover` correspondiente, por ejemplo `killerbench-cli bitlocker-auto-unlock-recover`.

## Una comprobación muestra Incompletas o Desconocido

No se pudieron leer las evidencias, a menudo porque KillerBench no se estaba ejecutando como administrador o porque un servicio de Windows no estaba disponible. KillerBench lo indica con honestidad en lugar de suponer que el equipo está en buen estado.

## Dónde se guardan los archivos

Los diarios, los registros pendientes y las evidencias guardadas están en `%LOCALAPPDATA%\KillerBench`. Cada acción protegida tiene su propia carpeta. Consérvalos cuando necesites un registro de lo que cambió.

## Informar de un problema

KillerBench está en desarrollo. Incluye el informe exportado y el registro de actividad cuando informes de un problema, y quita todo lo que no quieras compartir.
