# Seguridad

Seguridad inspecciona Microsoft Defender, el firewall, los certificados y los servicios de inicio de sesión. También muestra el estado del TPM y de la seguridad de la plataforma, del que dependen BitLocker y Windows Hello.

Guías paso a paso: [Cifrar una unidad con BitLocker](encrypt-with-bitlocker.html) y [Hacer Windows más privado](make-windows-private.html).

## Herramientas de esta sección

Inspecciona la protección, el firewall, los certificados y los servicios de inicio de sesión.

### Microsoft Defender

Inspecciona el estado de la protección y de las firmas de Defender. La reparación automática solo actualiza las firmas cuando Defender indica protección activa y firmas desactualizadas. Usa los orígenes de firmas configurados. No cambia la protección, las exclusiones ni las directivas; no hace análisis de malware.

### Centro de seguridad de Windows

Revisa de forma delimitada los registros de antivirus con el estado sin procesar del proveedor, el historial exacto de fallos del servicio y la disponibilidad actual de Centro de seguridad de Windows, sin interpretar el estado de la protección. Un inventario vacío no es un veredicto sobre el estado de la protección, y el número de registros, el estado sin procesar o los fallos históricos no pueden autorizar una recuperación ni un cambio de seguridad. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. La supervisión del estado de seguridad y las notificaciones existentes pueden reanudarse. Se conservan la configuración del antivirus, los registros de proveedores, la configuración de protección, las exclusiones, las notificaciones, las directivas y el tipo de inicio. No se solicita ningún análisis, actualización ni cambio de protección. La verificación abarca solo el estado estable del servicio, no la presencia de antivirus, el estado de la protección, la actualidad de las firmas, la entrega de notificaciones, la propiedad de los registros ni la directiva efectiva.

### Servicios de firewall

Lee la configuración de activación de los perfiles Dominio, Privado y Público en ActiveStore y después comprueba los servicios Motor de filtrado de base y Firewall de Windows Defender. La recuperación opcional solo inicia servicios habilitados y detenidos tras comprobaciones de dependencias actualizadas. La directiva de filtrado existente puede entrar en vigor e interrumpir el acceso a la red o remoto. Se conservan los tipos de inicio, los permisos de los servicios, las reglas del firewall, los perfiles y la configuración de IPsec. No hay restablecimientos, cambios de reglas ni detenciones de servicios. La verificación solo establece que el servicio está en ejecución; la protección efectiva y la conectividad siguen sin verificar.

### Caducidad de certificados

Inspecciona las fechas de los certificados y crea cadenas de confianza locales solo con la caché para los almacenes personales del equipo y del usuario actual, y devuelve una única conclusión operativa delimitada. La revocación, la presentación en servidores, los enlaces, la inscripción, la propiedad de las directivas, la confianza remota y la utilidad de la clave privada siguen sin verificar. No se cambia ningún certificado, configuración de confianza, inscripción, enlace, clave privada, aplicación, servicio ni directiva.

### Administrador de credenciales

Comprueba la disponibilidad del servicio Administrador de credenciales. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las aplicaciones existentes pueden reanudar las solicitudes de credenciales guardadas. Se conservan el contenido del almacén, las contraseñas, las claves de acceso, los certificados, los tokens, los registros de credenciales, los permisos, las directivas, los permisos del servicio y el tipo de inicio. No se solicita ningún contenido ni operación de credenciales. La verificación abarca solo el estado estable del servicio, no la recuperación de credenciales, la autenticación ni el acceso de las aplicaciones.

### Administrador de cuentas web

Revisa el historial del agente de cuentas con la carga útil anonimizada y la disponibilidad exacta de Administrador de cuentas web. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las aplicaciones existentes pueden reanudar las solicitudes de cuentas y tokens según la directiva actual. Los eventos históricos no pueden autorizar una recuperación ni establecer el estado actual de las cuentas. Se conservan las cuentas, las credenciales, los tokens, las cookies, los registros de aplicaciones, los permisos, el estado de inicio de sesión, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de cuenta, credencial, token, cookie, permiso ni inicio de sesión. La verificación abarca solo el estado estable del servicio, no la autenticación, la emisión de tokens ni el acceso de las aplicaciones.

### Ayudante para el inicio de sesión de cuenta Microsoft

Revisa el historial del agente de cuentas con la carga útil anonimizada y la disponibilidad exacta de Ayudante para el inicio de sesión de cuenta Microsoft. La recuperación opcional solo inicia el servicio exacto habilitado y detenido tras comprobaciones de dependencias actualizadas. Las solicitudes existentes de inicio de sesión con cuenta Microsoft pueden reanudarse según la directiva actual. Los eventos históricos no pueden autorizar una recuperación ni establecer el estado actual de las cuentas. Se conservan las cuentas, las credenciales, los tokens, el registro del dispositivo, los datos de aplicaciones, los permisos, el estado de inicio de sesión, las directivas, los permisos del servicio y el tipo de inicio. KillerBench no selecciona ninguna operación de cuenta, credencial, token, registro, permiso ni inicio de sesión. La verificación abarca solo el estado estable del servicio, no la autenticación, la conectividad de la cuenta ni el acceso de las aplicaciones.
