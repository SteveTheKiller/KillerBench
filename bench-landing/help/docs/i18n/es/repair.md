# Reparación

Reparación reúne las herramientas que pueden corregir algo. Cada una diagnostica primero. Solo se ofrece una reparación cuando evidencias recientes muestran el problema exacto que corrige, y solo se ejecuta después de que actives **Reparar automáticamente problemas confirmados** o confirmes tú mismo una reparación concreta. Cada reparación queda en el diario y se verifica después.

Guías paso a paso: [Reparar archivos del sistema](repair-system-files.html) y [Reparar Windows Update](fix-windows-update.html).

## Reparación automática y manual

**Reparar automáticamente problemas confirmados** permite que una herramienta de diagnóstico ejecute, después del análisis, las reparaciones que demuestra necesarias. Está desactivado de forma predeterminada.

Para una única reparación de mantenimiento, abre **Avanzado: registros y reparaciones manuales** y elige **Reparaciones**. Elige **Almacén de componentes (DISM)** o **Archivos del sistema (SFC)**, lee la vista previa, marca **Ejecutar esta reparación en este PC** y pulsa **Ejecutar reparación**. Consulta [Cómo se mantienen seguros los cambios](safety.html) para saber qué ocurre antes y después de cualquier cambio.

## Herramientas de esta sección

Úsalas después de que un diagnóstico identifique un problema. Las reparaciones requieren evidencias confirmadas y aprobación explícita.

### Servicios principales de Windows

Comprueba a la vez los servicios Energía, Plug and Play, Administrador de usuarios, Cliente de directiva de grupo, Servicio de interfaz de almacenamiento en red, Asignador de extremos de RPC, Llamada a procedimiento remoto, Iniciador de procesos de servidor DCOM, Servicio de infraestructura de tareas en segundo plano y CoreMessaging. La recuperación opcional solo inicia servicios automáticos habilitados exactos que, según evidencias recientes, están detenidos tras la ventana de inicio y comprobaciones completas de dependencias. Los servicios de inicio a petición no se cambian cuando están inactivos. Se conservan los planes de energía, los dispositivos, los controladores, las cuentas, las credenciales, los perfiles, las directivas, los adaptadores, las direcciones, las rutas, el DNS, la configuración del firewall, los registros, los extremos, los registros de tareas, los datos de aplicaciones, los permisos de los servicios y los tipos de inicio. No se solicita ninguna actualización de directivas, búsqueda de dispositivos, acción de cuenta, cambio de red, cambio de energía, cambio de registro ni inicio de tareas. La verificación abarca solo el estado estable de los servicios, no el comportamiento completo de las características.

### Archivos del sistema

Hace un inventario delimitado de los estados de los paquetes de mantenimiento, las características opcionales y las capacidades de Windows, comprueba los requisitos previos, analiza los archivos protegidos del sistema y elige SFC automáticamente solo cuando la comprobación en vivo confirma daños. Los estados pendientes de paquetes, características o capacidades piden revisión, pero no autorizan instalaciones, eliminaciones, limpiezas, reversiones, habilitaciones ni deshabilitaciones.

### Repositorio WMI

Comprueba la disponibilidad del servicio WMI y después la coherencia del repositorio. La reparación automática puede iniciar un servicio detenido elegible y recuperar daños confirmados, y después verificar cada resultado. Se conserva la configuración de servicio deshabilitado. Los registros legibles se combinan; el contenido ilegible puede requerir reinstalar el proveedor. Los clientes de administración pueden interrumpirse. Sin restablecimiento, eliminación ni reversión automática.

### Notificaciones del sistema de Windows

Lee el historial reciente del Administrador de control de servicios y después comprueba a la vez Sistema de eventos COM+ y Servicio de notificación de eventos del sistema. La recuperación opcional verifica que Sistema de eventos COM+ esté listo antes de iniciar SENS. Las notificaciones existentes de inicio de sesión, red, energía y COM+ pueden reanudarse con la configuración actual. Se conservan los catálogos COM+, las suscripciones, los registros de notificaciones, los consumidores, las cuentas, los permisos, la configuración de recuperación, las directivas y los tipos de inicio. No se cambia ninguna suscripción, registro, consumidor, cuenta, permiso ni directiva. La verificación abarca solo el estado estable de los servicios, no la entrega completa de notificaciones.

### Servicio de directivas de diagnóstico

Revisa el historial operativo delimitado de Directiva de diagnóstico, el historial exacto de fallos del servicio y la disponibilidad actual del servicio. Las evidencias incompletas tienen prioridad. Los registros históricos no pueden autorizar la recuperación del servicio ni una acción de solución de problemas. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Los escenarios de diagnóstico existentes pueden reanudarse. Se conservan las directivas, los datos de solución de problemas y el tipo de inicio. No se ejecuta ningún solucionador de problemas ni corrección sugerida. La verificación abarca solo el estado estable del servicio, no la detección de fallos, la finalización del diagnóstico ni el resultado de una reparación.

### Informe de errores de Windows

Revisa la configuración de volcados de memoria, los metadatos delimitados de los archivos de volcado, los informes recientes de bloqueos del sistema, el historial exacto de fallos del servicio Informe de errores de Windows y la disponibilidad actual del servicio. Los informes históricos no establecen un fallo actual ni una causa raíz y no pueden autorizar una recuperación. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Los informes en cola existentes pueden reanudarse según la directiva actual. Se conservan los archivos de informes, los volcados, el consentimiento, la directiva de carga y el tipo de inicio. No se solicita ninguna carga ni eliminación. La verificación abarca solo el estado estable del servicio, no la creación, carga o entrega de informes, la resolución de problemas ni el diagnóstico de bloqueos.

### Recopilador de eventos de Windows

Revisa el inventario delimitado de suscripciones de Recopilador de eventos de Windows, el historial exacto de fallos del servicio y la disponibilidad actual del servicio. Un inventario vacío puede ser intencionado, y el número de suscripciones o los fallos históricos no pueden autorizar una recuperación. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las suscripciones existentes pueden reanudar la recopilación remota de eventos. Se conservan las suscripciones, los eventos reenviados, las credenciales, la configuración de los canales, las directivas y el tipo de inicio. La verificación abarca solo el estado estable del servicio, no la accesibilidad del origen, la autenticación, la entrega remota, la actualidad de los eventos ni el estado de las suscripciones.

### Registro de eventos de Windows

Inspecciona cuatro canales de diagnóstico para conocer su estado habilitado, el modo de retención, el tamaño actual, el límite configurado, el número de registros y la saturación, y después comprueba la disponibilidad del servicio Registro de eventos de Windows. La reparación automática puede iniciar un servicio habilitado y detenido después de comprobar las dependencias y verificar que esté en ejecución. Una acción específica confirmada por separado puede habilitar un único canal de diagnóstico exacto deshabilitado y no administrado, conservando el resto de la configuración indicada. Se conservan los registros, los permisos de los canales, la retención, los límites de tamaño, los tipos de inicio y la configuración administrada. La entrega correcta de eventos y la capacidad de archivo siguen sin verificar.

### Servicio Hora de Windows

Comprueba la configuración, el servicio y la sincronización de Hora de Windows. La reparación automática puede iniciar W32Time cuando el estado actual indica el error exacto de servicio detenido y se superan comprobaciones actualizadas de directivas, modo y dependencias, y después solicitar una resincronización y verificar el estado nuevo. Esto puede ajustar el reloj usando los orígenes ya configurados. Se conservan el tipo de inicio, la configuración de orígenes y la configuración administrada.

### Servicio Programador de tareas

Hace un inventario delimitado del estado de las tareas programadas, los resultados de ejecución y el número de ejecuciones omitidas, y lo combina con el historial exacto de fallos del servicio Programador de tareas antes de comprobar la disponibilidad actual del servicio. Los hallazgos de tareas y los fallos históricos del servicio no pueden autorizar el inicio del servicio. La recuperación opcional solo inicia un servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las tareas habilitadas existentes pueden empezar con sus cuentas configuradas, incluidos los desencadenadores omitidos. No se cambia ninguna definición de tarea, entidad de seguridad, referencia de credenciales, directiva ni tipo de inicio, y no se inicia ni elimina directamente ninguna tarea. La verificación abarca solo el estado estable del servicio, no el éxito de las acciones, la validez de las credenciales ni los resultados futuros de las tareas.

### Servicio de perfil de usuario

Comprueba la disponibilidad de Servicio de perfil de usuario. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las solicitudes existentes de inicio de sesión y de perfil pueden reanudarse. Se conservan las carpetas de perfil, los subárboles del registro, los permisos, los registros de la lista de perfiles, las credenciales, los datos de usuario, las directivas, los permisos del servicio y el tipo de inicio. No se solicita ninguna operación de perfil. La verificación abarca solo el estado estable del servicio, no el inicio de sesión, la carga de perfiles ni el acceso a los datos de usuario.

### Servicios de copia de seguridad

Comprueba los servicios Instantáneas de volumen, Proveedor de instantáneas de software de Microsoft, Copia de seguridad de Windows y Servicio del motor de copia de seguridad a nivel de bloque, y después lee el estado de los escritores VSS, los tipos de proveedor registrados, las instantáneas actuales y la asignación del almacenamiento de instantáneas. Que haya servicios de inicio a petición detenidos puede ser normal cuando están inactivos. La recuperación opcional solo inicia servicios automáticos detenidos tras comprobaciones de dependencias, y después verifica que estén en ejecución. El trabajo de copia de seguridad programado o interrumpido puede reanudarse. Se conservan la configuración de inicio deshabilitado y a petición, las instantáneas, el almacenamiento de instantáneas, los registros de proveedores, los planes de copia de seguridad, los catálogos, las programaciones, los destinos, las credenciales y la retención. No se selecciona ni reconfigura ninguna copia de seguridad, no se reinicia ningún servicio y no se elimina ninguna instantánea ni catálogo. El registro del proveedor, el estado de los escritores, la asignación de almacenamiento y el estado estable de los servicios no verifican que la copia de seguridad se complete, que se pueda acceder al destino ni que la restauración funcione.
