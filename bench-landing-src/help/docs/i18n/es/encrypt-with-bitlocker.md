# Cifrar una unidad con BitLocker

Usa esta guía para activar BitLocker, hacer una copia de la clave de recuperación y confirmar que la unidad está protegida. KillerBench hace la copia de la contraseña de recuperación y la verifica antes de iniciar el cifrado, y nunca muestra ni registra una contraseña de recuperación.

## Antes de empezar

- Ejecuta KillerBench como administrador.
- La unidad de Windows necesita un protector de TPM y un protector de contraseña de recuperación. Una unidad de datos solo necesita una contraseña de recuperación.
- Decide dónde irá la copia de la clave de recuperación: un archivo fuera de la unidad que vas a cifrar y, opcionalmente, Active Directory o Microsoft Entra ID.

## Comprobar la unidad

1. Abre **BitLocker** y elige la unidad en **Volumen**.
2. Deja la operación en **Inspeccionar** y pulsa **Ejecutar diagnóstico**. El informe muestra el estado de cifrado, el estado de protección y las identidades de los protectores.
3. Si el TPM está presente pero no está listo, prepáralo primero desde **Seguridad**. KillerBench nunca borra un TPM.

## Agregar los protectores que necesitas

1. Abre **Más acciones de BitLocker**.
2. Elige **Agregar un protector de TPM** o **Agregar una contraseña de recuperación**, pulsa **Vista previa**, marca **Hacer este cambio en la unidad seleccionada cuando pulse Ejecutar acción** y pulsa **Ejecutar acción**.
3. Haz enseguida una copia de la nueva contraseña de recuperación. KillerBench no la muestra.

## Hacer copia de la clave de recuperación y cifrar

1. Pulsa **Buscar protectores de recuperación** y selecciona el protector de contraseña de recuperación.
2. Escribe una ruta completa para el archivo de copia, fuera de la unidad que vas a cifrar.
3. Elige **Cifrar**, elige el método de cifrado y marca **Hacer copia de la clave de recuperación seleccionada y luego iniciar el cifrado cuando seleccione Ejecutar diagnóstico**.
4. Pulsa **Ejecutar diagnóstico**. KillerBench escribe la copia, la verifica y después inicia el cifrado una sola vez.
5. El cifrado continúa en segundo plano. Ejecuta **Inspeccionar** más tarde para confirmar que la unidad aparece totalmente cifrada y con la protección activada.

## Hacer copia en Active Directory o Entra ID

En **Más acciones de BitLocker**, elige la copia en el directorio para el protector seleccionado. KillerBench informa de que Windows aceptó la solicitud, pero no puede leer la copia del directorio, así que confírmala en Active Directory o en el centro de administración de Entra.

## Unidades de datos y desbloqueo automático

Después de cifrar una unidad de datos, **Activar el desbloqueo automático** hace que se desbloquee cuando se inicia Windows. La unidad de Windows ya debe estar totalmente cifrada y con la protección activada.

## Desde la línea de comandos

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

Consulta [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) y [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Cada uno tiene su comando `-recover` correspondiente para una solicitud interrumpida.

## Relacionado

- [Sección BitLocker](bitlocker.html)
- [Sección Seguridad](security.html)
- [Cómo se mantienen seguros los cambios](safety.html)
