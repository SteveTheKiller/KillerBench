# Puesta en marcha

Puesta en marcha prepara un equipo nuevo. Revisa las aplicaciones y la configuración que selecciones y pone nombre al equipo. No descarga ni instala nada por sí sola.

Guías paso a paso: [Configurar un equipo nuevo](set-up-new-pc.html).

## Comprobar un instalador antes de ejecutarlo

La línea de comandos puede verificar el instalador de un fabricante antes de que lo ejecutes:

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
```

KillerBench solo confía en un instalador cuando lleva una firma Authenticode válida con el nombre exacto de la organización del fabricante. Nunca ejecuta el instalador.

## Herramientas de esta sección

Prepara un equipo nuevo: aplicaciones, nombre y configuración.

### Puesta en marcha

Revisa las aplicaciones de aprovisionamiento y los grupos de configuración seleccionados de forma explícita. Los registros actuales de desinstalación, paquetes de apps y Office Hacer clic y ejecutar se cotejan sin descargar ni ejecutar instaladores. Las selecciones de configuración describen sus consecuencias y remiten a flujos de trabajo protegidos independientes cuando estos existen. No se realiza ninguna acción de descarga, instalación, cuenta, actualización, limpieza, directiva, registro, servicio, perfil ni reinicio.

### Nombre del equipo

Revisa el nombre propuesto para el equipo frente a la identidad actual y el rol en el dominio. Sin cambio de nombre, sin recopilación de credenciales y sin reinicio. La disponibilidad del nombre, los permisos y las dependencias de aplicaciones siguen sin verificar.
