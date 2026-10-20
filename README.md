# CleanStats

Aplicación de escritorio WPF para Windows 11 destinada a analizar el uso del almacenamiento y clasificar candidatos a limpieza. El alcance inicial es de solo lectura.

## Requisitos de desarrollo

- Windows 11 x64
- .NET 10 SDK (versión 10.0.100 o una versión de feature band posterior compatible)
- Visual Studio 2026 con la carga de trabajo **Desarrollo de escritorio de .NET**, o CLI de .NET 10

## Estructura

- `src/CleanStats.App`: interfaz WPF y composición de la aplicación.
- `src/CleanStats.Core`: dominio y contratos, sin dependencias de UI.
- `src/CleanStats.Infrastructure`: adaptadores del sistema operativo y persistencia futura.
- `tests/CleanStats.Core.Tests`: pruebas de dominio.
- `tests/CleanStats.Infrastructure.Tests`: pruebas de infraestructura.
- `docs`: alcance, riesgos y decisiones de arquitectura.

## Estado

Etapa 1 — esqueleto de solución y arquitectura. No se ha implementado el escáner, clasificación, persistencia ni limpieza. El análisis y las operaciones destructivas no están disponibles.

## Compilar y probar en Windows

```powershell
dotnet --info
dotnet restore .\CleanStats.sln
dotnet build .\CleanStats.sln --configuration Debug
dotnet test .\CleanStats.sln --configuration Debug
```

La compilación debe validarse en Windows con .NET 10 instalado; el proyecto WPF está dirigido a `net10.0-windows`.
