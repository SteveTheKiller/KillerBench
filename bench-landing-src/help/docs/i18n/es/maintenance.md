# Mantenimiento

Mantenimiento mantiene el equipo ordenado: limpieza de archivos, perfiles de usuario antiguos, tareas programadas y despejar Windows. Cada herramienta previsualiza lo que quitaría antes de borrar nada.

Guías paso a paso: [Liberar espacio en disco](free-disk-space.html) y [Hacer Windows más privado](make-windows-private.html).

## Herramientas de esta sección

Limpia archivos, perfiles y tareas programadas, y despeja Windows.

### Limpieza

Estima los destinos seleccionados de archivos temporales, cachés y evidencias de diagnóstico conservadas usando la antigüedad y el límite de archivos del panel principal. Windows Temp y Windows SystemTemp son destinos independientes. Optimización de distribución es una opción de caché aparte, y el contenido quitado puede tener que descargarse de nuevo. La eliminación permanente opcional usa la casilla de limpieza independiente, vuelve a comprobar los candidatos conservados dentro del límite seleccionado y registra cada resultado. El borrado no pasa por la Papelera de reciclaje. Los destinos, la antigüedad y el límite de archivos también se pueden configurar con el comando cleanup de la CLI.

### Perfiles

Revisa los perfiles de Windows usando el umbral de antigüedad seleccionado, los registros exactos de ProfileList y, opcionalmente, las dependencias de cuentas de servicios y tareas programadas, y devuelve una única conclusión de revisión delimitada. Los perfiles cargados, los del sistema y el de la cuenta actual quedan excluidos de la revisión. Los candidatos son solo para revisión. No se cambia ningún perfil, carpeta, clave del registro, cuenta, servicio, tarea, credencial, directiva ni archivo, y no se selecciona ninguna eliminación. Las dependencias de aplicaciones y la posibilidad de recuperar copias de seguridad siguen sin verificar.

### Tareas programadas

Hace inventario de las tareas programadas visibles, sus estados y las entidades de seguridad indicadas. No se inicia, registra ni cambia ninguna tarea. No se verifican las acciones, los desencadenadores ni el éxito de la ejecución.

### Despejar Windows

Revisa áreas explícitas de limpieza de Windows como Recall e IA, telemetría, sugerencias de búsqueda, contenido para consumidores, barra de tareas e Inicio, notificaciones, Edge, energía, hibernación, idiomas de Office y software preinstalado, para la cuenta en ejecución o para todos los perfiles. La revisión es de solo lectura. El plan de energía de alto rendimiento, los temporizadores de suspensión y pantalla, y la hibernación se pueden cambiar uno a uno cuando esa área es la única seleccionada, con evidencias recientes y confirmación explícita.
