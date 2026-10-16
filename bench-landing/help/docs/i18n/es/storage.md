# Almacenamiento

Inspecciona discos, volúmenes, Espacios de almacenamiento y los servicios de mantenimiento de unidades. El diagnóstico no selecciona ninguna operación de disco; cualquier acción compatible que cambie el equipo necesita su propio destino exacto, requisitos previos y confirmación.

Guías paso a paso: [Comprobar y reparar una unidad](check-a-drive.html) y [Liberar espacio en disco](free-disk-space.html).

## Herramientas de esta sección

### Almacenamiento

Asigna de forma delimitada las identidades de discos, particiones y volúmenes con letra de unidad, evalúa la presión del 10 % de espacio libre en cada volumen con letra de unidad, inspecciona la asignación de almacenamiento de instantáneas VSS y los valores predeterminados de cuota de cada volumen, resume el progreso actual de los trabajos del módulo Storage, comprueba el volumen NTFS seleccionado y su estado de autorreparación en línea, e inspecciona los dispositivos de disco y controladora de este PC. Los umbrales de capacidad, la configuración de cuotas y los estados de los trabajos son evidencias para revisar y no seleccionan limpieza, cambio de tamaño, cambios de cuota ni control de trabajos. La asignación de letras de unidad es puntual y no abarca todas las rutas de montaje. Los hallazgos de dispositivos no se asocian al volumen seleccionado. El volumen de Windows es el predeterminado. No se cambian archivos, instantáneas, trabajos ni controladores, no se cambia ninguna configuración, no se desmonta ningún volumen y no se programa ningún reinicio.

### Espacios de almacenamiento

Hace inventario de los grupos de Espacios de almacenamiento no primordiales y de los discos virtuales, incluido su estado, estado operativo, capacidad, asignación y resistencia, y devuelve una única conclusión operativa delimitada. Windows puede iniciar a petición su proveedor de administración de Espacios de almacenamiento al responder a esta consulta de solo lectura. Se conservan los grupos, los discos virtuales, los discos físicos, los volúmenes, la resistencia, la asignación, la configuración de mantenimiento, las directivas y el tipo de inicio. No se solicita ninguna operación de grupo, disco, volumen, asignación, conexión, reparación, optimización ni configuración. Los resultados del proveedor no verifican la accesibilidad del grupo, la redundancia, la integridad de los datos, la posibilidad de recuperar copias de seguridad ni la E/S de las aplicaciones.

### Servicio de almacenamiento

Revisa de forma delimitada los trabajos actuales del módulo Storage, el historial exacto de fallos del servicio y la disponibilidad actual de Servicio de almacenamiento. Las evidencias incompletas tienen prioridad. El estado de los trabajos y los fallos históricos no pueden autorizar la recuperación del servicio ni cambios de almacenamiento. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las notificaciones de almacenamiento existentes y la actividad de almacenamiento administrada por directivas pueden reanudarse. Se conservan los volúmenes, las particiones, los sistemas de archivos, los puntos de montaje, los dispositivos, los controladores, la configuración de Sensor de almacenamiento, la directiva de mantenimiento, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de trabajo, volumen, partición, punto de montaje, archivo, dispositivo, controlador ni configuración de almacenamiento. La verificación abarca solo el estado estable del servicio, no el resultado de los trabajos ni las operaciones de dispositivo, volumen o directiva de almacenamiento.

### Servicio Optimizar unidades

Comprueba la disponibilidad del servicio Optimizar unidades. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. La actividad de optimización ya programada por Windows puede reanudarse según la directiva de mantenimiento actual. Se conservan los volúmenes, los sistemas de archivos, los datos, las programaciones de optimización, la directiva de mantenimiento, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ningún volumen ni analiza, hace trim, desfragmenta, optimiza o programa ninguna unidad. La verificación abarca solo el estado estable del servicio, no el análisis, la optimización ni el estado del dispositivo.
