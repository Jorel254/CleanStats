# CleanStats — alcance aprobado

## Decisiones aprobadas

- Nombre provisional: CleanStats.
- Sistema operativo: Windows 11 x64.
- Lenguaje/runtime: C# y .NET 10.
- Interfaz: WPF.
- Persistencia prevista: SQLite local para historial y configuración cuando se implemente esa etapa.
- Primer alcance: análisis y clasificación de archivos; no borrar ni modificar archivos.
- Optimización NTFS/MFT y análisis incremental USN: pospuestos hasta validar el escáner básico.
- Funcionamiento principal sin conexión y sin telemetría por defecto.

## Dentro del MVP

- Seleccionar una unidad o carpeta para analizar.
- Mostrar tamaños por archivo, carpeta y extensión.
- Identificar candidatos mediante reglas explicables.
- Mostrar ruta, tamaño, fechas disponibles, categoría, nivel de riesgo y motivo.
- Mostrar progreso, permitir cancelación y registrar errores/omisiones.
- Informar estimaciones de espacio recuperable sin garantizar que se recupere ese espacio físico.

## Fuera del MVP

- Eliminar archivos o moverlos a cuarentena.
- Limpieza automática o programada.
- Limpieza del Registro de Windows.
- Controlador de filtro de sistema de archivos o monitorización de lecturas en tiempo real.
- Optimización de RAM o promesas de acelerar Windows.
- Acceso acelerado MFT/USN antes de validar el escáner convencional.

## Parámetros iniciales

- Antigüedad para identificar elementos que requieren revisión: 180 días.
- Umbral de archivo grande: 1 GiB (1,073,741,824 bytes).
- Umbral inicial de presentación de candidatos: 10 MiB (10,485,760 bytes).
- Los umbrales son configurables y no autorizan por sí mismos ninguna eliminación.
- El último acceso puede estar deshabilitado o retrasado por Windows; no se usa como prueba concluyente de inactividad.

## Clasificación de riesgo

- R0 — Protegido: no se propone para limpieza normal.
- R1 — Alta confianza: solo ubicaciones y reglas específicas, con validaciones; en el MVP solo se informa.
- R2 — Revisión: instaladores, logs, cachés de aplicaciones y archivos antiguos; nunca se consideran basura confirmada.
- R3 — Desconocido: no hay evidencia suficiente; solo se informa.

No clasificar como basura únicamente por extensión, tamaño, fecha de modificación o último acceso.

## Criterios de aceptación del alcance

- El alcance inicial es exclusivamente de análisis/clasificación de solo lectura.
- Cada clasificación debe indicar regla y evidencia.
- Las operaciones de borrado, cuarentena y restauración están fuera del MVP.
- La optimización NTFS se pospone hasta que el escáner básico tenga pruebas y resultados comparables.
