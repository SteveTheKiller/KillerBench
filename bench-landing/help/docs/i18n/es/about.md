# Acerca de KillerBench

KillerBench es un banco de trabajo de diagnóstico, reparación y mantenimiento de Windows creado por Steve the Killer, parte de la [familia Killer Tools](https://killertools.net). La versión 0.1.0 está sin publicar. Su app de escritorio, Engine y la CLI comparten las evidencias de diagnóstico y los flujos de reparación compatibles.

## Disponibilidad y alcance

Todavía no hay ninguna descarga pública de KillerBench ni paquete de Engine. [Descarga y configuración](setup.html) describe el destino actual Windows x64 y los requisitos del Desktop Runtime. La validación nativa de reparaciones sigue en curso; que exista una herramienta no demuestra que se hayan probado todos los fallos, interrupciones o configuraciones de Windows.

## Avisos sobre el código fuente y la distribución

KillerBench tiene licencia **GPLv3**. Su licencia de distribución es la [Licencia Pública General de GNU versión 3](../license.txt). La licencia describe los permisos y condiciones para copiar, modificar y distribuir la obra cubierta.

El [repositorio público de KillerBench](https://github.com/SteveTheKiller/KillerBench) ofrece por ahora información del proyecto. La implementación completa no está disponible públicamente allí. Estas guías para desarrolladores describen una copia autorizada del código fuente; no ofrecen una descarga pública del código fuente.

Antes de distribuir un binario, quien lo publique debe facilitar el Código fuente correspondiente mediante un método permitido por la [sección 6 de la GPLv3](https://www.gnu.org/licenses/gpl-3.0.en.html#section6), incluido el código fuente y el material de compilación que exige la licencia. Las instrucciones de entrega del código fuente del primer paquete público y los avisos de terceros incluidos siguen pendientes. La GPLv3 no concede permiso para redistribuir fuentes tipográficas de terceros; los permisos actuales de las fuentes aún deben verificarse. Revisa la licencia, las instrucciones del código fuente y los avisos del paquete publicado antes de redistribuirlo.

## Privacidad y servicios externos

Los diagnósticos y el análisis de registros se ejecutan en local, sin servicio de IA ni cuenta de KillerBench. Los diarios y las evidencias conservadas usan las carpetas `%LOCALAPPDATA%\KillerBench` de la cuenta local. Las acciones seleccionadas pueden contactar con Windows Update, servicios de descarga de software, directorios u otros destinos indicados. La comprobación de actualizaciones al iniciar contacta con GitHub, y las actualizaciones de conocimiento y la página del instalador de MCP contactan con sus servicios configurados. Un cliente de IA que use MCP puede recibir el resultado de referencia devuelto.

Este sitio web guarda las opciones de tema y color de acento en el almacenamiento local del navegador. Sus guías no ejecutan ningún diagnóstico en tu equipo. Los registros sin procesar, los archivos importados y las evidencias exportadas pueden contener nombres de equipos, cuentas, rutas o texto de origen confidencial. Revísalos antes de compartirlos; no incluyas claves de recuperación, credenciales ni datos de clientes en un informe público.

## Soporte

La información del proyecto está en el repositorio público. Para obtener soporte, usa los datos de contacto publicados en [el sitio web del autor](https://thekiller.net); incluye la versión de la app, la compilación de Windows, la herramienta exacta, el resultado esperado y el resultado observado. Revisa y anonimiza las evidencias antes de enviarlas, y usa un canal privado aprobado para la información de clientes. Conserva los diarios cuando el resultado de una reparación sea incierto.

## Guías relacionadas

- [Cómo se mantienen seguros los cambios](safety.html)
- [Informes y evidencias](reports.html)
- [Guía de Engine para desarrolladores](../engine/)
- [KillerMCP](../mcp.html)
