# BitLocker

BitLocker inspecciona el cifrado de las unidades, los protectores y la información de recuperación, y después te ayuda a hacer copias, proteger o cambiar una unidad. Solo funciona en este equipo.

Guías paso a paso: [Cifrar una unidad con BitLocker](encrypt-with-bitlocker.html).

## Usar la herramienta BitLocker

Elige la letra de unidad y una **Operación**, y pulsa **Ejecutar diagnóstico**.

- **Inspeccionar** lee el estado de cifrado, el estado de protección y las identidades de los protectores. Es la opción predeterminada y no cambia nada.
- **Copia de recuperación** escribe la contraseña de recuperación seleccionada en un archivo nuevo fuera de la unidad protegida y la verifica. Pulsa antes **Buscar protectores de recuperación**, elige el protector y confirma la copia.
- **Cifrar** hace una copia de la contraseña de recuperación seleccionada, verifica la copia y después inicia el cifrado una sola vez con el método que elijas. La unidad de Windows necesita un protector de TPM y una contraseña de recuperación. Una unidad de datos solo necesita una contraseña de recuperación.
- **Reanudar protección** vuelve a activar la protección en una unidad totalmente cifrada en la que estaba suspendida.

Cada una necesita su propia casilla de confirmación, y KillerBench vuelve a comprobar la unidad justo antes de actuar.

## Más acciones de BitLocker

Abre **Más acciones de BitLocker** para los cambios que quedan fuera de **Ejecutar diagnóstico**. Elige una acción, pulsa **Vista previa** para leer exactamente lo que requiere, confirma y pulsa **Ejecutar acción**.

- **Activar el desbloqueo automático** hace que una unidad de datos cifrada se desbloquee cuando se inicia Windows. La unidad de Windows debe estar totalmente cifrada y con la protección activada.
- **Agregar un protector de TPM** o **Agregar una contraseña de recuperación** crea un protector nuevo. KillerBench nunca muestra ni registra una contraseña de recuperación nueva, así que haz una copia enseguida.
- **Guardar la contraseña de recuperación en Active Directory** o **Microsoft Entra ID** envía el protector que seleccionaste arriba. KillerBench informa de que Windows aceptó la solicitud. No puede leer la copia del directorio, así que confírmala allí.
- **Descifrar esta unidad** inicia el descifrado, por ejemplo antes de volver a cifrar con un método obligatorio. Necesita una segunda confirmación porque después la unidad queda sin protección, y no lo permite en la unidad de Windows mientras otras unidades se desbloqueen automáticamente desde ella.

## TPM y FIPS

Si el TPM está presente pero no está listo, la sección Seguridad puede prepararlo sin borrarlo. KillerBench nunca borra un TPM, porque eso destruye las claves que guardan BitLocker y Windows Hello. El cambio de la directiva de algoritmos FIPS está disponible como una reparación explícita y reversible para las organizaciones que necesitan PIN de BitLocker aprobados por FIPS.

## Herramientas de esta sección

Inspecciona el cifrado, los protectores y la información de recuperación, y después haz copias o protege la unidad.

### BitLocker

Previsualiza las opciones de BitLocker e inspecciona el volumen seleccionado. Este flujo de trabajo no realiza ningún cifrado ni copia de recuperación.
