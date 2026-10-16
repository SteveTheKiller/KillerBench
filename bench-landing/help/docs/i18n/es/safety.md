# Cómo se mantienen seguros los cambios

KillerBench trata el diagnóstico y el cambio como tareas distintas. El diagnóstico solo lee. El análisis completo del sistema nunca ejecuta reparaciones. Selecciona una reparación o acción concreta, revisa sus opciones y requisitos previos y confirma el cambio propuesto.

## Antes de un cambio

1. **Evidencias recientes.** La herramienta vuelve a leer el estado actual. Un informe guardado o un evento antiguo nunca autoriza un cambio.
2. **Elegibilidad exacta.** El cambio solo se ofrece cuando las evidencias coinciden con el problema exacto que corrige. Cualquier dato incompleto lo bloquea.
3. **Tu confirmación.** Revisa la acción seleccionada en su cuadro de confirmación. Algunas acciones, como descifrar una unidad, necesitan una segunda confirmación. El cuadro sigue el tema activo de la app; su aspecto no cambia los requisitos de la acción.
4. **Requisitos previos.** Se comprueban los derechos de administrador, el estado de reinicio y la propiedad por directiva o administración de dispositivos. La configuración que pertenece a una directiva no se toca.
5. **Un registro pendiente.** KillerBench anota lo que va a hacer antes de pedírselo a Windows.
6. **Una comprobación final.** Todo se comprueba de nuevo justo antes de la solicitud, así que cualquier cambio intermedio la detiene.

## Después de un cambio

- KillerBench vuelve a leer el resultado y solo indica **Verificado** cuando Windows muestra el estado esperado.
- Si Windows devuelve éxito pero el estado no cambió, el resultado lo dice y no se repite nada.
- Cada cambio se guarda en un diario en `%LOCALAPPDATA%\KillerBench`.

## Si algo se interrumpe

Las acciones compatibles con diario conservan un registro pendiente cuando una solicitud pudo llegar a Windows y la verificación no terminó. Usa la ruta de conciliación documentada de la acción, o su comando `-recover` correspondiente, para inspeccionar ese registro. La conciliación no es una reversión automática. Conserva el diario cuando el resultado sea incierto y no repitas el cambio a mano. El comportamiento ante bloqueos, cancelaciones y arranques posteriores aún necesita validación nativa específica de cada acción.

## Lo que KillerBench no hará

- Borrar un TPM, eliminar protectores que no creó ni mostrar contraseñas de recuperación.
- Cambiar una configuración que controlan la directiva de grupo o la administración de dispositivos.
- Ejecutar una reparación solo porque un evento histórico o un informe importado mencione un problema.

Los diagnósticos y el análisis de informes se ejecutan en local. Una acción que selecciones puede contactar con un destino externo, como descargas de software o la copia de la clave de recuperación en Active Directory o Entra ID. Revisa el destino antes de confirmar. Las capturas guardadas y las evidencias sin procesar pueden incluir la identidad del equipo y rutas de origen, así que revísalas antes de compartirlas.

## Estado de desarrollo

La cobertura de reparaciones sigue creciendo. Una compilación correcta o una comprobación simulada no demuestra el comportamiento nativo de una reparación. El archivo `VM-VALIDATION.md` del repositorio registra los escenarios en máquinas Windows desechables y la validación pendiente.
