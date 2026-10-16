# Liberar espacio en disco

Usa esta guía cuando a una unidad le queda poco espacio. KillerBench muestra en qué se va el espacio, previsualiza lo que quitaría cada destino de limpieza y solo borra después de que actives la limpieza permanente.

## Ver cuánto espacio queda

1. Abre **Almacenamiento** y elige **Almacenamiento**.
2. Pulsa **Ejecutar diagnóstico**. El informe marca cada volumen con letra de unidad que tenga menos del 10 % de espacio libre y muestra la asignación de instantáneas, los valores predeterminados de cuota y la directiva de Sensor de almacenamiento.
3. Para los volcados de memoria y el contenido antiguo de actualizaciones, ejecuta también **Constantes vitales** en **Diagnóstico** y **Windows Update** en **Actualizaciones**; el tamaño de la caché de actualizaciones aparece allí.

## Previsualizar una limpieza

1. Abre **Mantenimiento** y elige **Limpieza**.
2. Marca los destinos que quieras, como **Archivos temporales del usuario actual**, **Archivos temporales de Windows**, **Cachés de navegador del usuario actual**, **Caché de Delivery Optimization** o **Papelera de reciclaje del usuario actual**. Pasa el puntero sobre un destino para leer exactamente qué carpetas abarca.
3. Ajusta **Antigüedad mínima (días)** y **Archivos máximos por destino**.
4. Pulsa **Ejecutar diagnóstico** con **Realizar la limpieza permanente seleccionada después de comprobaciones actualizadas** desactivado. El informe enumera los candidatos y el espacio estimado de cada destino. No se borra nada.

## Quitar los archivos

1. Activa **Realizar la limpieza permanente seleccionada después de comprobaciones actualizadas** y pulsa **Ejecutar diagnóstico**.
2. KillerBench vuelve a comprobar cada candidato antes de quitarlo, omite los archivos recientes, protegidos o en uso, y registra cada resultado en el diario.

La limpieza permanente no pasa por la Papelera de reciclaje y no se puede deshacer. Quitar los informes de errores y los volcados de memoria descarta evidencias que podrías necesitar para investigar bloqueos, así que revísalos primero.

## Quitar la instalación anterior de Windows

Después de una actualización de características, **Instalación anterior de Windows** en **Limpieza** puede quitar la copia de reversión mediante la solicitud de eliminación de reversión compatible de Windows. Se ejecuta por separado, necesita **Respaldé todos los archivos y configuraciones que necesito de la instalación anterior** y quita para siempre la posibilidad de volver a la versión anterior.

## Desde la línea de comandos

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

Consulta [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) y [previous-windows](cli.html#cmd-previous-windows).

## Relacionado

- [Sección Mantenimiento](maintenance.html#cleanup)
- [Sección Almacenamiento](storage.html)
- [Comprobar y reparar una unidad](check-a-drive.html)
