# Diagnóstico

Diagnóstico es donde debería empezar cada visita. Cada herramienta lee el equipo y redacta un informe. Nada de aquí cambia Windows. La herramienta predeterminada, el análisis completo del sistema, ejecuta un conjunto amplio de comprobaciones de solo lectura y ordena lo que necesita atención.

Guías paso a paso: [Investigar bloqueos y pantallas azules](investigate-crashes.html) y [Reparar archivos del sistema](repair-system-files.html).

## Herramientas de esta sección

Comprueba primero el equipo y después lee en el informe qué necesita atención y cuál es el siguiente paso seguro.

### Análisis completo del sistema

Inspecciona 49 áreas de diagnóstico y evalúa las recetas de reparación instaladas con las evidencias actuales. Las comprobaciones pueden tardar varios minutos. El análisis completo del sistema es solo de diagnóstico. Usa un flujo de trabajo de reparación específico para seleccionar y confirmar una acción compatible después de revisar requisitos previos actualizados. Las instantáneas históricas, de copias de seguridad y de transferencias son de solo lectura. El contenido de los volcados no se abre. La inspección del bit de volumen sucio no programa reparaciones ni reinicios. Este análisis no instala actualizaciones, no cambia controladores, no borra archivos ni realiza una prueba completa del hardware.

### Constantes vitales

Captura la memoria disponible, el estado de los discos que indica el proveedor y el estado de protección de BitLocker, y después relaciona las constantes vitales actuales con los problemas de dispositivos, el historial de confiabilidad, los registros de errores de hardware, las evidencias de bloqueos, los apagados inesperados y los resultados del Diagnóstico de memoria de Windows. Los indicadores de reinicio, el espacio libre del disco del sistema, la elevación y la configuración del servicio de actualización siguen visibles. No se recopilan contraseñas de recuperación. Este flujo de trabajo no ejecuta reparaciones. El resultado combinado no es una prueba completa del hardware, del software ni de la estabilidad.

### Historial de confiabilidad

Lee los registros del Monitor de confiabilidad de Windows de los últimos 14 días junto con los errores recientes de Aplicación y los eventos de apagado inesperado, y devuelve una única conclusión histórica delimitada. Los registros históricos no establecen el estado actual ni la causa raíz. No se selecciona ninguna reparación y no se cambia ninguna aplicación, servicio ni configuración.

### Rendimiento del inicio

Lee los metadatos de los eventos de rendimiento de inicio y apagado de Windows de los últimos 14 días, conservando solo la identidad del proveedor y campos numéricos de duración delimitados, y los relaciona con los indicadores de reinicio pendiente compatibles. Las rutas, los nombres de procesos, las cuentas y el resto de la carga útil de los eventos se quitan antes de guardar las evidencias. Los registros históricos de degradación no identifican un cuello de botella actual ni autorizan cambios de inicio, servicios, aplicaciones, controladores, tareas, energía, reinicio ni directivas.

### Historial de errores de hardware

Lee el historial de Windows Hardware Error Architecture y del Diagnóstico de memoria de los últimos 14 días, los problemas actuales de dispositivos Plug and Play, el estado de los discos físicos y los contadores de confiabilidad del proveedor, y devuelve una única conclusión delimitada sobre el hardware. Los registros deben relacionarse con su carga útil, los diagnósticos del firmware y las pruebas de hardware del fabricante. No se deduce ningún componente defectuoso ni causa raíz, no se selecciona ninguna reparación y no se cambia ningún hardware, firmware, controlador, disco, grupo, volumen, archivo, servicio, energía, reinicio ni configuración.

### Historial de instalación de controladores

Lee el final conservado del historial de instalación de dispositivos de Windows SetupAPI, relaciona las secciones fallidas con las identidades Plug and Play exactas actuales y devuelve una única conclusión delimitada. Las secciones históricas del registro no demuestran que un dispositivo siga presente ni que un fallo sea actual. No se instala, quita, revierte ni actualiza ningún controlador.

### Puntos de Restaurar sistema

Enumera hasta 256 puntos de Restaurar sistema disponibles, revisa la elegibilidad explícita para crear uno y devuelve una única conclusión de disponibilidad delimitada. El inventario no demuestra que la protección esté activada en todos los volúmenes ni que una restauración vaya a funcionar. Durante el diagnóstico no se crea, elimina ni aplica ningún punto de restauración.

### Entorno de recuperación de Windows

Lee la configuración actual del Entorno de recuperación de Windows con REAgentC, relaciona una ubicación local habilitada con el disco y la partición exactos actuales, y devuelve una única conclusión de disponibilidad delimitada. La reparación automática opcional puede habilitar una configuración existente deshabilitada y después verificar el estado indicado. La imagen de recuperación no se descarga, traslada ni sustituye, no se inicia ningún arranque de recuperación y Windows no se reinicia. El estado habilitado y la presencia de la partición no demuestran que la imagen de recuperación pueda arrancar o reparar este PC.

### Activación de Windows

Lee el estado actual de la licencia de Windows sin recopilar claves de producto completas ni parciales, conserva los metadatos anonimizados de los eventos de advertencia, error y críticos de Protección de software de los últimos siete días, y devuelve una única conclusión operativa delimitada. La carga útil de los eventos se quita antes de guardar las evidencias. Los registros históricos siguen como contexto cuando la licencia actual aparece como Con licencia. No se selecciona ninguna acción de activación, clave de producto, token, licencia, cuenta, servicio ni directiva. El estado del proveedor y el historial de eventos no demuestran el derecho de suscripción, la accesibilidad del servidor de activación, la integridad del almacén de tokens, la validez futura ni la causa raíz.

### Servicios de Windows

Hace inventario de los nombres de servicio instalados, los nombres para mostrar, los estados, los modos de inicio y los tipos de acción de recuperación limitados por privacidad, sin recopilar cuentas, rutas de ejecutables, comandos de recuperación ni mensajes de reinicio. Revisa la coherencia de los modos de inicio, las identidades de las dependencias directas y los ciclos, repite los estados pendientes exactos, inspecciona los servicios compatibles y las dependencias de los servicios detenidos, y después relaciona los fallos recientes del Administrador de control de servicios solo cuando hay un nombre de servicio exacto o un nombre para mostrar único. El uso de grupos de orden de carga se cuenta sin expandir sus miembros, y las identidades ambiguas quedan sin resolver. Que haya servicios de inicio a petición detenidos puede ser normal. No se inicia ningún servicio y se conservan la configuración de inicio, dependencias y recuperación. Usa el flujo de trabajo correspondiente de Reparación cuando una característica concreta necesite recuperación.

### Errores de aplicaciones

Lee los eventos recientes de error y críticos de Aplicación junto con un historial delimitado del Monitor de confiabilidad, y devuelve una única conclusión sobre el historial de estabilidad. Los registros históricos no demuestran un fallo actual ni una causa raíz, y solo orientan un diagnóstico más específico. No se detiene, repara, quita ni reconfigura ninguna aplicación.

### Dispositivos de pantalla

Inspecciona los adaptadores gráficos y los monitores en busca de errores de dispositivo indicados, identificadores de hardware y versiones de controladores instaladas. Sin instalación de controladores, restablecimiento de dispositivos ni cambio de resolución. No se prueban la calidad de imagen ni las conexiones físicas.

### Dispositivos de entrada

Inspecciona los errores de dispositivos de teclado, mouse y HID y el inventario de controladores. No se capturan pulsaciones, no se restablecen dispositivos y no se cambian controladores. No se prueba el funcionamiento físico de la entrada.

### Problemas de dispositivos

Busca códigos de error de configuración Plug and Play distintos de cero en todas las clases de dispositivos y después relaciona las evidencias de los dispositivos presentes con el historial delimitado de instalación de controladores, configuración de dispositivos y Plug and Play de SetupAPI. Los registros históricos por sí solos no establecen un fallo actual. Revisa la identidad exacta del dispositivo y las evidencias de controladores aprobados sin cambiar el hardware, los controladores ni la configuración. No se verifican los dispositivos que faltan ni el funcionamiento físico.
