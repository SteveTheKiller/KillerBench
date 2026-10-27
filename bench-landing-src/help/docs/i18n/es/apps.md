# Apps

Apps revisa los programas instalados, los paquetes de Store, Office y Outlook, las extensiones del navegador, las apps de inicio y la búsqueda. Las herramientas de eliminación y restablecimiento siempre muestran primero una revisión y solo cambian lo que selecciones.

Guías paso a paso: [Configurar un equipo nuevo](set-up-new-pc.html).

## Herramientas de esta sección

Revisa las aplicaciones instaladas, los paquetes de Store, Office y Outlook, las extensiones del navegador y la búsqueda.

### Aplicaciones instaladas

Lee los registros de desinstalación de aplicaciones en el ámbito seleccionado del equipo o de la cuenta en ejecución, y combina ese ámbito con evidencias delimitadas de errores de Aplicación, del Monitor de confiabilidad y de reinicios. Se excluyen otras cuentas y las apps portátiles. Los registros y el historial general no son una prueba exacta del estado ni de la propiedad de una aplicación. No se selecciona ninguna acción de instalador, desinstalador, aplicación, proceso, servicio, archivo, registro, cuenta, directiva ni reinicio.

### Paquetes de apps

Hace inventario de los paquetes Appx/MSIX registrados para el ámbito de cuenta seleccionado. La cuenta en ejecución es la opción predeterminada; todos los usuarios requiere acceso de administrador. Indica las identidades exactas, las marcas de framework y de no extraíble, y el número de dependencias. No se evalúan los paquetes aprovisionados, las aplicaciones Win32, la propiedad administrada ni los datos de las aplicaciones. No se quita ningún paquete.

### Extensiones del navegador

Hace un inventario delimitado de las identidades de las extensiones de Chrome, Edge, Brave y Firefox de la cuenta en ejecución. Se excluyen el historial del navegador, las credenciales, la configuración de las extensiones, el contenido de los manifiestos, los comandos y los valores de directiva. La presencia no demuestra que la extensión esté habilitada, sea de confianza, a quién pertenece ni que sea seguro quitarla. No se cambia ningún navegador, extensión ni directiva.

### Aplicaciones de inicio

Hace inventario de los nombres de registro Run y RunOnce del equipo y de la cuenta en ejecución, los nombres de archivo de la carpeta Inicio, los estados de aprobación de inicio de Windows y los nombres y estados de las tareas programadas de arranque o inicio de sesión, y devuelve una única conclusión sobre el entorno de inicio. No se recopilan líneas de comandos, rutas de destino, marcas de tiempo binarias de aprobación, acciones de tareas, argumentos, cuentas ni credenciales. Un registro no demuestra que un elemento se ejecutara, causara un retraso, sea no deseado ni que sea seguro deshabilitarlo. El estado de aprobación no demuestra que exista un registro correspondiente. No se selecciona ningún cambio de registro, archivo, aplicación, tarea, servicio ni directiva.

### Eliminación de apps de Microsoft

Revisa los registros seleccionados de OneDrive, Outlook, Office/M365, Project y Teams con opciones de ámbito de cuenta y de conservación. Se cotejan los inventarios del equipo, de la cuenta en ejecución y de los paquetes registrados; la revisión de todos los usuarios incluye también los paquetes aprovisionados para usuarios nuevos. Un paquete compatible de la cuenta actual puede recibir un plan de protección exacto e independiente en la app de escritorio o en la CLI. Quitar ese único paquete es una acción de escritorio protegida e independiente, con sus propias confirmaciones. La revisión en sí no cambia ningún proceso, aplicación, paquete, archivo, credencial, ubicación de carpeta, valor del registro ni dato de usuario.

### Restablecer Outlook

Revisa el nuevo Outlook, Outlook clásico o ambos, con opciones explícitas para las cachés, la configuración del perfil, la autenticación, los archivos OST, los complementos y la reinstalación del nuevo Outlook. La revisión de la cuenta en ejecución comprueba ubicaciones conocidas delimitadas, registros exactos y que los procesos estén cerrados. Una acción independiente y confirmada que solo afecta a la caché conserva los directorios de caché exactos con nombres de recuperación únicos y verifica cada movimiento. La revisión de todos los usuarios y las demás áreas de restablecimiento son solo de revisión. No se quita ninguna caché conservada, perfil, archivo OST, credencial, complemento, paquete ni configuración.

### Inventario de Foxit

Hace inventario de los productos Foxit instalados en el equipo y de los valores del registro del actualizador en ambas vistas. No se verifican las instalaciones de usuario, la directiva efectiva ni la propiedad de los productos. No se cambia ninguna aplicación ni configuración.

### Servicio Windows Installer

Comprueba las directivas de restricción, reversión, elevación, registro y depuración de Installer, los marcadores de operaciones en curso, los binarios del motor y la visibilidad de los paquetes en caché registrados antes de recuperar el servicio. Relaciona los resultados fallidos y correctos recientes solo con identidades de producto exactas indicadas. Si se cumplen los requisitos previos, la reparación automática puede iniciar el servicio habilitado y verificar que esté En ejecución. Los resultados históricos, el registro, la visibilidad de la caché y los hallazgos de depuración no autorizan cambios de paquetes ni de directivas. Se conservan las directivas, los registros, los marcadores de transacciones, los tipos de inicio y los paquetes en caché. No se prueba la instalación de ningún paquete concreto.

### Windows Search

Comprueba la disponibilidad del servicio Windows Search, la actividad actual de SystemIndex y el historial reciente de eventos de Search. La reparación automática puede iniciar un servicio habilitado y detenido y verificar que esté en ejecución. Una opción explícita e independiente de reconstrucción del índice conserva el índice anterior, inicia un índice nuevo y verifica el estado exacto del servicio y del directorio. Se conservan las ubicaciones indizadas, las directivas, el tipo de inicio y los datos conservados. Iniciar el servicio o reconstruir el índice puede reanudar la indización en segundo plano. La integridad de los resultados de búsqueda sigue sin probarse.

### Preparación de aplicaciones

Comprueba la disponibilidad del servicio Preparación de aplicaciones. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. El primer inicio de sesión y el aprovisionamiento de aplicaciones existentes pueden reanudarse según la directiva actual. Se conservan los paquetes, los registros, las licencias, los perfiles de usuario, los datos de aplicaciones, las directivas y el tipo de inicio. KillerBench no selecciona ninguna operación de paquetes. La verificación abarca solo el estado estable del servicio, no el inicio de sesión, el aprovisionamiento ni el inicio de aplicaciones.

### Servicio de repositorio de estado

Comprueba la disponibilidad de Servicio de repositorio de estado. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las solicitudes existentes de estado del modelo de aplicaciones pueden reanudarse. Se conservan los registros, los paquetes, las licencias, los perfiles, los datos del navegador y de las aplicaciones, el contenido del repositorio, las directivas y el tipo de inicio. No se solicita ningún restablecimiento, nuevo registro ni eliminación. La verificación abarca solo el estado estable del servicio, no la coherencia del repositorio ni el funcionamiento de las aplicaciones.

### Servicio de implementación de AppX

Comprueba la disponibilidad de Servicio de implementación de AppX. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las solicitudes existentes de implementación, registro o mantenimiento pueden reanudarse según la directiva actual. Se conservan los paquetes, los registros, las licencias, los perfiles, los datos de aplicaciones, el contenido del repositorio, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de paquetes. La verificación abarca solo el estado estable del servicio, no la implementación, el registro ni el inicio de aplicaciones.

### Servicio de licencias de cliente

Comprueba la disponibilidad de Servicio de licencias de cliente. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las solicitudes existentes de licencias de aplicaciones de Store pueden reanudarse según la directiva actual. Se conservan los paquetes, los registros, las licencias, los tokens, las cuentas, los perfiles, los datos de aplicaciones, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de paquetes, licencias, tokens ni cuentas. La verificación abarca solo el estado estable del servicio, no la validación de licencias ni el inicio de aplicaciones.

### Servicio de instalación de Microsoft Store

Comprueba el historial reciente de instalaciones de Microsoft Store y la disponibilidad exacta del servicio de instalación de Store. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las solicitudes existentes de descarga e instalación de Store pueden reanudarse según la directiva actual. Se conservan los paquetes, los registros, las licencias, las cuentas, los datos de aplicaciones, el estado de las descargas, los orígenes, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de paquetes, descargas, licencias, cuentas, orígenes ni instalaciones. La verificación abarca solo el estado estable del servicio, no los resultados de descargas, instalaciones, licencias ni aplicaciones.

### Servicio Administrador de licencias de Windows

Comprueba la disponibilidad de Servicio Administrador de licencias de Windows. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las comprobaciones existentes de licencias de aplicaciones pueden reanudarse según la directiva actual. Se conservan los paquetes, los registros, las licencias, los tokens, las cuentas, los datos de aplicaciones, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de paquetes, licencias, tokens, cuentas, registros ni derechos. La verificación abarca solo el estado estable del servicio, no la validación de licencias, los derechos ni el inicio de aplicaciones.

### Servicio PushToInstall de Windows

Comprueba la disponibilidad de Servicio PushToInstall de Windows. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Una solicitud de instalación remota ya aprobada puede reanudarse según la directiva actual. Se conservan los paquetes, los registros, las licencias, las cuentas, los datos de aplicaciones, las solicitudes en cola, los orígenes, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no crea, aprueba ni selecciona ninguna solicitud, paquete, origen, cuenta ni instalación. La verificación abarca solo el estado estable del servicio, no el procesamiento de solicitudes, la descarga, la instalación ni los resultados de las aplicaciones.
