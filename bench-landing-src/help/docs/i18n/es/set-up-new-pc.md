# Configurar un equipo nuevo

Usa esta guía al preparar un PC nuevo o recién restablecido. KillerBench revisa el equipo, las aplicaciones que piensas instalar y el nombre que quieres darle, y después te indica las herramientas protegidas que hacen cada cambio.

## Empezar con una referencia

1. Ejecuta **Análisis completo del sistema** desde **Diagnóstico** y guarda el informe como registro del estado inicial del equipo.
2. Instala las actualizaciones pendientes. Consulta [Reparar Windows Update](fix-windows-update.html) si alguna falla.

## Revisar aplicaciones y configuración

1. Abre **Puesta en marcha** y elige **Puesta en marcha**.
2. Selecciona las aplicaciones y los grupos de configuración que piensas preparar y pulsa **Ejecutar diagnóstico**. La revisión los compara con lo que ya está instalado. KillerBench no descarga ni ejecuta instaladores.
3. Antes de ejecutar el instalador de un fabricante, verifícalo desde la línea de comandos. KillerBench solo confía en él cuando lleva una firma válida con el nombre exacto de la organización de ese fabricante.

## Poner nombre al equipo

1. Elige **Nombre del equipo**, escribe el nombre y pulsa **Ejecutar diagnóstico** para revisarlo frente a la identidad actual y el rol en el dominio.
2. En un equipo de grupo de trabajo, marca **Cambiar nombre de este PC de grupo de trabajo después de comprobaciones actualizadas** para cambiarle el nombre. Hace falta reiniciar para terminar.

## Protegerlo y ordenarlo

- [Cifra una unidad con BitLocker](encrypt-with-bitlocker.html) y haz una copia de la clave de recuperación.
- [Haz Windows más privado](make-windows-private.html) y revisa **Despejar Windows** en **Mantenimiento**.
- Crea un punto de restauración con **Preparar un punto de restauración** cuando todo esté listo.

## Desde la línea de comandos

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

Consulta [workflow](cli.html#cmd-workflow) y [restore-point](cli.html#cmd-restore-point).

## Relacionado

- [Sección Puesta en marcha](onboarding.html)
- [Tu primer diagnóstico](first-run.html)
