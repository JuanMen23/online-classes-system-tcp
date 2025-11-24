# Obligatorio Redes 1 - Sistema de Clases Online

## Descripción del Proyecto

Este proyecto implementa un sistema de clases online desarrollado en .NET 9, siguiendo los requerimientos especificados en la guía de aulas. El sistema está compuesto por una aplicación servidor y una aplicación cliente que se comunican mediante sockets TCP.

La lógica de comunicación servidor-cliente se basa en la guía de sockets proporcionada en aulas, implementando los conceptos fundamentales de programación de redes.

## Arquitectura del Sistema

Se implementó una **arquitectura de capas** que separa las responsabilidades del sistema:

- **Presentation Layer**: ClientApplication y ServerApplication
- **Business Layer**: Services (SocketService, ClientHandler)  
- **Data Access Layer**: Socket communication
- **Shared Layer**: Protocol constants y configuración

Esta arquitectura facilita el mantenimiento, testing y escalabilidad del sistema.

## Estructura del Proyecto

```
/ObligatorioRedes1
│
├── Client/                          # Aplicación Cliente
│   ├── Program.cs                   # Entry point del cliente
│   ├── ClientApplication.cs         # Lógica principal y ciclo de vida
│   ├── Services/
│   │   └── SocketService.cs         # Manejo de comunicación socket
│   └── Models/                      # Modelos de datos (futuro)
│
├── Server/                          # Aplicación Servidor
│   ├── Program.cs                   # Entry point del servidor
│   ├── ServerApplication.cs         # Lógica principal y gestión de conexiones
│   ├── Services/
│   │   └── ClientHandler.cs         # Manejo individual de clientes
│   └── Models/                      # Modelos de datos (futuro)
│
├── Common/                          # Biblioteca Compartida
│   ├── Protocol/
│   │   └── ProtocolConstants.cs     # Constantes del protocolo de comunicación
│   └── Config/
│       └── AppConfig.cs             # Configuración de la aplicación
│
└── ObligatorioRedes1.sln           # Solución de Visual Studio
```

## Tecnologías Utilizadas

- **.NET 9**: Framework de desarrollo
- **C#**: Lenguaje de programación
- **System.Net.Sockets**: Comunicación TCP (basado en guía de aulas)
- **System.Threading**: Manejo de concurrencia

## Características Implementadas

- **Comunicación TCP**: Cliente-servidor mediante sockets
- **Múltiples Clientes**: El servidor maneja múltiples conexiones simultáneas
- **Manejo de Hilos**: Cada cliente se maneja en un hilo separado
- **Desconexión Limpia**: Manejo adecuado de desconexiones y errores
- **Shutdown Controlado**: El servidor puede cerrarse de forma ordenada

## Cómo Ejecutar el Proyecto

### Prerrequisitos
- .NET 9 SDK instalado
- Terminal o línea de comandos

### Pasos para Ejecutar

1. **Clonar o descargar el proyecto**
2. **Abrir terminal en la carpeta del proyecto**
3. **Compilar la solución**:
   ```bash
   dotnet build
   ```

4. **Ejecutar el servidor** (en una terminal):
   ```bash
   dotnet run --project Server
   ```

5. **Ejecutar el cliente** (en otra terminal):
   ```bash
   dotnet run --project Client
   ```

### Uso del Sistema

1. **Servidor**: Se inicia y queda esperando conexiones en `127.0.0.1:20000`
2. **Cliente**: Se conecta al servidor y permite enviar mensajes
3. **Comunicación**: El servidor responde con un echo de los mensajes recibidos
4. **Salida**: Escribir `exit` en el cliente para desconectarse
5. **Shutdown**: Escribir `shutdown` en el servidor para cerrarlo

## Colección de Postman

El repositorio incluye una colección de Postman para probar el Servidor de Logs (REST API).

**Ubicación:** `postman/LogsServer_Collection.json`

**Documentación:** Ver `postman/README.md` para instrucciones de importación y uso.

La colección incluye ejemplos para:
- Obtener todos los logs
- Filtrar por usuario, clase, nivel, evento, fecha, texto
- Combinar múltiples filtros
- Agregar logs de prueba

## Desarrollo del Proyecto

Este proyecto fue desarrollado siguiendo un enfoque iterativo:

1. **Análisis de Requerimientos**: Se analizó la guía de aulas para entender los requerimientos
2. **Diseño de Arquitectura**: Se seleccionó una arquitectura de capas apropiada
3. **Kick-off con Cursor AI**: Se utilizó Cursor AI para generar la estructura inicial y código base
4. **Implementación de Sockets**: Se basó en la guía de sockets de aulas para la comunicación TCP

