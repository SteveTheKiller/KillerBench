# Dispositivos

Dispositivos abarca las impresoras, el audio, las cámaras, Bluetooth, la entrada y los servicios de Windows de los que depende el hardware conectado.

Guías paso a paso: [Resolver problemas de impresión](fix-printing.html) e [Investigar bloqueos y pantallas azules](investigate-crashes.html).

## Herramientas de esta sección

Impresión, audio, cámaras, Bluetooth, entrada y los servicios que sostienen el hardware conectado.

### Impresión

Comprueba Cola de impresión, las pausas de las colas locales, los trabajos de impresión visibles, la configuración sin conexión y el estado que indican los controladores, y después hace inventario de los registros de colas, controladores, puertos, extremos TCP/IP estándar, procesadores de impresión y monitores de impresión. Relaciona los problemas recientes de PrintService solo cuando hay una identidad de cola actual exacta. Los extremos TCP/IP estándar asignados reciben un único intento de conexión delimitado, sin datos de aplicación, SNMP ni trabajos de impresión. La reparación automática puede iniciar la cola de impresión habilitada, reanudar solo colas exactas en pausa con evidencias recientes de trabajos en espera y desactivar el modo sin conexión. Las pausas intencionadas sin trabajos son solo de revisión. Se conservan las conexiones remotas, las cadenas de comunidad SNMP, los registros de extensiones y el contenido de los trabajos. Un código de estado o una conexión correcta no establecen la identidad de la impresora, los consumibles, la mecánica, la aceptación de la cola, el éxito del procesamiento ni la impresión física.

### Audio

Comprueba las identidades y los formatos compartidos de la entrada y la salida multimedia predeterminadas, el estado de los dispositivos y controladores de audio, Compilador de extremo de audio de Windows y Audio de Windows. La reparación automática inicia los servicios habilitados y detenidos y verifica que estén en ejecución. Se conservan la configuración de inicio, los controladores, los dispositivos seleccionados, el modo exclusivo y los niveles de volumen. La reproducción y grabación de sonido y el enrutamiento de las aplicaciones siguen sin probarse.

### Compatibilidad con cámaras

Inspecciona los errores de los dispositivos de cámara y de imagen, los identificadores de hardware, las evidencias de controladores instalados y la disponibilidad de Servidor de marco de la cámara de Windows. La recuperación opcional solo inicia un servicio habilitado y detenido tras comprobaciones de dependencias. Una aplicación que ya solicita acceso a la cámara puede reanudarse con los permisos existentes. Se conservan la configuración de privacidad de la cámara, los permisos, la configuración de los dispositivos, las directivas, los controladores y el tipo de inicio. Sin restablecimiento, cambio de permisos ni instalación de controladores. La verificación abarca solo la configuración indicada del dispositivo y el estado en ejecución del servicio, no la captura de imágenes, el acceso de privacidad ni el funcionamiento físico.

### Compatibilidad con Bluetooth

Inspecciona los errores de los dispositivos Bluetooth, los identificadores de hardware, las evidencias de controladores instalados y la disponibilidad de Servicio de compatibilidad con Bluetooth. La recuperación opcional solo inicia un servicio habilitado y detenido tras comprobaciones de dependencias; los dispositivos emparejados pueden volver a conectarse. Se conservan los emparejamientos, la configuración de radio, la configuración de los dispositivos, las directivas y el tipo de inicio. Sin restablecimiento, eliminación de dispositivos ni instalación de controladores. La verificación abarca solo la configuración indicada del dispositivo y el estado en ejecución del servicio, no la detección, el emparejamiento, el audio ni la transferencia de datos.

### Compatibilidad con tarjetas inteligentes

Inspecciona los errores de los lectores de tarjetas inteligentes, los identificadores de hardware, las evidencias de controladores instalados y la disponibilidad del servicio Tarjeta inteligente. La recuperación opcional solo inicia un servicio habilitado y detenido tras comprobaciones de dependencias. Las aplicaciones pueden volver a detectar las tarjetas insertadas según la directiva existente. Se conservan los certificados, los PIN, las credenciales, la configuración de lectores y tarjetas, las directivas, los controladores y el tipo de inicio. Sin inicialización, inscripción, operaciones de PIN, restablecimiento de lectores ni instalación de controladores. La verificación abarca solo la configuración indicada del lector y el estado en ejecución del servicio, no la detección de tarjetas, la autenticación ni el acceso de las aplicaciones.

### Compatibilidad biométrica

Inspecciona los errores de los dispositivos biométricos, los identificadores de hardware, las evidencias de controladores instalados y la disponibilidad de Servicio biométrico de Windows. La recuperación opcional solo inicia un servicio habilitado y detenido tras comprobaciones de dependencias. Las solicitudes existentes de Windows Hello y de aplicaciones pueden reanudarse según la directiva actual. Se conservan las plantillas biométricas, los datos de Windows Hello, las credenciales, la configuración de los dispositivos, los permisos, las directivas, los controladores y el tipo de inicio. Sin inscripción, eliminación de plantillas, restablecimiento de sensores, cambio de permisos ni instalación de controladores. La verificación abarca solo la configuración indicada del sensor y el estado en ejecución del servicio, no la captura, la comparación, el inicio de sesión ni el acceso de las aplicaciones.

### Servicio de dispositivo de interfaz humana

Comprueba la disponibilidad de Servicio de dispositivo de interfaz humana. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Los botones de acceso rápido, los controles multimedia y otras características HID existentes pueden reanudarse. Se conservan los dispositivos, los controladores, las asignaciones de botones, la configuración de accesibilidad, la configuración de entrada, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de dispositivos, controladores, asignaciones ni configuración de entrada. La verificación abarca solo el estado estable del servicio, no el comportamiento de los botones, los controles ni las aplicaciones.

### Servicio enumerador de dispositivos portátiles

Comprueba la disponibilidad de Servicio enumerador de dispositivos portátiles. La recuperación opcional solo inicia el servicio exacto cuando está detenido y configurado para iniciarse automáticamente, tras comprobaciones de dependencias actualizadas. Un servicio de inicio a petición detenido sigue siendo solo informativo. Las aplicaciones existentes pueden volver a detectar los dispositivos portátiles conectados y acceder a ellos. Se conservan los dispositivos, los controladores, los archivos, las bibliotecas, la configuración de sincronización, los permisos, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de dispositivos, archivos, bibliotecas, sincronización ni transferencia. La verificación abarca solo el estado estable del servicio, no la enumeración, el acceso, la sincronización ni la transferencia.

### Adquisición de imágenes de Windows (WIA)

Comprueba la disponibilidad del servicio Adquisición de imágenes de Windows. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las solicitudes existentes de escáneres y de adquisición de imágenes fijas pueden reanudarse con los permisos actuales de las aplicaciones. Se conservan los dispositivos, los controladores, los datos de imagen, los permisos de las aplicaciones, la configuración de privacidad, la configuración de transferencia, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de dispositivos, controladores, imágenes, permisos, transferencia ni adquisición. La verificación abarca solo el estado estable del servicio, no el funcionamiento del escáner, la cámara, la transferencia ni las aplicaciones.

### Detección de hardware shell

Inspecciona evidencias delimitadas de Plug and Play de controladoras de almacenamiento, discos y dispositivos portátiles y las referencias de controladores instalados, y después comprueba la disponibilidad del servicio Detección de hardware shell. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las notificaciones existentes de Reproducción automática y de llegada de hardware pueden reanudarse según la directiva actual. Las observaciones de dispositivos y el historial conservado no pueden autorizar una recuperación. Se conservan los dispositivos, los controladores, los volúmenes, los puntos de montaje, la configuración de Reproducción automática, los controladores de eventos, las directivas, los permisos del servicio y el tipo de inicio. No se solicita ninguna operación de dispositivos ni de configuración. La verificación abarca solo el estado estable del servicio, no la entrega de notificaciones, la disponibilidad de los medios, el acceso a los volúmenes ni el funcionamiento físico de los dispositivos.

### Servicio de instalación de dispositivos

Comprueba la disponibilidad de Servicio de instalación de dispositivos. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. El trabajo existente de instalación de dispositivos solicitado por Windows puede reanudarse según la directiva actual. Se conservan los dispositivos, los controladores, los paquetes, la configuración, los enlaces, la configuración de clases, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ni instala nada. La verificación abarca solo el estado estable del servicio, no la instalación ni el funcionamiento de los dispositivos.

### Servicio de asociación de dispositivos

Comprueba el historial reciente de configuración de dispositivos y de Plug and Play, y después la disponibilidad de Servicio de asociación de dispositivos. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. El trabajo existente de asociación de dispositivos solicitado por Windows puede reanudarse. Se conservan los dispositivos emparejados y asociados, los controladores, las credenciales, los metadatos, los permisos, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de dispositivos, emparejamiento, eliminación, restablecimiento ni controladores. La verificación abarca solo el estado estable del servicio, no la detección, el emparejamiento, la asociación ni el funcionamiento de los dispositivos.

### Servicio de mejora de pantalla

Inspecciona las evidencias de los adaptadores gráficos y los monitores, y después comprueba la disponibilidad de Servicio de mejora de pantalla. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las solicitudes existentes de brillo, color y mejora pueden reanudarse. Se conservan la configuración de pantalla, el brillo, los perfiles de color, la configuración HDR, los dispositivos, los controladores, la configuración de energía, las directivas, los permisos del servicio y el tipo de inicio. No se cambia ninguna configuración de pantalla. La verificación abarca solo el estado estable del servicio, no el comportamiento de la mejora, la calidad de imagen ni el funcionamiento físico de la pantalla.

### Servicio de administración de entrada de texto

Inspecciona las evidencias de teclado, mouse y dispositivos HID, y después comprueba la disponibilidad de Servicio de administración de entrada de texto. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las solicitudes existentes de teclado táctil, escritura a mano, emoji y entrada de texto avanzada pueden reanudarse. Se conservan los diccionarios, los datos de escritura a mano, la configuración de idioma y teclado, la configuración de accesibilidad y privacidad, los dispositivos, los controladores, las directivas, los permisos del servicio y el tipo de inicio. No se recopila ningún texto introducido, pulsación ni contenido de escritura a mano. La verificación abarca solo el estado estable del servicio, no la introducción de texto, el reconocimiento ni el funcionamiento de los dispositivos.

### Servicio de caché de fuentes de Windows

Comprueba la disponibilidad de Servicio de caché de fuentes de Windows. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las aplicaciones existentes pueden volver a usar la caché de fuentes actual. Se conservan las fuentes instaladas, los archivos de fuentes, los datos de aplicaciones, los archivos de caché, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no instala ni quita ninguna fuente, y no elimina ni reconstruye la caché. La verificación abarca solo el estado estable del servicio, no la representación del texto ni el comportamiento de las aplicaciones.

### Host de proveedor de detección de función

Comprueba la disponibilidad de Host de proveedor de detección de función. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las aplicaciones existentes pueden volver a detectar dispositivos de red a través de los proveedores instalados. Se conservan los perfiles de red, los recursos compartidos, los dispositivos, los proveedores, las reglas del firewall, la configuración de detección, las credenciales, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de dispositivos, recursos compartidos, proveedores, reglas del firewall ni configuración de detección. La verificación abarca solo el estado estable del servicio, no el estado de los proveedores, la detección de dispositivos ni la conectividad.

### Publicación de recurso de detección de función

Comprueba la disponibilidad de Publicación de recurso de detección de función. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Este PC puede volverse detectable mediante la configuración existente de detección de redes y del firewall. Se conservan los perfiles de red, los recursos compartidos, los recursos publicados, las reglas del firewall, la configuración de detección, las credenciales, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de recursos compartidos, recursos, reglas del firewall ni configuración de detección. La verificación abarca solo el estado estable del servicio, no la publicación, la visibilidad, el acceso ni la conectividad.
