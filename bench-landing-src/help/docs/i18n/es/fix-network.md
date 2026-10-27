# Resolver problemas de red e internet

Usa esta guía cuando un equipo no tiene internet, no puede llegar a un servidor o a un nombre, o pierde la conexión una y otra vez. KillerBench comprueba por orden el adaptador, la dirección, la puerta de enlace, el DNS y el firewall, y solo renueva una concesión o inicia un servicio cuando evidencias recientes demuestran que hace falta.

## Comprobar la conexión

1. Abre **Red** y elige **Red**.
2. Para probar algo concreto, rellena **Búsqueda de nombre de host (opcional)**, o **Destino TCP (opcional)** y **Puerto TCP**. Déjalos en blanco para comprobar solo la configuración local. No se envía ningún dato de aplicación.
3. Ajusta **Tiempo de espera por búsqueda o conexión (segundos)** si la red es lenta.
4. Deja **Reparar automáticamente problemas confirmados** desactivado y pulsa **Ejecutar diagnóstico**.
5. Lee el informe. Distingue entre una dirección que falta, una puerta de enlace que falta o no responde, problemas de configuración de DNS, la configuración de proxy, discrepancias en el perfil del firewall y una búsqueda o conexión fallida, e indica el siguiente paso para cada caso.

Que la puerta de enlace responda no demuestra que haya acceso a internet, y que falle una búsqueda a través de tu resolvedor configurado no significa que el sitio esté caído.

## Reparar lo que encontró el informe

1. Vuelve a ejecutar **Red** con **Reparar automáticamente problemas confirmados** activado. Puede iniciar los servicios habilitados Cliente DNS, Cliente DHCP, cliente SMB, Reconocimiento de ubicación de red, Servicio de lista de redes y Aplicación auxiliar IP, y renovar adaptadores DHCP físicos conectados que no tengan una dirección IPv4 utilizable.
2. Cuenta con que la conexión se corte brevemente durante una renovación. Las direcciones estáticas y los adaptadores virtuales nunca se cambian.
3. Para un único nombre en caché incorrecto, usa el vaciado de la caché DNS para ese host exacto en lugar de borrarlo todo.

Las rutas, las entradas de Hosts, los servidores DNS, las reglas del firewall, los proveedores de Winsock y los perfiles de red se conservan siempre.

## Redes inalámbricas, inicio de sesión por cable y proxies

- **Redes inalámbricas** comprueba el adaptador y Configuración automática de WLAN, y puede iniciar el servicio si está detenido.
- **Autenticación cableada (802.1X)** hace lo mismo con Configuración automática de redes cableadas en las redes que exigen iniciar sesión en el puerto.
- **Proxy automático WinHTTP** muestra la configuración de proxy guardada para el equipo y la cuenta actual. KillerBench nunca descarga una URL de PAC ni cambia la configuración de proxy durante el diagnóstico.

## Desde la línea de comandos

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

Consulta [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) y [dns-cache](cli.html#cmd-dns-cache).

## Relacionado

- [Sección Red](network.html)
- [Resolver problemas de impresión](fix-printing.html)
- [Cómo se mantienen seguros los cambios](safety.html)
