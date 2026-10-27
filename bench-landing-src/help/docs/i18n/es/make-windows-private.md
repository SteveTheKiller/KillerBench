# Hacer Windows más privado

Usa esta guía para revisar y reforzar la privacidad de un portátil. KillerBench lee la configuración actual, muestra cuáles ya controlan las directivas o la administración de dispositivos, y solo cambia la configuración que elijas, de una en una, con una forma exacta de deshacerla.

## Revisar la configuración actual

1. Abre **Privacidad**.
2. Marca las áreas que quieras revisar. Todas las áreas empiezan sin marcar.
3. Pulsa **Ejecutar diagnóstico**. El informe muestra el valor actual para este equipo y para la cuenta que ejecuta KillerBench, y marca la configuración controlada por directivas.

## Cambiar una configuración

1. Elige la configuración en **Configuración de privacidad**, como la cámara, el micrófono, la ubicación, los datos de diagnóstico, la publicidad, el historial del portapapeles, la búsqueda de contenido en la nube o los resaltados de búsqueda.
2. Elige el nuevo valor y marca **Aplicar cuando seleccione Ejecutar diagnóstico**.
3. Pulsa **Ejecutar diagnóstico**. KillerBench comprueba si hay una directiva que la controle, registra el valor actual, hace ese único cambio y lo vuelve a leer.

La configuración controlada por la directiva de grupo o la administración de dispositivos no se toca. Cámbiala donde se administra.

## Deshacer un cambio

Cada cambio verificado conserva un diario. La acción de deshacer correspondiente restaura el valor anterior exacto, de nuevo tras una comprobación de directivas.

## Despejar Windows

Para las sugerencias, el contenido para consumidores, Recall y las funciones de IA, el desorden de la barra de tareas e Inicio, Edge y el software preinstalado, abre **Mantenimiento** y elige **Despejar Windows**. La revisión es de solo lectura y no se selecciona nada automáticamente.

## Desde la línea de comandos

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

Consulta [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) y [debloat-review](cli.html#cmd-debloat-review). Una clave no compatible muestra las opciones compatibles.

## Relacionado

- [Sección Privacidad](privacy.html)
- [Configurar un equipo nuevo](set-up-new-pc.html)
