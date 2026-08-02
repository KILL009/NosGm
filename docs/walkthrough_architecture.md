# Arquitectura y Build x64

Este documento registra la configuración definitiva de la solución para garantizar una compilación limpia en x64 sin advertencias de desajuste de arquitectura.

## 1. Mapeos de la Solución (Release|x64)
La configuración Release|x64 (y Debug|x64) de NosGm.sln ha sido mapeada cuidadosamente según la naturaleza de cada proyecto para evitar forzar plataformas inválidas:

### Ejecutables (Mapeados a x64)
Los siguientes proyectos principales han sido configurados con <PlatformTarget>x64</PlatformTarget> y son dirigidos hacia la plataforma x64 en el .sln:
- NosGm.Login
- NosGm.Master.Server
- NosGm.World
- NosGm.Parser
- NosGm.LogServer

### Bibliotecas (Mapeadas a AnyCPU)
El resto de proyectos, incluyendo NosGm.Authentication.Client (que es una biblioteca cliente gRPC) y NosGm.Cluster.Contracts, mantienen la plataforma Any CPU tanto en sus archivos .csproj como en su mapeo dentro de la configuración x64 de la solución.

## 2. Authentication Server
NosGm.Authentication.Server es un ejecutable moderno en .NET 10 y se publica de manera independiente (out-of-band respecto a MSBuild legacy) con el runtime win-x64:
`powershell
dotnet publish ".\Data\NosGm.Program\NosGm.Authentication.Server\NosGm.Authentication.Server.csproj" --configuration Release --runtime win-x64 --self-contained false --output ".\bin\Release\Authentication" --nologo
`

## 3. Verificación y Resultados
Al compilar la solución completa heredada a través de MSBuild forzando /p:Platform=x64 (como se hace en GitHub Actions) se obtienen los siguientes resultados:
- **MSB3270**: 0 coincidencias en los logs de compilación, confirmando la resolución total del conflicto de arquitecturas.
- **Binlog**: La evidencia de la compilación (junto al log de texto) se captura y almacena en .\artifacts\msb3270-x64.binlog.
- **Rutas de Salida**: Todas las carpetas de salida (in\Release\Master\, in\Release\World\, etc.) han sido preservadas intactas, sin mutar a carpetas temporales x64.

## 4. Prueba Funcional del Arranque
Tras aplicar esta configuración estricta, la ejecución de la prueba funcional mediante el arranque moderno completo (erify-modern-login-runtime-activation.ps1) confirmó que los procesos de Master, AuthBridge, Authentication Server, World y Login continúan encontrando todas sus dependencias sin problemas.
