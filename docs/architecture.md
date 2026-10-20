# CleanStats — arquitectura inicial

## Dependencias permitidas

`App -> Core` y `App -> Infrastructure`; `Infrastructure -> Core`. `Core` no depende de WPF, SQLite ni API nativas de Windows.

## Responsabilidades

- **App:** ventanas WPF, ViewModels, navegación, estado visual y composición de dependencias.
- **Core:** modelos de dominio, enums, contratos de servicios y lógica pura de reglas.
- **Infrastructure:** adaptadores de sistema de archivos, persistencia y APIs nativas en etapas futuras.
- **Tests:** pruebas unitarias del dominio y pruebas de infraestructura aisladas mediante directorios temporales.

## Reglas de calidad

- Nullable reference types activados.
- `async`/`await` para tareas prolongadas.
- `CancellationToken` en operaciones de escaneo futuras.
- Nunca ejecutar trabajo de disco prolongado en el hilo de UI.
- Manejar errores por elemento; no ocultar resultados parciales.
- Sin operaciones de escritura durante el análisis inicial.
- Mantener solución compilable y pruebas ejecutables en cada etapa.
