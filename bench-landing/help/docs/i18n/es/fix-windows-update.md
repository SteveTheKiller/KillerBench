# Reparar Windows Update

Usa esta guía cuando las actualizaciones fallan, se quedan paradas o se reinstalan una y otra vez. KillerBench lee primero la cadena de actualización, explica el fallo con las evidencias que lo respaldan y solo inicia un servicio o repara el almacén de componentes cuando evidencias recientes demuestran ese problema exacto.

## Antes de empezar

- Ejecuta KillerBench como administrador. Los servicios de actualización, el almacén de componentes y los archivos protegidos del sistema necesitan elevación para obtener evidencias completas.
- Reinicia primero si Windows ya tiene un reinicio pendiente. Muchas reparaciones de actualizaciones están bloqueadas hasta que no quede ningún reinicio pendiente.
- Mantén el equipo conectado a la corriente durante el mantenimiento.

## Averiguar por qué fallan las actualizaciones

1. Abre **Actualizaciones** y elige **Windows Update** en la barra lateral de herramientas.
2. Deja **Reparar automáticamente problemas confirmados** desactivado en la primera pasada y pulsa **Ejecutar diagnóstico**.
3. Lee el **Informe legible**. Enumera los servicios de actualización detenidos, los reinicios pendientes, el poco espacio libre, los daños del almacén de componentes y los códigos de error exactos que registró Windows, con un siguiente paso recomendado.
4. Si el informe menciona un código de error, las comprobaciones de seguimiento correspondientes se ejecutan automáticamente. También puedes buscar uno tú mismo con `killerbench-cli update-code 0x80070002`.

## Vigilar una actualización que parece atascada

Elige **Monitor de actividad de mantenimiento** en **Actualizaciones** y pulsa **Ejecutar diagnóstico**. Toma instantáneas repetidas de la actividad de actualización, Setup e Installer, de los totales de transferencia y de los indicadores de reinicio. La actividad se muestra solo como evidencia; nunca afirma por sí sola que haya un atasco o un fallo.

## Reparar lo que encontró el informe

1. Vuelve a ejecutar **Windows Update** con **Reparar automáticamente problemas confirmados** activado. Puede iniciar servicios de actualización habilitados que estén confirmados como detenidos, y solo ejecuta DISM o SFC cuando la comprobación en vivo confirma daños.
2. Para un único servicio, elige su herramienta específica, como **Servicio BITS**, **Servicios de cifrado**, **Instalador de módulos de Windows** o **Servicio Update Orchestrator**. Cada una ofrece recuperación solo para ese servicio exacto.
3. Cuando el informe diga que reinicies, reinicia antes de ejecutar nada más. KillerBench registra los resultados que requieren reinicio y aplaza el trabajo posterior hasta que se haya reiniciado.

Cada reparación queda en el diario y se vuelve a leer. Consulta [Cómo se mantienen seguros los cambios](safety.html) para ver la secuencia completa.

## Instalar una actualización concreta

1. Elige **Actualizaciones disponibles** en **Actualizaciones** y pulsa **Ejecutar diagnóstico** para descubrir las actualizaciones del origen configurado.
2. Selecciona las actualizaciones que quieras y pulsa **Previsualizar seleccionadas**. La vista previa enumera exactamente lo que se instalaría y cualquier reinicio necesario.
3. Instala solo después de leer la vista previa. KillerBench nunca selecciona ni aprueba una actualización por ti.

## Desde la línea de comandos

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

Consulta [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) y [update-plan](cli.html#cmd-update-plan). Si se interrumpió una instalación, ejecuta [update-recover](cli.html#cmd-update-recover); comprueba el resultado y nunca reintenta la actualización.

## Relacionado

- [Sección Actualizaciones](updates.html)
- [Reparar archivos del sistema](repair-system-files.html)
- [Solución de problemas](troubleshooting.html)
