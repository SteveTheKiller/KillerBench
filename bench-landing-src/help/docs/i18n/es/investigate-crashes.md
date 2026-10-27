# Investigar bloqueos y pantallas azules

Usa esta guía cuando Windows se reinicia de forma inesperada, muestra un error de detención o una aplicación se bloquea una y otra vez. KillerBench reúne en un solo informe las evidencias de bloqueos, apagados, errores de hardware y confiabilidad. Describe lo que se registró y nunca adivina una causa raíz.

## Comprobar el equipo

1. Abre **Diagnóstico** y elige **Constantes vitales**.
2. Pulsa **Ejecutar diagnóstico**. El informe combina la memoria, el estado de los discos, los problemas de dispositivos, el historial del Monitor de confiabilidad, los registros de errores de hardware, los informes de bloqueos, los apagados inesperados y los resultados del Diagnóstico de memoria de Windows.
3. Lee la conclusión y las evidencias que la acompañan. Los bloqueos históricos explican lo que ocurrió; no demuestran que el fallo siga presente.

## Acotar el problema

- **Historial de errores de hardware** lee los registros de WHEA y del diagnóstico de memoria junto con los problemas de dispositivos y el estado de los discos.
- **Historial de confiabilidad** agrupa los bloqueos de aplicaciones, los cuelgues y los apagados inesperados de los últimos 14 días.
- **Errores de aplicaciones** se centra en los programas que se bloquean.
- **Historial de instalación de controladores** muestra las instalaciones de controladores fallidas asociadas a dispositivos que están presentes ahora.
- **Problemas de dispositivos** enumera los dispositivos con códigos de error del Administrador de dispositivos.

## Leer volcados de memoria

KillerBench enumera la configuración de volcados y los metadatos de los archivos de volcado, y puede analizar la estructura de los minivolcados para obtener la excepción y los nombres de los módulos. La app de escritorio nunca abre el contenido de los volcados. Con un paquete de conocimiento de comprobación de errores cargado, los códigos de detención se resuelven a su significado documentado.

Conserva los volcados hasta que termines de investigar. **Limpieza** puede quitarlos, pero así se descartan las evidencias.

## Desde la línea de comandos

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

Consulta [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) y [device-code](cli.html#cmd-device-code).

## Relacionado

- [Sección Diagnóstico](diagnostics.html#vitals)
- [Informes y evidencias](reports.html)
