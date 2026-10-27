# Ayuda de KillerBench

KillerBench es un banco de trabajo de diagnóstico y reparación de Windows para técnicos. Primero comprueba el equipo, explica lo que encontró junto con las evidencias de cada hallazgo y solo cambia Windows cuando eliges una corrección concreta y la confirmas.

KillerBench está en desarrollo. Todavía no hay descarga pública. Estas páginas describen la compilación actual.

## Qué hace

- Ejecuta diagnósticos de solo lectura sobre Windows Update, dispositivos, almacenamiento, redes, impresión, seguridad, apps y perfiles, y después redacta un informe legible.
- Ofrece reparaciones protegidas que solo se ejecutan cuando evidencias recientes demuestran el problema exacto, y verifica el resultado después.
- Cubre tareas de configuración y refuerzo como BitLocker, la configuración de privacidad, despejar Windows y poner en marcha un equipo nuevo.
- Ejecuta los diagnósticos y el análisis de informes en local, sin IA y sin cuenta de KillerBench. Algunas acciones seleccionadas, como las descargas y la copia de la clave de recuperación en Entra ID, pueden contactar con servicios externos.
- Usa el tema activo de la app en la consola, los informes, las confirmaciones y los cuadros de diálogo Abrir, Guardar y Carpeta.

## Por dónde empezar

- [Descarga y configuración](setup.html)
- [Acerca de KillerBench](about.html)

- [Tu primer diagnóstico](first-run.html) recorre un análisis completo del sistema.
- [El espacio de trabajo](workspace.html) explica las secciones, los paneles y los atajos.
- [Cómo se mantienen seguros los cambios](safety.html) explica qué comprueba KillerBench antes y después de cualquier cambio.
- Las [guías paso a paso](fix-windows-update.html) recorren las tareas habituales.
- Las páginas de **Secciones** describen cada herramienta, agrupadas igual que en la app.
- [Línea de comandos](cli.html) enumera todos los comandos de `killerbench-cli`.
- [Solución de problemas](troubleshooting.html) trata las acciones bloqueadas, los reinicios y las solicitudes interrumpidas.

Los desarrolladores que quieran integrar los diagnósticos en sus propias herramientas pueden leer la [Guía de Engine para desarrolladores](../engine/index.html).
