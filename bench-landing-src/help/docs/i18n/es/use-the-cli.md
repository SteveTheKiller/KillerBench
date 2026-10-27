# Ejecutar KillerBench desde scripts

Usa la línea de comandos cuando quieras evidencias de KillerBench en un script, un trabajo de RMM o una consola remota. `killerbench-cli` ejecuta el mismo Engine que la app de escritorio e imprime JSON.

## Primeros pasos

1. Abre PowerShell donde esté `killerbench-cli.exe`, o añade su carpeta a la ruta.
2. Ejecuta `killerbench-cli help` para ver todos los comandos con su límite de seguridad.
3. Ejecuta `killerbench-cli diagnostic-scopes` para ver los 11 grupos de diagnóstico recomendados.

## Ejecutar un análisis y guardar un informe

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` es solo de diagnóstico y rechaza `--auto-repair`. Usa un flujo de trabajo de reparación específico cuando necesites una acción concreta. `--report` escribe el mismo informe con tema que exporta la app. Usa `--scope` para comprobar solo algunas áreas, y `common-check` para obtener JSON combinado sin diario de flujo de trabajo.

## Leer los códigos de salida

| Código | Significado |
| --- | --- |
| 0 | La vista previa o el resultado verificado tuvo éxito. |
| 1 | Un error de inicio, de entrada o del diario detuvo el comando. |
| 2 | No se reconocieron los argumentos. |
| 3 | Bloqueado, fallido o sin verificar. |
| 4 | Un flujo de trabajo necesita un reinicio antes de poder verificar. |
| 130 | Lo detuviste tú. |

La salida 0 en un comando de solo lectura significa que se ejecutó, no que el equipo esté en buen estado. Lee el JSON para ver los hallazgos.

## Hacer un cambio

Todos los comandos que pueden cambiar el equipo muestran una vista previa de forma predeterminada. Lee la vista previa y después ejecuta el mismo comando con `--execute`. Si se interrumpe una solicitud, ejecuta el comando `-recover` correspondiente; informa del resultado real y nunca repite el cambio.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Volver a abrir resultados anteriores

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## Usarlo desde un asistente de IA

[KillerMCP](../mcp.html) puede llamar a la CLI instalada para consultar códigos de error del Administrador de dispositivos y de Windows.

## Relacionado

- [Referencia de la línea de comandos](cli.html)
- [Guía de Engine para desarrolladores](../engine/index.html)
- [Solución de problemas](troubleshooting.html)
