# Línea de comandos

`killerbench-cli` ejecuta el mismo Engine que la app de escritorio desde PowerShell o una consola remota. Todos los comandos que podrían cambiar el equipo muestran una vista previa de forma predeterminada y necesitan una opción explícita, como `--execute`, antes de actuar. Los comandos imprimen las evidencias JSON en la salida estándar y el progreso en la salida de errores estándar.

Guías paso a paso: [Ejecutar KillerBench desde scripts](use-the-cli.html).

El código de salida 0 significa que la vista previa o el resultado verificado tuvo éxito; 1, un error de inicio, de entrada o del diario; 2, que no se reconocieron los argumentos; 3, bloqueado, fallido o sin verificar; 4, que un flujo de trabajo necesita un reinicio antes de poder verificar, y 130, que lo detuviste tú.

Ejecuta `killerbench-cli` sin argumentos para imprimir esta lista en tu propio equipo.

## Actualizaciones y mantenimiento

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Revisión exacta de una transferencia u observación con dos instantáneas; sin reintento.
- `bits-jobs [--all-users]`: Estados, propietarios y recuentos de bytes de los trabajos de BITS; sin cambios en las transferencias.
- `bits-retry JOB_GUID [--execute]`: Vista previa de forma predeterminada; reintento explícito de un trabajo exacto, sin afirmar que la aplicación haya terminado.
- `bits-retry-recover JOB_GUID`: Dos instantáneas de solo lectura; archiva los resultados estables sin otro reintento.
- `component-repair-source`: Directiva administrada del origen de reparación de DISM y conflictos; sin cambios.
- `delivery-optimization`: Estado del servicio Optimización de distribución, de solo lectura; evidencias JSON.
- `delivery-optimization-activity`: Recuentos actuales de trabajos y totales de bytes limitados por privacidad; sin identidades de transferencias ni cambios.
- `delivery-optimization-policy`: Configuración clásica y de Policy CSP, de solo lectura; se excluyen los valores confidenciales y las identidades de proveedores.
- `feature-service-events`: Lee el historial exacto de fallos de los servicios de hora, búsqueda, WMI, registro de eventos y audio.
- `feature-update-policy`: Directiva de versión de destino, aplazamiento, pausa y protección, de solo lectura; evidencias JSON.
- `pending-file-operations`: Resumen de operaciones de archivo pendientes limitado por privacidad, de solo lectura; evidencias JSON.
- `restart-check`: Orígenes de reinicio pendiente, de solo lectura; evidencias JSON.
- `servicing-logs`: Análisis delimitado del registro CBS más reciente, de los archivos CBS de texto y comprimidos, y de los registros de DISM y de Windows Setup; sin reparación.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Instantáneas de la actividad de mantenimiento, de solo lectura.
- `servicing-service-events`: Historiales exactos de fallos de la cadena de mantenimiento de Windows Update, de solo lectura.
- `servicing-service-states`: Estado actual de los seis servicios de la cadena de mantenimiento de Windows Update, de solo lectura.
- `setup-code DECIMAL_OR_0xHEX`: Referencia documentada de códigos de resultado de Windows Setup; sin inspección del equipo ni reparación.
- `update-applicability`: Evidencias de edición, compilación y arquitectura de Windows, de solo lectura.
- `update-cache`: Totales delimitados de la caché de SoftwareDistribution, de solo lectura; sin nombres de archivo.
- `update-code DECIMAL_OR_0xHEX`: Referencia documentada de códigos de resultado de Windows Update; sin inspección del equipo ni reparación.
- `update-context`: Evidencias de la elevación actual y de la sesión de escritorio, de solo lectura; sin identificadores de cuenta.
- `update-engine-files`: Evidencias de presencia, versión y firma del motor nativo de Windows Update, de solo lectura.
- `update-events`: Historial del cliente de Windows Update de los últimos siete días, de solo lectura; JSON con las evidencias sin procesar.
- `update-followup DECIMAL_OR_0xHEX`: Diagnósticos recientes de solo lectura elegidos según un código de resultado documentado de Windows Update.
- `update-history`: Los 100 resultados de operaciones de Windows Update más recientes, de solo lectura; evidencias JSON.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: Repite `--select`; vista previa de forma predeterminada.
- `update-policy`: Directiva administrada de Windows Update, de solo lectura; evidencias JSON.
- `update-policy-surfaces`: Evidencias de la superficie de configuración clásica y de Policy CSP, de solo lectura; se excluyen los valores y las identidades de proveedores.
- `update-recover`: Comprueba una actualización pendiente; nunca la instala ni la reintenta.
- `update-restart-policy`: Plazos de cumplimiento, periodos de gracia, supresión de reinicios y comportamiento de las notificaciones, de solo lectura.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: Descubrimiento y coincidencias en JSON; sin instalación.
- `update-service-events`: Historial exacto de fallos del servicio Windows Update, de solo lectura.
- `update-service-files`: Evidencias exactas de registro y firma de la DLL del servicio Windows Update, de solo lectura.
- `update-services`: Registros de servicios de Windows Update, de solo lectura; evidencias JSON.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Condicionado a una búsqueda reciente; vista previa de forma predeterminada; conserva el almacén anterior.
- `update-store-reset-recover`: Concilia un restablecimiento de almacén preparado sin repetirlo.
- `windows-capabilities`: Estados de las capacidades de Windows en línea, de solo lectura; resultado JSON.
- `windows-update-task`: Estado exacto y historial de ejecución de la tarea Scheduled Start, de solo lectura; sin ejecutar tareas ni operaciones de actualización.

## BitLocker y TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Vista previa de forma predeterminada; activa el desbloqueo automático en un volumen de datos cifrado.
- `bitlocker-auto-unlock-recover`: Verifica una solicitud de desbloqueo automático interrumpida sin repetirla.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Vista previa de forma predeterminada; solo un archivo nuevo de copia de recuperación.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Verifica una copia interrumpida sin volver a escribirla.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Vista previa de forma predeterminada; inicia el descifrado, por ejemplo antes de volver a cifrar con un método obligatorio.
- `bitlocker-decrypt-recover`: Verifica una solicitud de descifrado interrumpida sin repetirla.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Vista previa de forma predeterminada; requiere protectores de TPM y de recuperación existentes.
- `bitlocker-encrypt-recover`: Verifica el estado de conversión pendiente sin repetir la solicitud.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Vista previa de forma predeterminada; guarda una contraseña de recuperación existente en Active Directory o Microsoft Entra ID.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Solo vista previa de opciones.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Vista previa de forma predeterminada; agrega un protector de TPM o de contraseña de recuperación; la contraseña nunca se imprime.
- `bitlocker-protector-recover`: Verifica una solicitud de protector interrumpida sin repetirla.
- `bitlocker-protectors DRIVE:`: Solo identidades y tipos de protectores; sin contraseñas de recuperación.
- `bitlocker-resume DRIVE: [--execute]`: Vista previa de forma predeterminada; solo un volumen exacto totalmente cifrado y suspendido.
- `bitlocker-resume-recover`: Verifica el estado de protección pendiente sin repetir la solicitud.

## Seguridad y privacidad

- `certificate-chains`: Confianza de cadena local solo con caché para certificados personales, de solo lectura; sin revocación ni descargas.
- `defender-overview [--report]`: Resumen de protección, programación de análisis y amenazas agregadas, de solo lectura; sin acción.
- `defender-protection [--report]`: Preferencias de protección y superficies de directiva reconocidas, de solo lectura; se excluyen los detalles confidenciales; sin cambios.
- `defender-scan Quick|Full [--execute] [--report]`: Vista previa de forma predeterminada; un único análisis explícito y protegido de Microsoft Defender.
- `defender-scan-history [--report]`: Horarios de los análisis rápido y completo y estado de retraso, de solo lectura; se excluyen los detalles confidenciales; sin análisis.
- `defender-scan-recover [--report]`: Conciliación de solo lectura sin iniciar otro análisis.
- `defender-threats [--report]`: Historial agregado de amenazas, marcas activas actuales e identificadores de gravedad; se excluyen los detalles confidenciales; sin acción.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Vista previa de forma predeterminada; solo perfiles locales no administrados.
- `firewall-enable-recover`: Inspecciona un cambio pendiente sin repetirlo.
- `firewall-events`: Lee el historial operativo delimitado de advertencias, errores y eventos críticos del Firewall de Windows.
- `firewall-logging`: Lee la configuración del registro de tráfico de los perfiles Dominio, Privado y Público sin abrir los registros.
- `firewall-rules`: Lee recuentos de reglas efectivas y de orígenes de directiva limitados por privacidad.
- `firewall-service-events`: Lee el historial exacto de fallos de BFE y Firewall de Windows Defender en el Administrador de control de servicios.
- `privacy-clipboard-history on|off [--execute]`: Vista previa de forma predeterminada; valor exacto del usuario actual conservando las directivas.
- `privacy-clipboard-history-recover`: Vuelve a comprobar un cambio interrumpido sin repetirlo.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: Restauración protegida del valor anterior.
- `privacy-plan --select Camera --select ClipboardSync`: Solo revisa las selecciones; no se cambia ninguna configuración.
- `privacy-setting KEY on|off [--execute]`: Vista previa de forma predeterminada; cambio del usuario actual que conserva las directivas; las claves no válidas muestran las opciones compatibles.
- `privacy-setting-recover KEY`: Vuelve a comprobar un cambio interrumpido sin repetirlo.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: Restauración protegida del valor exacto.
- `security-check`: TPM, Arranque seguro, protecciones de virtualización y directiva de UAC, de solo lectura; evidencias JSON.

## Servicios

- `service-binaries`: Presencia de los binarios locales de los servicios y estado de Authenticode, de solo lectura; se excluyen las rutas y no se contactan rutas no locales.
- `service-check NAME [NAME ...] | service-check --all`: Estados de los servicios compatibles y evidencias de dependencias, de solo lectura.
- `service-dependencies`: Revisión de la integridad de las dependencias directas, de solo lectura; sin cambios en los servicios.
- `service-dlls`: Presencia de las DLL de servicio registradas y estado de Authenticode, de solo lectura; se excluyen las rutas y no se contactan rutas no locales.
- `service-events`: Historial de fallos de servicios, de solo lectura; JSON con las evidencias sin procesar.
- `time-events`: Historial reciente de eventos de Hora de Windows, de solo lectura, con evidencias JSON.
- `service-logons`: Revisión de la clase de cuenta de los servicios y de la interacción con el escritorio, de solo lectura; se excluyen las identidades de cuenta.
- `service-path-quoting`: Revisión del entrecomillado de las rutas de los ejecutables de servicio, de solo lectura; se excluyen las rutas completas y los argumentos.
- `service-recovery-policy`: Resumen de las acciones de recuperación de los servicios instalados, de solo lectura; se excluyen los comandos y los mensajes de reinicio.
- `service-startup-config`: Revisión de la coherencia de los modos de inicio, de solo lectura; sin cambios en los servicios.
- `service-transitions`: Observación repetida de los estados pendientes de los servicios, de solo lectura; sin acción sobre los servicios.
- `service-triggers`: Resumen de los desencadenadores de los servicios instalados, de solo lectura; se excluyen las cargas útiles de los desencadenadores; sin cambios.

## Almacenamiento, archivos y copias de seguridad

- `backup-events`: Historial de VSS y volsnap; evidencias JSON; sin cambios.
- `backup-service-events`: Lee el historial exacto de fallos de VSS, del proveedor de instantáneas de software, de Copia de seguridad de Windows y del motor a nivel de bloque.
- `backup-summary`: Resumen de las operaciones locales de Copia de seguridad de Windows; sin cambios en las copias de seguridad.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Vista previa de forma predeterminada; la Papelera de reciclaje es todo o nada y la antigüedad no se aplica.
- `disk-cleanup-task`: Estado exacto y historial de ejecución de la tarea, de solo lectura; sin ejecutar tareas ni limpieza.
- `disk-diagnostic`: Relaciona las tareas exactas de diagnóstico de disco con el historial de eventos; sin acción sobre tareas ni discos.
- `disk-diagnostic-tasks`: Historial exacto de las tareas de recopilación y resolución, de solo lectura; sin acción sobre tareas ni discos.
- `disk-quotas`: Valores predeterminados y estado de las cuotas de cada volumen, de solo lectura; se excluyen las identidades de usuario; sin cambios.
- `file-history-events`: Recuentos de advertencias y errores del Historial de archivos con proyección de privacidad; sin cambios.
- `file-history-status`: Estado de protección del Historial de archivos de la cuenta actual; sin cambios.
- `search-index-rebuild [--execute]`: Vista previa de forma predeterminada; conserva el índice anterior y verifica el estado nuevo.
- `search-index-rebuild-recover`: Conciliación de solo lectura sin repetir los comandos de reparación.
- `storage-check DRIVE_LETTER`: Comprobación fija de NTFS de solo lectura; resultados JSON y progreso en vivo.
- `storage-events`: Historial de almacenamiento, de solo lectura; JSON con las evidencias sin procesar.
- `storage-jobs`: Estado y progreso agregados de los trabajos del módulo Storage, de solo lectura; JSON; sin cambios.
- `storage-reliability`: Contadores de temperatura, desgaste y E/S, de solo lectura; JSON; sin reparación.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Vista previa de forma predeterminada; la ejecución requiere confirmar la copia de seguridad; los volúmenes del sistema se programan para el reinicio.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Solo vista previa; sin reparación ni programación.
- `storage-repair-recover`: Verifica una reparación del sistema de archivos completada o programada sin repetirla.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Recopila y vincula las evidencias actuales; sin reparación ni programación.
- `storage-sense-policy`: Directiva reconocida de limpieza del equipo, de solo lectura; se excluyen la configuración de usuario y las identidades de archivos.
- `storage-sense-task`: Estado exacto y historial de ejecución de la tarea, de solo lectura; sin ejecutar tareas ni limpieza.
- `storage-service-events`: Lee el historial exacto de fallos de Servicio de almacenamiento, Optimizar unidades, VSS y el proveedor de instantáneas de software.
- `volume-capacity DRIVE:`: Espacio disponible, de solo lectura, con evidencias JSON.
- `volume-dirty DRIVE:`: Estado del bit de volumen sucio, de solo lectura; sin reparación ni reinicio.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Análisis de solo lectura de forma predeterminada; requiere un volumen exacto en buen estado y un medio compatible.
- `volume-optimize-recover`: Verifica una operación pendiente completada sin repetirla.
- `vss-providers`: Nombres, versiones y tipos de los proveedores de VSS, de solo lectura; se excluyen los identificadores; sin cambios de registro.
- `vss-writers`: Estados indicados de los escritores; interpretación de la salida en inglés; sin restablecimiento.

## Impresión

- `print-events`: Historial de administración de PrintService de los últimos siete días, de solo lectura; evidencias JSON.
- `print-extension-files`: Resumen de presencia y firma de los binarios de procesadores y monitores de impresión, de solo lectura; se excluyen las rutas completas.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Vista previa de forma predeterminada; solo un trabajo exacto en estado Error o Bloqueado.
- `print-job-cancel-recover [--folder PATH]`: Vuelve a comprobar una cancelación interrumpida sin repetirla.
- `print-job-list`: Enumera los trabajos visibles actuales sin cambiarlos.
- `print-operational-events`: Estado del canal operativo de PrintService y el historial disponible, de solo lectura; evidencias JSON.
- `print-policy`: Restricciones de Point and Print y directiva de instalación de controladores, de solo lectura; evidencias JSON.
- `print-rendering-policy`: Directiva de procesamiento y de aislamiento de controladores, de solo lectura; evidencias JSON.
- `print-spooler-events`: Fallos del Administrador de control de servicios vinculados a la identidad local de Cola de impresión, de solo lectura; evidencias JSON.
- `printer-check --queue NAME [--queue NAME ...]`: Configuración de la cola local seleccionada, de solo lectura.
- `printer-conditions`: Códigos de estado de las colas locales, de solo lectura; sin verificación física ni cambios.
- `printer-driver-files`: Resumen de presencia de los archivos registrados y de la firma de los ejecutables, de solo lectura; se excluyen las rutas completas.
- `printer-driver-models`: Resumen de versión 3, versión 4 y compatibilidad con paquetes, de solo lectura; evidencias JSON.
- `printer-drivers`: Inventario y coincidencia de registros de colas y controladores, de solo lectura; evidencias JSON.
- `printer-endpoint-check`: Conexiones delimitadas a los extremos exactos asignados de impresoras TCP/IP estándar; sin detección, SNMP ni datos de impresión.
- `printer-list`: Descubre las colas locales sin cambiar la configuración.
- `printer-ports`: Coincidencia de registros de colas y puertos, de solo lectura; evidencias JSON.
- `printer-queue-attributes`: Atributos de cola de impresión, retención y uso compartido de las colas locales, de solo lectura.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Vista previa de forma predeterminada; desactivar el modo sin conexión puede liberar los trabajos en espera.

## Red

- `dhcp-check`: Elegibilidad de concesiones DHCP que faltan o han caducado, de solo lectura; evidencias JSON, sin renovación.
- `dhcp-events`: Historial de eventos del cliente DHCP para IPv4 e IPv6, de solo lectura; resultado JSON.
- `dhcp-leases`: Evidencias de marca de tiempo, caducidad y servidor de la concesión, de solo lectura; resultado JSON.
- `dhcp-renew [--execute]`: Vista previa de forma predeterminada; solo concesiones DHCP exactas que faltan o han caducado en adaptadores físicos conectados.
- `dhcp-renew-recover`: Conciliación de solo lectura sin solicitar otra concesión.
- `dns-cache HOST [--execute]`: Vista previa de forma predeterminada; vaciado protegido de la caché local con verificación del host exacto.
- `dns-cache-recover`: Vuelve a comprobar una operación de caché interrumpida sin repetir el vaciado.
- `dns-cache-summary`: Estado de la caché DNS local y recuentos por tipo de registro, limitados por privacidad; sin cambios.
- `dns-check HOST [--timeout-seconds 1..60]`: Resolución de nombres del sistema delimitada; resultado JSON.
- `dns-client-config`: Evidencias de sufijos, registro, devolución y directivas, de solo lectura; resultado JSON.
- `dns-client-events`: Historial de advertencias y fallos del cliente DNS limitado por privacidad, de solo lectura; resultado JSON.
- `dns-nrpt-policy`: Evidencias de la directiva efectiva de espacios de nombres DNS, de solo lectura; resultado JSON.
- `ip-addresses`: Revisión del estado, el origen y los conflictos de las direcciones IP actuales, de solo lectura; evidencias JSON.
- `ip-interface-config`: Evidencias de configuración de las interfaces IP, de solo lectura; resultado JSON.
- `network-firewall`: Relaciona las asignaciones de red activas con la activación del perfil de firewall correspondiente.
- `network-offload`: Configuración global de descarga de red de Windows, de solo lectura; resultado JSON.
- `network-profiles`: Categorías actuales de los perfiles y clasificaciones de conectividad, de solo lectura; se excluyen los nombres de red.
- `network-service-events`: Lee el historial exacto de fallos de DHCP, DNS, Estación de trabajo, NLA, Servicio de lista de redes y Aplicación auxiliar IP.
- `proxy-check`: Evidencias de proxy de WinHTTP guardadas y de Internet de la cuenta actual, de solo lectura.
- `route-priority`: Comparación de la métrica efectiva de la ruta predeterminada, de solo lectura; evidencias JSON.
- `smb-client-config`: Configuración de firma, acceso de invitados y transporte de SMB, de solo lectura.
- `smb-connections`: Negociación de protocolo activa, de solo lectura; se excluyen las identidades remotas.
- `smb-server-config`: Configuración de protocolo, firma, cifrado y multicanal de SMB, de solo lectura.
- `smb-shares`: Inventario delimitado de recursos compartidos SMB, de solo lectura; se excluyen las rutas, los permisos, las sesiones y las cuentas.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Comprobación de conexión delimitada; no se envía ningún dato de aplicación.
- `tcp-port-pressure`: Intervalos TCP dinámicos y uso agregado de puertos locales, de solo lectura; se excluyen los extremos y los procesos.
- `tcp-settings`: Evidencias de configuración de las plantillas TCP de Windows, de solo lectura; resultado JSON.
- `vpn-profiles`: Configuración de perfiles VPN de la cuenta actual, de solo lectura; se excluyen las credenciales.
- `winrm-listeners`: Resumen del registro de los agentes de escucha de WinRM, de solo lectura; se excluyen las huellas digitales de certificados.
- `winsock-catalog`: Estructura del catálogo nativo de protocolos de Winsock, de solo lectura; resultado JSON.
- `winsock-namespaces`: Estructura nativa de proveedores de espacios de nombres de Winsock, de solo lectura; resultado JSON.
- `winsock-provider-files`: Presencia y firmas de los archivos de proveedores de protocolos, de solo lectura; resultado JSON.
- `winsock-readiness`: Disponibilidad nativa de protocolos, proveedores de espacios de nombres y archivos de proveedores de Winsock, de solo lectura; resultados JSON.

## Apps

- `app-folder-review FOLDER_NAME [--report]`: Revisión de carpetas exactas con raíz fija; sin eliminación.
- `app-package-review FULL_NAME [--all-users]`: Destino exacto y paquetes dependientes; sin eliminación.
- `app-packages [--all-users]`: Solo inventario de Appx/MSIX registrados; sin eliminación.
- `browser-extensions`: Revisión de identidades de extensiones de Chrome, Edge, Brave y Firefox; sin eliminación.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Solo revisión; sin cambios en el sistema.
- `depot-review [application and configuration selections] [--json]`: Solo revisión; sin aprovisionamiento.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Comprobación de Authenticode y del firmante del fabricante, de solo lectura; no se ejecuta nada.
- `foxit-inventory`: Inventario de productos del equipo, de solo lectura; salida JSON.
- `installed-apps [--current-user]`: Registros de desinstalación y marcas de capacidad; sin eliminación.
- `installed-apps-compare BEFORE.json AFTER.json`: Diferencias de registro respecto a un inventario guardado; sin cambios en el equipo.
- `installed-hotfixes`: Inventario delimitado de revisiones de Windows instaladas, de solo lectura; evidencias JSON.
- `installer-activity`: Evidencias de procesos de Installer y marcadores de transacciones, de solo lectura; evidencias JSON.
- `installer-check`: Directiva de Installer y disponibilidad del motor, de solo lectura; evidencias JSON.
- `installer-engine-files`: Resumen de presencia y Authenticode de los archivos necesarios de Installer, de solo lectura; evidencias JSON.
- `installer-events`: Historial de MSI de los últimos siete días, de solo lectura; evidencias JSON.
- `installer-logging`: Directiva de registro y depuración de Installer, de solo lectura; evidencias JSON.
- `installer-pending-state`: Correlación entre operaciones de archivo pendientes y actividad de Installer, de solo lectura; evidencias JSON.
- `installer-registration-repair [--execute]`: Vista previa de forma predeterminada; solo un defecto exacto de registro del servicio; nunca anula el registro del motor.
- `installer-registration-repair-recover`: Vuelve a comprobar un registro interrumpido sin repetir los comandos.
- `installer-security-policy`: Directiva de seguridad y de revisiones de Installer, de solo lectura; evidencias JSON.
- `installer-service-events`: Fallos del Administrador de control de servicios vinculados a la identidad local de Windows Installer, de solo lectura; evidencias JSON.
- `installer-service-registration`: Resumen del registro del servicio Windows Installer, de solo lectura; evidencias JSON.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: Vista previa reciente de la elegibilidad exacta; sin proveedor de eliminación.
- `mace-package-recover`: Concilia de nuevo un registro interrumpido de eliminación de un paquete de app; nunca repite la eliminación.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Solo revisión; sin eliminación.
- `msi-cache`: Visibilidad de la caché registrada de Windows Installer, de solo lectura; evidencias JSON.
- `msi-code NONNEGATIVE_DECIMAL`: Referencia documentada de códigos de retorno de Windows Installer; sin inspección del equipo ni reparación.
- `msi-product {PRODUCT-CODE}`: Estado exacto del registro de MSI, de solo lectura; evidencias JSON.
- `msi-repair {PRODUCT-CODE} [--execute]`: Vista previa de forma predeterminada; solo un producto exacto dañado o con mala configuración.
- `msi-repair-recover`: Vuelve a comprobar la reparación interrumpida de un producto exacto sin repetirla.
- `msi-sources`: Metadatos de origen registrados de Windows Installer limitados por privacidad, de solo lectura; evidencias JSON.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Vista previa de forma predeterminada; conserva las cachés exactas de la cuenta en ejecución con nombres de recuperación.
- `orca-cache-reset-recover`: Verifica la conservación interrumpida de la caché sin repetir ningún movimiento.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Solo revisión; sin cambios en Outlook.
- `startup-apps`: Revisión de los registros de inicio y de las tareas de arranque o inicio de sesión; sin deshabilitar nada.
- `third-party-update ID SOURCE VERSION [--accept-agreements] [--execute]`: Vista previa de forma predeterminada; solo una versión exacta.
- `third-party-update-recover`: Verifica una versión exacta pendiente sin reintentar la actualización.
- `third-party-updates`: Descubrimiento de actualizaciones de aplicaciones con Microsoft.WinGet.Client, de solo lectura; sin cambios en las aplicaciones.

## Sistema

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Búsqueda local exacta de asignaciones del Bluetooth SIG; sin solicitudes de red.
- `crash-config`: Modo y rutas de volcado guardados de Windows; sin cambios.
- `crash-dumps`: Metadatos de los archivos de volcado predeterminados de Windows; sin análisis del contenido ni cambios.
- `crash-events`: Informes recientes de errores del sistema; sin diagnóstico ni cambios.
- `crash-review [--knowledge PACK_PATH ...]`: Hasta 32 paquetes; revisión combinada de bloqueos, apagados, estructura delimitada de minivolcados y, opcionalmente, la referencia exacta de comprobación de errores; sin cambios.
- `default-printer`: Selección predeterminada y configuración de administración automática, de solo lectura; evidencias JSON.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Vista previa de forma predeterminada; solo si la predeterminada falta o está en conflicto.
- `default-printer-set-recover [--folder PATH]`: Vuelve a comprobar una selección interrumpida sin repetirla.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: Evidencias de dispositivos de solo lectura con referencias exactas opcionales de controladores de hasta 32 paquetes.
- `device-code DECIMAL_OR_0xHEX`: Referencia sin conexión de los códigos de problema del Administrador de dispositivos.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Solo candidatos de referencia.
- `event-channel-enable CHANNEL [--execute]`: Vista previa de forma predeterminada; habilita un único canal de diagnóstico exacto no administrado y conserva el resto de la configuración.
- `event-channel-enable-recover`: Conciliación de solo lectura sin repetir el comando de habilitación.
- `event-channels`: Estado, retención, tamaño y saturación de cuatro canales de diagnóstico, de solo lectura.
- `font-registrations`: Comprobación delimitada de registros y presencia de archivos del equipo y del usuario actual, de solo lectura.
- `memory-diagnostic-results`: Historial delimitado del Diagnóstico de memoria de Windows, de solo lectura; evidencias JSON.
- `minidump-analyze PATH`: Metadatos estructurales delimitados de minivolcados; sin páginas de memoria, símbolos ni cambios.
- `name-rename PROPOSED_NAME [--execute]`: Vista previa de forma predeterminada; solo PC independientes de grupo de trabajo; nunca reinicia Windows.
- `name-rename-recover`: Verifica un cambio de nombre pendiente sin repetirlo.
- `name-review PROPOSED_NAME`: Revisión de la identidad y del nombre propuesto, de solo lectura.
- `profiles --recover`: Verifica la ausencia registrada y conserva las evidencias de recuperación; nunca reintenta la eliminación.
- `profiles --recover-backup`: Verifica y archiva un artefacto de copia de seguridad pendiente; nunca reintenta la copia.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Vista previa primero; la copia nativa protegida requiere ejecución explícita y un destino NTFS local que no exista.
- `profiles --select SID --create-backup-artifact DESTINATION`: Compara una copia de seguridad ya copiada y crea su artefacto fijo; no copia ni quita ningún perfil.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Revisión de solo lectura; la verificación requiere un SID seleccionado.
- `task-check`: Inventario de tareas, hallazgos clasificados y evidencias JSON, de solo lectura.
- `task-definition TASK_FOLDER TASK_NAME`: XML exacto de la tarea, de solo lectura; no disponible no significa ausente.
- `task-inventory`: Identidades y estados de las tareas, de solo lectura; salida JSON.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Revisión histórica de la recuperación; nunca autoriza un reintento.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Valida y revisa un borrador de tarea; sin registro.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Vista previa de forma predeterminada; definición deshabilitada de solo creación con Sistema local.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Conciliación de solo lectura de la definición exacta; nunca reintenta ni ejecuta una tarea.
- `task-xml PLAN.json YYYY-MM-DD`: XML de revisión deshabilitado en la salida estándar; solo programaciones de calendario en la zona horaria local; sin registro.
- `time-config`: Evidencias del modo, los pares y la directiva de Hora de Windows, de solo lectura.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Vista previa de forma predeterminada; el modo personalizado requiere pares.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Solo vista previa.
- `time-recover --folder PATH`: Verifica y archiva un registro pendiente; nunca reintenta los cambios.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Solo lectura; el modo personalizado requiere pares.
- `wmi-provider`: Consulta local del proveedor del sistema operativo, de solo lectura; evidencias JSON.

## Conocimiento, validación y Engine

- `knowledge PACK_PATH CONTEXT CODE`: Búsqueda de referencia local; sin ejecutar reparaciones.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Activación local verificada; sin descarga.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Verifica el paquete local activo; sin cambios.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Solo cambios de referencia; sin instalar paquetes.
- `knowledge-info PACK_PATH`: Metadatos del paquete y recuentos de entradas por contexto; sin cambios.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Adjunta referencias contextuales a los códigos observados.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Selecciona el paquete conservado verificado más reciente; sin descarga.
- `knowledge-search PACK_PATH CONTEXT TEXT`: Hasta 50 coincidencias de texto de referencia; sin ejecutar reparaciones.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Resuelve una identidad exacta en hasta 32 paquetes; nunca se selecciona un conflicto.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Verifica todas las bases de datos instaladas configuradas; sin descarga ni activación.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Descarga y activa de forma atómica bases de datos candidatas firmadas desde los puntos de conexión HTTPS configurados.
- `open <file.kbench> [--json]`: Vuelve a abrir una captura guardada y la evalúa de nuevo con las reglas instaladas.
- `scan --output <new-file.kbench> [--json]`: Captura de Windows Update de solo lectura guardada en un archivo nuevo.
- `validation-coverage RECORD.json [...]`: Resume las evidencias nativas en VM de la versión actual; sin diagnósticos ni reparaciones.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Plan de escenarios en VM desechables, de solo lectura; sin diagnósticos ni reparaciones.
- `validation-record RECORD.json`: Valida un registro delimitado de evidencias nativas en VM; sin diagnósticos ni reparaciones.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Informe local de cobertura en VM de la versión actual.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Ejecuta un plan de opciones compartido validado de solo lectura.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Solo inventario.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Diario de diagnóstico específico; sin reparaciones.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Solo revisión registrada en el diario.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: Estado exacto opcional del producto incluido en el diario.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: Diagnóstico opcional del destino seleccionado incluido en el diario.
- `workflow storage [--volume D:] [--json]`: Comprobación del sistema de archivos de solo lectura registrada en el diario.
- `workflow-journal FILE.json`: Contenido histórico; la salida 0 significa que se leyó correctamente, no que esté en buen estado.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Muestra o valida controles independientes del host; sin análisis ni reparación.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Solo informe histórico; no se ejecuta ningún comando.

## Otros comandos

- `<analyze|investigate> <text-log-path> [--json]`: Analiza evidencias de texto locales.
- `adapter-bindings`: Resumen de los enlaces de protocolos principales, de solo lectura; evidencias JSON.
- `adapter-statistics`: Contadores de bytes, errores y descartes de los adaptadores, de solo lectura; evidencias JSON.
- `application-events`: Errores recientes de Aplicación, recuentos agrupados y evidencias JSON, de solo lectura.
- `applocker-policy`: Modos de recopilación efectivos y recuentos agregados de reglas, de solo lectura; se excluyen los detalles de las reglas; sin cambios.
- `common-check [--scope NAME ...]`: Todas las áreas de diagnóstico de forma predeterminada; solo las áreas seleccionadas cuando se especifican; JSON de solo lectura.
- `delete-notifications`: Configuración de notificación de eliminación de NTFS y ReFS, de solo lectura; JSON; sin optimización ni cambio de configuración.
- `diagnostic-scopes [--all]`: Enumera 11 grupos de resultados recomendados, o las 49 áreas de solo lectura; sin recopilación.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Diario de diagnóstico específico; sin reparaciones.
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Todas las áreas de diagnóstico; sin reparación salvo que se indique `--auto-repair` explícitamente.
- `gateway-check`: Comprobaciones ICMP delimitadas de los siguientes saltos predeterminados exactos configurados; sin barrido de direcciones.
- `infrastructure-service-events`: Lee una instantánea delimitada y exacta de fallos de los servicios de conectividad, administración remota, notificaciones y servicios principales.
- `investigate-batch FILE [FILE ...]`: Hasta 12 archivos; JSON Lines; salida 3 para importaciones fallidas o parciales.
- `licensing-check`: Estado de activación de Windows sin claves de producto, de solo lectura; evidencias JSON.
- `mac-vendor PACK_PATH MAC_ADDRESS`: Búsqueda en paquete local por prefijo más largo; sin solicitudes de red.
- `ncsi-policy`: Directiva de clasificación de conectividad de Windows, de solo lectura; se excluyen los destinos de sondeo.
- `neighbor-cache`: Evidencias de accesibilidad de vecinos limitadas por privacidad, de solo lectura; resultado JSON.
- `ntfs-self-healing DRIVE:`: Configuración de reparación de NTFS en línea del volumen seleccionado, de solo lectura; sin reparación ni cambio de configuración.
- `pagefile-check`: Resumen de la administración, asignación y uso del archivo de paginación, de solo lectura; se excluyen las rutas completas.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Búsqueda exacta en paquete local; sin solicitudes de red ni selección de controladores.
- `physical-disks`: Estado actual de salud, operativo, de medios, de bus y de uso, de solo lectura; JSON; sin reparación.
- `platform-service-events`: Lee una instantánea delimitada y exacta de fallos de los servicios de aplicaciones, identidad, perfiles y compatibilidad de dispositivos.
- `previous-windows [--execute --backup-confirmed]`: Vista previa de forma predeterminada; quita para siempre la capacidad de reversión de Windows mediante DISM.
- `previous-windows-recover`: Conciliación de solo lectura sin repetir la eliminación de la reversión.
- `provisioned-packages`: Paquetes preparados para usuarios nuevos en Windows local; sin eliminación.
- `recipes [WORKFLOW]`: Enumera las definiciones instaladas o las dependencias y límites de un flujo de trabajo concreto; sin ejecución.
- `repair <dism|sfc> [--execute]`: Vista previa de forma predeterminada.
- `reserved-storage`: Estado del almacenamiento reservado de Windows en línea, de solo lectura; evidencias JSON.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Vista previa de forma predeterminada; crea un punto tras comprobaciones actualizadas.
- `restore-point-recover`: Verifica una solicitud pendiente sin crear otro punto.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Solo vista previa.
- `shadow-copies`: Estados y atributos actuales de las instantáneas VSS, de solo lectura; se excluyen las identidades; sin cambios en las instantáneas.
- `shadow-storage`: Asignación y presión del área diferencial de VSS, de solo lectura; JSON; sin cambio de tamaño ni acción sobre instantáneas.
- `uac-policy`: Activación de UAC y directiva de solicitud de elevación, de solo lectura; sin datos de cuentas ni credenciales.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Búsqueda local exacta en el registro de UEFI; sin solicitudes de red ni selección de controladores.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Búsqueda exacta en paquete local; sin solicitudes de red ni selección de controladores.
- `vitals-check`: Instantáneas de Windows, hardware, cuenta, recursos, batería y discos físicos, de solo lectura; evidencias JSON.
- `win32-code DECIMAL_OR_0xHEX`: Búsqueda sin conexión de mensajes en la instalación de Windows.
- `workflows`: Enumera las claves y los títulos de los flujos de trabajo instalados; sin análisis ni reparación.
