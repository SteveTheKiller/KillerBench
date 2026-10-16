# Resolver problemas de impresión

Usa esta guía cuando los trabajos se quedan en la cola, una impresora aparece sin conexión o la impresora predeterminada no es la correcta. KillerBench lee primero la cola de impresión, las colas, los trabajos, los controladores y los puertos, y después te permite corregir una cola o un trabajo exactos.

## Comprobar las impresoras

1. Abre **Dispositivos** y elige **Impresión**.
2. Pulsa **Buscar impresoras locales** y elige la cola que falla. Para comprobar todas las colas, elige **Usar comprobaciones generales de impresión**.
3. Deja **Reparar automáticamente problemas confirmados** desactivado y pulsa **Ejecutar diagnóstico**.
4. Lee el informe. Muestra el estado de Cola de impresión, las colas en pausa, el modo sin conexión, los trabajos visibles, el estado de los controladores, los registros de puertos, los errores recientes de PrintService y, para las impresoras de red, un intento de conexión a la dirección asignada a la impresora.

Una conexión correcta o un código de estado limpio no demuestra que la impresora tenga papel, tóner o un mecanismo que funcione.

## Reparar la cola

1. Vuelve a ejecutar **Impresión** con **Reparar automáticamente problemas confirmados** activado. Puede iniciar la cola de impresión habilitada, reanudar una cola exacta en pausa que tenga trabajos esperando y desactivar el modo sin conexión.
2. Desactivar el modo sin conexión puede liberar los trabajos en espera, así que asegúrate de que esos trabajos todavía deban imprimirse.
3. Una cola que alguien pausó a propósito y que no tiene nada en espera sigue en pausa.

## Eliminar un trabajo atascado

1. Pulsa **Buscar trabajos de impresión bloqueados**.
2. Selecciona el trabajo que muestra Error o Bloqueado y marca **Cancelar solo este trabajo seleccionado**.
3. Pulsa **Ejecutar diagnóstico**. Solo se cancela ese trabajo y el resultado se vuelve a leer.

## Corregir la impresora predeterminada

Elige la cola que debería ser la predeterminada, marca **Reparar una impresora predeterminada faltante o en conflicto con esta cola** y pulsa **Ejecutar diagnóstico**. Solo actúa cuando la predeterminada falta o está en conflicto.

## Desde la línea de comandos

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

Consulta [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) y [default-printer-set](cli.html#cmd-default-printer-set).

## Relacionado

- [Sección Dispositivos](devices.html#printing)
- [Resolver problemas de red e internet](fix-network.html)
