# Reparar archivos del sistema

Usa esta guía cuando los componentes de Windows están dañados: las características no se instalan, las actualizaciones informan de errores de componentes o las herramientas del sistema se bloquean. KillerBench solo ejecuta DISM y SFC después de que una comprobación en vivo confirme daños, y después verifica el resultado.

## Antes de empezar

- Ejecuta KillerBench como administrador.
- Resuelve primero cualquier reinicio pendiente.
- Si el equipo está administrado, comprueba si una directiva dirige DISM a un origen de reparación concreto. KillerBench informa de esa directiva y no la cambia.

## Buscar daños

1. Abre **Reparación** y elige **Archivos del sistema**.
2. Deja **Reparar automáticamente problemas confirmados** desactivado y pulsa **Ejecutar diagnóstico**.
3. Lee el informe. Hace inventario de los estados de los paquetes de mantenimiento, las características opcionales y las capacidades, comprueba los requisitos previos y analiza los archivos protegidos del sistema. Un resultado limpio significa que no se ofrece ninguna reparación.

## Reparar daños confirmados

1. Activa **Reparar automáticamente problemas confirmados** y pulsa **Ejecutar reparación**. KillerBench elige DISM o SFC solo cuando la comprobación en vivo confirma daños.
2. Espera a la verificación. El informe muestra **Verificado** solo cuando la comprobación de seguimiento lee un estado reparado.
3. Si el informe dice que el almacén no se puede recuperar o que el origen de reparación no está disponible, detente ahí. Repetir la reparación no cambiará el resultado.

Una ejecución interrumpida de DISM o SFC se concilia en el siguiente arranque sin repetir el comando de mantenimiento.

## Limpiar el almacén de componentes

Elige **Limpieza del almacén de componentes** en **Actualizaciones**. Primero pregunta a Windows si se recomienda la limpieza. El mantenimiento opcional quita los componentes reemplazados y vuelve a analizar. Nunca usa ResetBase y nunca borra contenido de WinSxS a mano.

## Desde la línea de comandos

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

Consulta [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) y [servicing-logs](cli.html#cmd-servicing-logs). `repair` muestra una vista previa de forma predeterminada y solo actúa con `--execute`.

## Relacionado

- [Reparar Windows Update](fix-windows-update.html)
- [Sección Reparación](repair.html#system-files)
- [Cómo se mantienen seguros los cambios](safety.html)
