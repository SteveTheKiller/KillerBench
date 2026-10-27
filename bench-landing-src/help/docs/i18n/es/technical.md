# La tecnología de KillerBench

Un vistazo por dentro a cómo KillerBench lee un equipo Windows, mantiene honestas sus evidencias y decide cuándo se permite un cambio.

## Pila tecnológica

KillerBench es una app nativa de Windows. La app de escritorio, la línea de comandos y el Engine que tienen debajo comparten las mismas evidencias de diagnóstico y los mismos flujos de reparación.

| Componente | Detalle |
| --- | --- |
| Interfaz | WPF sobre .NET 10 (`net10.0-windows`), Windows x64, marco de ventana personalizado |
| Engine | `Engine.Core`, `Engine` y `Engine.Windows`, sin ninguna referencia a WPF ni a controles de escritorio |
| Línea de comandos | `killerbench-cli`, un host independiente sobre el mismo Engine |
| Proveedores de Windows | .NET, API de Windows, PowerShell y herramientas de línea de comandos de Windows, cada uno ejecutado con límites fijos de tiempo, registros y tamaño |
| Informes | Salida sin procesar, Informe legible, HTML, texto y JSON |
| Datos de referencia | Paquetes de conocimiento con versión para códigos de error, identificadores de hardware, fabricantes y referencias de soporte |
| Empaquetado | Instalador: dependiente del framework, necesita .NET Desktop Runtime 10 para Windows x64. Portátil: autocontenido, sin instalar runtime |
| Apariencia | 13 temas de la familia y 20 idiomas |

## Instalación, carpetas y datos

KillerBench se distribuye de dos formas. El instalador es la descarga estándar y admite una instalación por usuario o para todos los usuarios. La edición portátil lleva su propio runtime de .NET y se ejecuta sin instalación.

KillerBench se ejecuta con los derechos de la cuenta que lo inició. No se eleva por sí mismo. Inícialo con **Ejecutar como administrador** cuando las evidencias protegidas, como los protectores de BitLocker, o una reparación necesiten acceso de administrador. La elevación nunca selecciona ni aprueba una reparación.

### Qué hace el instalador

El instalador verifica y extrae la aplicación en `%LOCALAPPDATA%\Programs\KillerBench\`, o en `%ProgramFiles%\KillerBench\` tras una solicitud de elevación para todos los usuarios. Añade un acceso directo en el menú Inicio, un acceso directo opcional en el escritorio y una entrada en Agregar o quitar programas con desinstalación normal y silenciosa. La edición portátil verifica y extrae su contenido en `%LOCALAPPDATA%\KillerBench\Portable\` para la ejecución actual y limpia las copias abandonadas en el siguiente inicio.

### Dónde está cada cosa

| Ubicación | Contiene |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Tema, color de acento, escala de la interfaz e idioma |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | Un diario JSON por cada ejecución de flujo de trabajo: decisiones, salida delimitada de comandos y resultados de reparaciones |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | Diarios de ejecución de reparaciones de mantenimiento como DISM y SFC |
| Carpetas de características en `%LOCALAPPDATA%\KillerBench\` | Protecciones pendientes y resultados de cada acción protegida, por ejemplo `BitLockerBackup`, `Time`, `Firewall` y `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | El nivel de reparación de KillerMCP elegido en Acerca de |
| `HKCU` o `HKLM\Software\KillerBench` | La ruta instalada, según el ámbito de la instalación |
| Un archivo `.kbench` que elijas | Una captura guardada con sus evidencias conservadas |

Los diarios se quedan en este equipo y KillerBench no los carga. Elimina un diario solo cuando ya no necesites las evidencias de un resultado incierto.

## Primero el diagnóstico

KillerBench trata el diagnóstico y el cambio como tareas distintas. El diagnóstico solo lee. Un cambio solo ocurre cuando eliges una reparación o acción concreta, revisas sus opciones y requisitos previos y la confirmas.

El análisis completo del sistema es solo de diagnóstico, también cuando un flujo de trabajo específico tiene activada la reparación automática. Las capturas guardadas, los registros importados y los eventos históricos nunca autorizan una reparación.

### Recopilación delimitada

Cada comando nativo y cada proveedor de Windows se ejecuta con límites fijos de tiempo, registros y tamaño. Un proveedor registra su estado de salida, el tiempo de espera agotado, el truncamiento, la salida estándar y la salida de errores. Un proveedor que falla o se trunca nunca puede leerse como una respuesta limpia y vacía.

### Conservación mínima

Los recopiladores guardan lo que necesita un hallazgo. Muchos resultados conservan nombres, categorías, recuentos, estados y marcas de tiempo, y dejan fuera credenciales, líneas de comandos, rutas completas, identidades de cuentas, extremos de red y cargas útiles de eventos no relacionadas.

## Evidencias que puedes seguir

Cada hallazgo está vinculado a las evidencias que lo produjeron, y el informe siempre separa lo que se demostró de lo que solo se observó.

| Estado | Significado |
| --- | --- |
| Por revisar | Las evidencias respaldan un problema que merece atención. |
| Información | Se recopilaron evidencias y se muestran como contexto. |
| Incompletas o Desconocido | Una comprobación no pudo terminar o la evidencia no estaba disponible. Nunca cuenta como buen estado. |
| Histórico | Un evento del pasado. Explica el historial, pero no demuestra un fallo actual. |
| Verificado | Una reparación o un cambio se confirmó volviendo a leer el resultado. |

### Desconocido es un resultado

Desconocido significa que se denegó el acceso, faltaba un origen, un proveedor falló, la salida estaba mal formada, se superó un límite o las evidencias no coincidían. KillerBench muestra esa limitación y el siguiente paso útil en lugar de ocultarla.

### El historial sigue siendo historial

Los registros de eventos y los registros importados describen lo que se anotó en un momento pasado. Pueden apuntar a un código de error o al siguiente ámbito de diagnóstico. No demuestran que el fallo siga activo.

## Reparaciones protegidas

Las reparaciones son operaciones tipadas con reglas de elegibilidad, ejecución, verificación y recuperación. No son cadenas de comandos sacadas de una línea de registro. Cada reparación compatible sigue la misma secuencia:

1. Recopilar evidencias actuales completas.
2. Coincidir con una receta compatible.
3. Revisar las opciones necesarias y sus consecuencias.
4. Confirmar los requisitos previos administrativos y de directivas.
5. Volver a recopilar las evidencias decisivas justo antes del cambio.
6. Escribir un registro de preparación duradero.
7. Ejecutar una única acción nativa delimitada.
8. Recopilar evidencias de verificación recientes.
9. Registrar un resultado verificado, bloqueado, detenido o incierto.

Si las evidencias decisivas cambian antes de que se ejecute la acción, KillerBench se detiene sin realizarla.

### Opciones explícitas

Las reparaciones que dependen de una opción necesitan que elijas un valor exacto, como un volumen, una cola de impresión, el destino de la clave de recuperación, un valor de energía, la versión de un paquete o un origen de reparación. KillerBench nunca se inventa esos valores.

### Equipos administrados

La directiva de grupo y la administración de dispositivos pueden hacer que un cambio local no surta efecto o no sea adecuado. Las reparaciones compatibles inspeccionan quién controla una configuración y se detienen cuando ese control es administrado o desconocido. La configuración que pertenece a una directiva no se toca.

### Reparación automática

La reparación automática solo deja continuar a las recetas elegibles. No se salta las evidencias, no elige opciones que faltan, no ignora las directivas, no reintenta una acción incierta, no reinicia Windows ni convierte en reparación un hallazgo que necesita revisión.

## Diarios e interrupciones

Antes de que KillerBench pida a Windows un cambio, anota lo que va a hacer. Cada cambio se guarda en un diario en `%LOCALAPPDATA%\KillerBench`.

Después del cambio, KillerBench vuelve a leer el resultado y solo indica **Verificado** cuando Windows muestra el estado esperado. Si Windows informa de éxito pero el estado no cambió, el resultado lo dice y no se repite nada.

Un proceso puede detenerse después de que Windows reciba una solicitud y antes de que termine la verificación. El registro pendiente se conserva, y una conciliación posterior lee el estado actual para averiguar qué ocurrió. La conciliación no es una reversión automática y nunca repite el cambio original. Conserva el diario cuando el resultado sea incierto.

## Constantes y límites

| Especificación | Valor |
| --- | --- |
| Cobertura de diagnóstico | El análisis completo del sistema recorre 49 áreas de diagnóstico y comprueba 96 recetas de reparación protegidas |
| Lectura de orígenes de texto | 1.000.000 caracteres por origen, 2000 líneas coincidentes mostradas |
| Fuentes de sesión | 12 en la barra lateral; una captura admite hasta 24 orígenes y 64 MB |
| Consultas de eventos | Los 200 eventos más recientes por canal de los últimos 7 días |
| Análisis de Windows Update | 15 segundos por origen, dos minutos en total |
| Investigación de registros | 10 000 entradas analizadas, 256 códigos distintos, 8 fragmentos de 320 caracteres por código |
| Archivo CBS comprimido | Archivo de 256 MiB, miembro de registro de 512 MiB, extracción de 30 segundos |
| CHKDSK de solo lectura | Diez minutos; una reparación está limitada a 65 minutos |
| Comprobaciones DNS y TCP | Diez segundos de forma predeterminada, un minuto como máximo |
| Inventario de servicios | Hasta 1024 servicios instalados |
| Paquetes de conocimiento | Hasta 32 abiertos a la vez |
| Revisión de archivos grandes | Archivos de al menos 100 MB |
| Monitor de actividad de mantenimiento | Intervalo de 1 a 60 segundos, de 1 a 120 instantáneas |
| Borradores de tareas programadas | Límite de ejecución de 1 a 4320 minutos |
| Orígenes de hora personalizados | Hasta ocho pares NTP |
| Exportación de actividad | Hasta 150 entradas conservadas |
| Idiomas y temas | 20 idiomas, 13 temas con variantes de acento |

## Lo que no hará

- Borrar un TPM, eliminar protectores que no creó ni mostrar contraseñas de recuperación.
- Cambiar una configuración que controlan la directiva de grupo o la administración de dispositivos.
- Ejecutar una reparación solo porque un evento histórico o un informe importado mencione un problema.
- Convertir evidencias que faltan, parciales o fallidas en un veredicto de buen estado.

## Informes

Cada ejecución produce un informe que puedes leer, filtrar y exportar. Un informe separa:

1. Qué se inspeccionó
2. Qué problema se encontró
3. Qué evidencias respaldan el hallazgo
4. Qué acción se propuso o se realizó
5. Cómo se verificó el resultado
6. Qué sigue siendo desconocido o necesita un reinicio

**Salida sin procesar** conserva las evidencias técnicas con sus números de línea originales para que puedas seguir cualquier hallazgo hasta su origen. El **Informe legible** resume la misma ejecución en lenguaje sencillo, con el siguiente paso seguro en primer lugar. Los informes HTML usan el sistema de temas de Killer Tools y se leen bien sin la app. El texto prioriza la portabilidad, y JSON conserva los campos estructurados para otro programa.

## Registros guardados y capturas

KillerBench puede investigar evidencias sin ejecutar nada en el equipo. Suelta un registro de texto en la ventana, o pega su ruta y elige **Abrir registro**. Las líneas de CBS, CSI y Setup se analizan con sus marcas de tiempo, y los hallazgos enlazan con la línea original exacta. Los archivos EVTX y XML de eventos se pueden importar del mismo modo.

**Analizar Windows Update** es una recopilación de solo lectura y bajo demanda de los registros de CBS, DISM, Panther y de reversión de Setup, los canales de eventos de actualización y mantenimiento, los estados de los servicios de actualización, el espacio libre, el tiempo de actividad, los indicadores de reinicio pendiente y la directiva de actualización seleccionada. **Guardar captura** escribe las evidencias conservadas en un archivo `.kbench`. **Abrir captura** lo vuelve a abrir sin los archivos originales y lo evalúa de nuevo con las reglas instaladas, de modo que las mismas reglas dan los mismos hallazgos.

## Modo de demostración

Inicia `KillerBench.exe --demo` para probar **Ejecutar diagnóstico** en una estación de trabajo ficticia. Cada flujo de trabajo produce evidencias sintéticas y un progreso temporizado. No se llama a ningún proveedor de Windows, no se ejecuta ningún comando de reparación y no se escribe ningún diario. Los botones de herramientas independientes, fuera de **Ejecutar diagnóstico**, siguen activos, así que no los uses como acciones de demostración.

`KillerBench.exe --demo-screenshot` abre un informe de Análisis completo del sistema de solo visualización para un equipo ficticio fijo. Se bloquean los clics en el espacio de trabajo, los atajos y la acción de soltar archivos.

## Paquetes de conocimiento

Los datos de referencia grandes y que se actualizan a menudo están en paquetes de conocimiento, no en la lógica de diagnóstico: códigos de error de Windows y Setup, identificadores de fabricantes y dispositivos, referencias PCI, USB, ACPI y UEFI, datos de fabricantes por OUI de red, ubicaciones de controladores y soporte, y enlaces de soporte seleccionados.

Cada paquete identifica su formato, la versión del esquema, el origen, la revisión del origen, la hora de generación y el hash del contenido, y KillerBench los valida antes de usarlos. Un paquete desconocido o no válido sigue sin estar disponible en lugar de recurrir a una suposición. Una actualización se descarga en una ubicación temporal, se valida y después sustituye al paquete activo en un solo paso, así que una actualización fallida deja el paquete anterior en su sitio.

Los datos de conocimiento pueden explicar un identificador o sugerir dónde mirar a continuación. No pueden convertir un código histórico en un fallo actual, elegir un controlador automáticamente ni autorizar una reparación.

## Actualizaciones y firma

Al iniciarse, KillerBench pide a GitHub la última versión estable, con un límite de cuatro segundos, y solo ofrece una actualización cuando esa versión es más reciente. Puedes desactivar la comprobación con **Buscar actualizaciones al iniciar** en Acerca de, o en el aviso de actualización. La descarga es el paquete que corresponde a la copia en ejecución, instalador o portátil, y su SHA-256 debe coincidir con el `SHA256SUMS.txt` de la versión antes de instalar nada. La app se cierra y se vuelve a abrir para terminar. No hay ningún servicio en segundo plano.

La tarjeta Acerca de muestra la versión, el editor y la huella digital de la firma de código, y el SHA-256 del ejecutable en ejecución, para que puedas comprobar la copia que tienes.

## KillerMCP

KillerBench no incluye ningún agente de IA. [KillerMCP](../mcp.html) puede conectar uno al mismo Engine. Las **Opciones de KillerMCP** de Acerca de deciden qué puede hacer ese agente:

| Configuración | Efecto |
| --- | --- |
| Desactivado (predeterminado) | El agente no puede solicitar ninguna reparación. |
| Permitir reparaciones seguras | El agente puede solicitar las reparaciones clasificadas como seguras. |
| Permitir también reparaciones disruptivas | Las reparaciones disruptivas pasan a estar disponibles, y cada una sigue necesitando aprobación. |

Solo una persona delante de este equipo puede subir el nivel. Tras un resultado de reparación incierto, KillerMCP escribe un bloqueo, y las reparaciones del agente siguen bloqueadas hasta que alguien lo quita en Acerca de.

## Localización

La interfaz está disponible en 20 idiomas: inglés, bengalí, checo, alemán, español, francés, húngaro, italiano, japonés, kazajo, noruego, polaco, portugués (Brasil), ruso, esloveno, turco, ucraniano, vietnamita, y chino simplificado y tradicional. Un cambio de idioma se aplica al instante, incluidos los nombres de las secciones y de los flujos de trabajo, el texto de bienvenida y un informe legible abierto. Cualquier texto que falte se muestra en inglés.

Las evidencias, los hallazgos de Engine, los diarios y la Salida sin procesar siguen en inglés, igual que un registro de Windows, para que un informe se lea igual en cualquier equipo y se pueda buscar texto exacto.

## Línea de comandos y códigos de salida

`killerbench-cli` ejecuta el mismo Engine sin la app de escritorio. El progreso va a la salida de errores estándar y el JSON a la salida estándar. La mayoría de los comandos muestran una vista previa de forma predeterminada, y un cambio necesita `--execute`. Los comandos `-recover` correspondientes revisan una acción interrumpida sin repetirla. Consulta [Ejecutar KillerBench desde scripts](use-the-cli.html) y la [referencia de comandos](cli.html).

| Código de salida | Comandos de flujo de trabajo | Análisis y apertura |
| --- | --- | --- |
| 0 | Ningún problema sin resolver en los ámbitos comprobados | Cobertura completada |
| 1 | Error de inicio o del diario | Fallo |
| 2 | Argumentos no válidos | Argumentos no válidos |
| 3 | Requiere atención | Recopilación parcial |
| 4 | Se requiere reinicio antes de la verificación | |
| 130 | Detenido | Cancelado o tiempo de espera total agotado |

Ningún código de salida certifica que un equipo esté en buen estado.

## Arquitectura

El Engine se divide en tres proyectos sin interfaz. `Engine.Core` aporta primitivas y recetas declarativas, `Engine` gestiona los flujos de trabajo compartidos y las evidencias, y `Engine.Windows` registra los proveedores nativos. La app de escritorio, la CLI y las pruebas son hosts independientes.

| Capa | Responsabilidad |
| --- | --- |
| Recopiladores de capacidades | Leen el estado delimitado de Windows para un ámbito exacto |
| Analizadores de evidencias | Validan la forma, la identidad, la integridad y los límites |
| Interpretación del diagnóstico | Convierte las evidencias en estados explícitos del flujo de trabajo |
| Orquestación de flujos de trabajo | Combina comprobaciones, progreso, recetas y diarios |
| Coordinadores de reparación | Vuelven a comprobar la elegibilidad, preparan, cambian, verifican y concilian |
| Informes | Presentan problemas, acciones, resultados y limitaciones |
| Paquetes de conocimiento | Resuelven datos de referencia con versión sin cambiar la lógica |

Los recopiladores fallan de forma cerrada cuando la identidad es ambigua, la salida está mal formada, se supera un límite o no se puede inspeccionar la autoridad necesaria. Hoy el Engine se dirige a Windows y no es multiplataforma. La [Guía de Engine para desarrolladores](../engine/index.html) trata la API, la CLI y la integración del Engine en otro host de .NET.

## Privacidad y red

Los diagnósticos y el análisis de registros se ejecutan en local, sin servicio de IA ni cuenta de KillerBench. Los diarios y las evidencias conservadas se quedan en las carpetas `%LOCALAPPDATA%\KillerBench` de la cuenta local.

Las acciones que selecciones pueden contactar con su destino, como Windows Update, servicios de descarga de software o la copia de la clave de recuperación en Active Directory o Entra ID. La comprobación de actualizaciones al iniciar contacta con GitHub, y las actualizaciones de conocimiento y el instalador de KillerMCP contactan con sus servicios configurados. Un cliente de IA que use [KillerMCP](../mcp.html) puede recibir el resultado de referencia que pidió.

Los registros sin procesar, los archivos importados y las evidencias exportadas pueden contener nombres de equipos, cuentas, rutas o texto de origen confidencial. Revísalos antes de compartirlos y nunca incluyas claves de recuperación, credenciales ni datos de clientes en un informe público.

## Estado de desarrollo

KillerBench está en desarrollo y todavía no hay descarga pública. La cobertura de reparaciones sigue creciendo y la validación nativa de reparaciones sigue en curso. Una compilación correcta o una comprobación simulada no demuestra el comportamiento nativo de una reparación. Se usan máquinas virtuales Windows desechables para validar cada cambio nativo en distintas versiones de Windows y estados de fallo. Consulta [Cómo se mantienen seguros los cambios](safety.html) para ver el modelo de seguridad completo.

## Glosario

Definiciones en lenguaje sencillo de los términos usados en esta página, en orden alfabético.

| Término | Qué significa |
| --- | --- |
| BITS | Servicio de transferencia inteligente en segundo plano, el servicio de Windows que descarga actualizaciones y otros archivos en segundo plano. |
| Captura | Un archivo `.kbench` guardado con las evidencias conservadas, para poder revisarlo de nuevo sin el equipo original. |
| CBS | Mantenimiento basado en componentes, el subsistema de Windows que instala actualizaciones y características. Su registro es `CBS.log`. |
| Conciliación | Leer el estado actual después de una interrupción para saber qué ocurrió, sin repetir el cambio. |
| Diario | Un registro JSON local que se escribe antes de un cambio y se actualiza después, para poder revisar más tarde una acción interrumpida. |
| DISM | Administración y mantenimiento de imágenes de implementación, la herramienta de Windows que comprueba y repara el almacén de componentes. |
| Elevación | Ejecutar con derechos de administrador tras un aviso de Windows. KillerBench nunca se eleva por sí mismo. |
| Evidencia | Lo que KillerBench leyó realmente de Windows, guardado con su origen para poder seguir un hallazgo hasta él. |
| Fallo cerrado | Cuando las evidencias son ambiguas, faltan o son demasiado grandes, el resultado es Desconocido y no se permite ningún cambio. |
| Flujo de trabajo | La ejecución de una herramienta: las comprobaciones que recopila, las recetas que puede usar y el informe que produce. |
| Paquete de conocimiento | Un archivo de datos de referencia con versión y validado, como códigos de error o identificadores de hardware. |
| Panther | La carpeta donde el programa de instalación de Windows escribe sus registros de actualización. |
| Propietario de directiva | La directiva de grupo o la administración de dispositivos que controla una configuración. KillerBench no toca la configuración controlada. |
| Protección pendiente | El registro que marca un cambio como posiblemente enviado a Windows. Bloquea la repetición hasta que la conciliación lea el resultado. |
| Receta | Una reparación tipada con sus propias reglas de elegibilidad, ejecución, verificación y recuperación. |
| SFC | Comprobador de archivos de sistema, la herramienta de Windows que verifica y restaura los archivos protegidos del sistema. |
| TPM | Módulo de plataforma segura, el chip de seguridad que usa BitLocker para proteger la clave de la unidad. |
| Verificado | El resultado se volvió a leer de Windows y coincidió con lo que debía hacer el cambio. |
