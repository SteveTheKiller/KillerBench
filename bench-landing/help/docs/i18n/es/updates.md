# Actualizaciones

Actualizaciones abarca Windows Update, los servicios que lo sostienen y el almacén de componentes. Puedes ver lo que está instalado, lo que está pendiente y por qué falla una actualización, y revisar las actualizaciones disponibles antes de elegir cuáles instalar.

Guías paso a paso: [Reparar Windows Update](fix-windows-update.html) y [Reparar archivos del sistema](repair-system-files.html).

## Herramientas de esta sección

Vigila la actividad de mantenimiento, revisa las actualizaciones disponibles y repara la cadena de Windows Update.

### Monitor de actividad de mantenimiento

Observa de forma delimitada la actividad de mantenimiento de Windows Update y Setup en una o varias instantáneas. Las opciones controlan la frecuencia de actualización y si se incluyen los eventos de actualización, el historial de actualizaciones, la tarea programada exacta de Windows Update, los estados exactos de los servicios, los indicadores de reinicio, las transferencias en segundo plano de la cuenta actual, la actividad de Optimización de distribución y la actividad de Windows Installer limitada por privacidad. Se excluyen los identificadores de proceso, las líneas de comandos, los usuarios y las rutas. La actividad no demuestra progreso, finalización, atasco ni fallo. No se solicita ninguna acción de actualización, instalación, transferencia, tarea, servicio, proceso, directiva, reinicio ni reparación.

### Actualizaciones disponibles

Descubre las actualizaciones de software y controladores seleccionadas usando el origen configurado, Windows Update o un servicio Microsoft Update ya registrado. Se comprueba que el origen esté listo antes del descubrimiento. El descubrimiento no instala nada, no acepta licencias, no registra orígenes ni cambia directivas. Las actualizaciones seleccionadas se pueden instalar después, por separado, solo tras marcar la casilla de confirmación. Es independiente de la reparación del mantenimiento.

### Windows Update

Hace un inventario delimitado de los estados de los paquetes de mantenimiento, las características opcionales y las capacidades de Windows, comprueba los indicadores de reinicio, el espacio libre y los servicios de actualización, observa Optimización de distribución sin iniciarla, y después analiza el almacén de componentes y los archivos protegidos del sistema. Los estados pendientes de paquetes, características o capacidades piden revisión, pero no autorizan ninguna acción sobre los componentes. La reparación automática puede iniciar los servicios detenidos Windows Update, BITS, Servicios de cifrado, Instalador de módulos de Windows y Update Orchestrator que estén configurados para iniciarse automáticamente. Un servicio de inicio a petición detenido se notifica, pero no se inicia. Iniciar Update Orchestrator puede reanudar la actividad de actualización programada existente según la directiva actual. Se conservan los paquetes, las características opcionales, las capacidades, la caché de Optimización de distribución, los pares, el ancho de banda, las directivas, la inscripción del origen, las programaciones y la configuración de inicio. DISM o SFC requieren evidencias en vivo de daños. Este flujo de trabajo no selecciona ni aprueba ninguna actualización.

### Servicio Windows Update

Comprueba el estado exacto de Servicio Windows Update, la DLL del servicio registrada, la actividad de actualización actual delimitada y el historial exacto de fallos del servicio. Las observaciones de la DLL, la actividad de actualización y los fallos históricos no pueden autorizar el inicio del servicio ni una operación de actualización. La recuperación opcional solo inicia un servicio automático detenido tras comprobaciones actualizadas de dependencias y del registro del ejecutable. Un servicio de inicio a petición detenido sigue siendo solo informativo. La actividad de actualización existente puede reanudarse según la directiva actual. Se conservan la directiva de actualización, la inscripción del origen, las programaciones, la configuración de reinicio, los paquetes, los trabajos de BITS, los permisos del servicio y el tipo de inicio. KillerBench no selecciona, aprueba ni instala ninguna actualización. La verificación abarca solo el estado estable del servicio, no la accesibilidad del origen, la aplicabilidad, la descarga, la instalación, la finalización ni la disponibilidad para reiniciar.

### Optimización de distribución

Comprueba el estado exacto del servicio Optimización de distribución. Un servicio de inicio a petición detenido sigue siendo solo informativo. Solo un estado exacto En pausa puede ofrecer recuperación. La recuperación opcional repite la comprobación de estado, envía una única solicitud de continuar delimitada y exige dos observaciones coherentes En ejecución. Se conservan el tipo de inicio, el contenido de la caché, los pares, el ancho de banda, el origen de las actualizaciones y la directiva administrada. La actividad de transferencia, el estado de la caché, el progreso de las actualizaciones y su finalización siguen sin verificar.

### Servicio BITS

Comprueba el estado exacto de Servicio de transferencia inteligente en segundo plano, los trabajos de transferencia de la cuenta actual y el historial exacto y delimitado de fallos del servicio. Los fallos históricos y las instantáneas de transferencias no pueden autorizar el inicio del servicio. La recuperación opcional solo inicia el servicio exacto cuando está detenido y configurado para iniciarse automáticamente, tras comprobaciones de dependencias actualizadas. Un servicio de inicio a petición detenido sigue siendo solo informativo. Las aplicaciones existentes pueden reanudar las transferencias en segundo plano en cola. Se conservan los trabajos, las prioridades, los propietarios, los destinos, la configuración de proxy, las directivas y el tipo de inicio. La verificación abarca solo el estado estable del servicio, no el progreso de las transferencias, la propiedad de las aplicaciones ni la finalización.

### Transferencias en segundo plano (BITS)

Lee los trabajos de transferencia de BITS en el ámbito de cuenta seleccionado y revisa las identidades exactas de los trabajos. Los nombres de las colas no establecen a qué aplicación pertenecen. No se reanuda, completa, cancela, elimina ni reasigna ninguna transferencia.

### Servicios de cifrado

Comprueba el estado exacto de Servicios de cifrado, la DLL del servicio registrada y el historial exacto y delimitado de fallos del servicio. Los fallos históricos y las observaciones de la DLL no pueden autorizar el inicio del servicio ni la sustitución de componentes. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las operaciones existentes de catálogo, certificados y actualizaciones pueden reanudarse. Se conservan los certificados, las claves, los catálogos, la configuración de confianza, las directivas y el tipo de inicio. La verificación abarca solo el estado estable del servicio, no la validación de certificados, firmas, catálogos ni confianza.

### Instalador de módulos de Windows

Comprueba el estado exacto de Instalador de módulos de Windows, las transiciones delimitadas de los paquetes de mantenimiento y el historial exacto de fallos del servicio. Los fallos históricos y los estados de los paquetes no pueden autorizar el inicio del servicio ni una operación de paquetes. La recuperación opcional solo inicia el servicio exacto cuando está detenido y configurado para iniciarse automáticamente, tras comprobaciones de dependencias actualizadas. Un servicio de inicio a petición detenido sigue siendo solo informativo. La actividad de mantenimiento existente puede reanudarse. Se conservan los paquetes, el contenido del almacén de componentes, los orígenes, las directivas y el tipo de inicio. La verificación abarca solo el estado estable del servicio, no el estado del almacén de componentes, la aplicabilidad de los paquetes, la finalización del mantenimiento ni la disponibilidad para reiniciar.

### Servicio Windows Update Medic

Comprueba el estado exacto de Servicio Windows Update Medic, la DLL del servicio registrada, la actividad de actualización actual delimitada y el historial exacto de fallos del servicio. Las observaciones de la DLL, la actividad de actualización y los fallos históricos no pueden autorizar el inicio del servicio ni una acción sobre los componentes. Que un servicio de inicio a petición esté detenido puede ser normal. Cuando se selecciona esta herramienta exacta, la recuperación opcional solo puede iniciar el servicio habilitado y detenido tras comprobaciones de dependencias actualizadas. La corrección de Windows puede reanudarse según la directiva actual y puede reparar componentes de actualización o desencadenar actividad de actualización. Se conservan la directiva de actualización, la inscripción del origen, las programaciones, los paquetes, los trabajos de BITS, la configuración de reinicio, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna actualización ni cambia ninguna configuración de actualización. La verificación abarca solo el estado estable del servicio, no la corrección, el estado de los componentes, la aplicabilidad, la descarga, la instalación ni la disponibilidad para reiniciar.

### Servicio Update Orchestrator

Comprueba el estado exacto de Servicio Update Orchestrator, la tarea integrada exacta de Windows Update, la actividad actual delimitada y el historial exacto de fallos del servicio. Los resultados de la tarea, la actividad de actualización y los fallos históricos no pueden autorizar el inicio del servicio ni una operación de actualización. La recuperación opcional solo inicia un servicio automático detenido tras comprobaciones actualizadas de dependencias y del registro del ejecutable. Un servicio de inicio a petición detenido sigue siendo solo informativo. La actividad de actualización programada existente puede reanudarse según la directiva actual y puede descargar o instalar actualizaciones ya aprobadas. Se conservan la directiva de actualización, la inscripción del origen, las programaciones, la configuración de reinicio, los paquetes, los trabajos de BITS, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ni aprueba ninguna actualización. La verificación abarca solo el estado estable del servicio, no la aplicabilidad, la descarga, la instalación, la finalización ni la disponibilidad para reiniciar.

### Limpieza del almacén de componentes

Analiza si Windows recomienda limpiar el almacén de componentes. El mantenimiento opcional quita de inmediato los componentes reemplazados y vuelve a analizar. Sin ResetBase ni borrado manual de WinSxS. No hay forma automática de deshacerlo; no se mide el espacio en disco recuperado.
