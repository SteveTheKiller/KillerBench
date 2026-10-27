# Tu primer diagnóstico

Empieza por [Descarga y configuración](setup.html) y después ejecuta un Análisis completo del sistema. Recopila evidencias de diagnóstico y guarda un diario local; no ejecuta reparaciones. Un hallazgo describe el alcance inspeccionado, no todos los fallos posibles del equipo.

## Ejecutar el análisis

1. Abre KillerBench. La sección **Diagnóstico** se abre con **Análisis completo del sistema** seleccionado.
2. Deja **Reparar automáticamente problemas confirmados** desactivado en tu primera ejecución.
3. Pulsa **Ejecutar diagnóstico**. El progreso aparece en **Salida sin procesar**, a la derecha, mientras se ejecuta cada comprobación.
4. Al terminar, el panel de salida cambia al **Informe legible**.

Empieza sin elevación. Algunas evidencias de Windows, como los protectores de BitLocker y los servicios protegidos, necesitan derechos de administrador. Con una cuenta estándar, esas comprobaciones indican que la evidencia no está disponible. Si necesitas esa evidencia, termina la ejecución actual, guarda el informe y vuelve a abrir KillerBench a propósito con **Ejecutar como administrador**. La elevación no selecciona ni aprueba ninguna reparación.

## Leer el informe

El informe empieza con los recuentos de **Por revisar**, **Reparaciones verificadas**, **Incompletas** y **Comprobadas**, seguidos de un siguiente paso recomendado.

- **Qué necesita atención** enumera los problemas que respaldan las evidencias, empezando por los más importantes.
- **Información recopilada** muestra lo que se leyó, incluidas las evidencias incompletas o no disponibles.
- Los eventos históricos se marcan como historial. Un error del mes pasado no se trata como un fallo actual.

La falta de evidencias nunca cuenta como buen estado. Si una comprobación no pudo ejecutarse, el informe lo dice.

## Elegir tú las comprobaciones

Abre **Elegir comprobaciones (avanzado)** para elegir comprobaciones concretas en lugar del análisis completo. Cada sección de la app también tiene herramientas específicas, descritas en las páginas de **Secciones** de esta guía.

## Corregir lo que encontraste

Cuando el informe recomienda una corrección, indica qué sección y qué herramienta abrir. Lee [Cómo se mantienen seguros los cambios](safety.html) antes de tu primera reparación. La validación nativa sigue en curso; un diagnóstico correcto no demuestra que cada reparación ofrecida se haya probado en tu compilación de Windows. Mantén copias de seguridad y revisa el destino exacto y las consecuencias antes de cualquier cambio.
