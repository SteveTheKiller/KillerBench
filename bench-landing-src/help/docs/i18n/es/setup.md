# Descarga y configuración

KillerBench 1.0.0 está sin publicar. Todavía no hay ningún paquete ni descarga pública de KillerBench. Esta página describe los requisitos actuales del paquete para que puedas prepararte sin descargar una copia no oficial.

## Windows y runtime

- El destino de publicación actual es **Windows x64**. Windows 11 x64 es la referencia actual de validación nativa. No se ofrecen distribuciones nativas para ARM64, x86 ni Windows Server.
- KillerBench se empaqueta de dos formas. El instalador es **dependiente del framework** y requiere **.NET Desktop Runtime 10 para Windows x64**. El .NET Runtime básico, ASP.NET Core Runtime y .NET Framework no incluyen WPF. La edición portátil lleva su propio runtime y no necesita una instalación aparte. La herramienta de línea de comandos `killerbench-cli.exe` necesita .NET 10 Runtime para Windows x64. El SDK solo hace falta para desarrollo, no para el uso normal.
- Usa una versión de Windows compatible con [.NET 10 en Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows). Que el runtime sea compatible no significa que las reparaciones de KillerBench estén validadas en todas las ediciones o compilaciones compatibles. La cobertura de Windows 10 sigue pendiente y sus ediciones compatibles dependen de los requisitos actuales del ciclo de vida de Microsoft.
- Obtén el Desktop Runtime solo desde [la página de descarga de .NET 10 de Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), eligiendo **.NET Desktop Runtime**, **Windows**, **x64**. Sigue las normas de instalación de software de tu organización.

Si `dotnet` está disponible, `dotnet --list-runtimes` debería mostrar `Microsoft.WindowsDesktop.App 10.0.x` para la instalación x64. Un runtime instalado solo para otra arquitectura no sirve para el paquete de instalación x64.

## Cuando se publique un paquete

1. Usa la descarga oficial enlazada desde este sitio web. Todavía no hay ningún enlace de descarga de KillerBench activo.
2. Lee los requisitos de ese paquete, la [licencia GPLv3](../license.txt), las instrucciones de Código fuente correspondiente, los avisos de terceros incluidos y la información de firma y suma de comprobación. El nombre del archivo por sí solo no demuestra su autenticidad.
3. Ejecuta el instalador para la cuenta actual, o para todos los usuarios tras una solicitud de elevación. Añade un acceso directo en el menú Inicio, un acceso directo opcional en el escritorio y una entrada en Agregar o quitar programas. La edición portátil se ejecuta sin instalación.
4. Inicia KillerBench con tu cuenta normal y sigue [Tu primer diagnóstico](first-run.html). Los fallos de acceso del diagnóstico siguen visibles; la elevación es una decisión aparte y deliberada.

No te saltes las advertencias de seguridad de Windows para ejecutar una copia no verificada. Mantén una copia de seguridad actual antes de seleccionar cualquier acción que cambie el equipo.

Al iniciarse, KillerBench consulta GitHub en busca de una versión estable más reciente y pregunta antes de instalarla. La descarga debe coincidir con la suma de comprobación SHA-256 publicada. Desactiva la comprobación con **Buscar actualizaciones al iniciar** en Acerca de.

## Límites actuales

La compilación actual tiene doce secciones, diagnóstico de solo lectura, informes y superficies de reparación seleccionadas de forma individual. La validación nativa de reparaciones sigue en curso. Una interfaz que funciona, una compilación correcta o el resultado de un fixture no demuestran el comportamiento de una reparación en un equipo real. Consulta [Cómo se mantienen seguros los cambios](safety.html#development-status).

La consola ilustrativa de la página principal es una salida sintética. El primer paquete público sigue pendiente.

## Código fuente, licencia y soporte

KillerBench tiene licencia GPLv3. Las instrucciones de entrega del código fuente y los permisos de las fuentes tipográficas de terceros siguen pendientes para el primer paquete público. Lee [Acerca de KillerBench](about.html) para conocer la disponibilidad del código fuente, los avisos de distribución, la privacidad de las evidencias y la información de soporte. La [guía de Engine](../engine/) es para desarrolladores con una copia autorizada del código fuente; no implica que haya un paquete público de Engine.
