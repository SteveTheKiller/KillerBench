# Comprobar y reparar una unidad

Usa esta guía cuando un volumen informa de errores, Windows pide analizar una unidad o los archivos no se abren. KillerBench comprueba primero el volumen en modo de solo lectura y solo programa o ejecuta una reparación del sistema de archivos después de que confirmes que tienes una copia de seguridad.

## Antes de empezar

- Haz una copia de seguridad de los datos importantes del volumen. Una reparación del sistema de archivos puede cambiar o quitar archivos dañados.
- Ejecuta KillerBench como administrador.

## Comprobar el volumen

1. Abre **Almacenamiento** y elige **Almacenamiento**.
2. Elige la unidad en **Volumen para comprobar** y pulsa **Ejecutar diagnóstico**.
3. Lee el informe. Muestra el estado del disco y del volumen, el bit de volumen sucio, la autorreparación de NTFS en línea, los errores de controladora y de dispositivos de disco, los contadores de confiabilidad y los eventos de almacenamiento recientes, como reinicios, reintentos y extracciones inesperadas.

Una comprobación limpia significa que no se ofrece ninguna reparación. La temperatura del disco, el desgaste y los contadores de E/S no corregidos son evidencias para revisar y no activan una reparación por sí solos.

## Reparar el sistema de archivos

1. Pulsa **Revisar reparación de sistema de archivos** y elige **Reparación puntual** o **Análisis y reparación sin conexión**.
2. Para un volumen de datos, marca **Permitir que un volumen no de sistema quede sin conexión** si puedes cerrar todo lo que lo usa. El volumen de Windows no puede quedar sin conexión, así que su reparación se programa para el siguiente reinicio.
3. Marca **Los datos importantes de este volumen están respaldados**.
4. Confirma la reparación. KillerBench vuelve a comprobar el volumen exacto justo antes de actuar y primero registra una entrada pendiente.
5. Para una reparación programada, reinicia cuando te convenga. KillerBench verifica el resultado después del reinicio sin volver a ejecutar la reparación.

## Optimizar o hacer retrim

La misma herramienta puede analizar el volumen y ejecutar la optimización que recomienda Windows, **Retrim de almacenamiento de estado sólido** o una desfragmentación del disco duro. Cada una es una acción confirmada por separado sobre un único volumen exacto en buen estado.

## Desde la línea de comandos

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

Consulta [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) y [volume-optimize](cli.html#cmd-volume-optimize).

## Relacionado

- [Sección Almacenamiento](storage.html)
- [Liberar espacio en disco](free-disk-space.html)
- [Solución de problemas](troubleshooting.html)
