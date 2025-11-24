# Documento de Diseño Arquitectónico
## Tercera Iteración - Sistema de Clases Online

**Proyecto:** Obligatorio Redes 1  
**Iteración:** Tercera (Upgrade a Clases Online)  
**Fecha:** 2025  
**Tecnologías:** .NET 9, gRPC, RabbitMQ, REST API, WebSocket, Docker

---

## Tabla de Contenidos

1. [Análisis de Requerimientos](#1-análisis-de-requerimientos)
2. [Arquitectura General](#2-arquitectura-general)
3. [Distribución de Tecnologías](#3-distribución-de-tecnologías)
4. [Decisiones de Diseño Detalladas](#4-decisiones-de-diseño-detalladas)
5. [Modelos de Datos](#5-modelos-de-datos)
6. [Protocolos de Comunicación](#6-protocolos-de-comunicación)
7. [Plan de Implementación](#7-plan-de-implementación)
8. [Consideraciones Técnicas](#8-consideraciones-técnicas)
9. [Diagramas de Flujo](#9-diagramas-de-flujo)

---

## 1. Análisis de Requerimientos

### 1.1 Requerimientos Funcionales del Servidor de Logs

**SLRF1. Recepción de logs desde el servidor principal**
- El servidor de logs debe recibir logs de eventos generados en el servidor principal
- Debe ser desacoplado y no bloquear operaciones del servidor principal

**SLRF2. Filtro de logs**
- El servidor debe filtrar logs por al menos 3 criterios combinables
- Criterios propuestos: Usuario, Clase, Fecha, Nivel (INFO/WARNING/ERROR), Evento (crear, registrar, cancelar, etc.)
- Los filtros deben ser combinables mediante operadores AND

**SLRF3. Acceso remoto**
- Debe exponer un servicio que permita acceder a los logs filtrados de manera remota
- Debe ser accesible desde cualquier cliente HTTP

### 1.2 Requerimientos Funcionales del Servidor de Chat

**CSR1. Gestión de clases online**
- Aplicación independiente para gestionar clases online
- Debe manejar múltiples clases simultáneamente
- Cada clase tiene un link/código único para acceso

**CSR2. Verificación**
- Debe validar links/códigos de clases
- Debe verificar autenticación de usuarios con el servidor principal
- Validación debe ser síncrona y eficiente

### 1.3 Requerimientos Funcionales del Cliente de Chat

**CCR1. Acceso a clases online**
- Usuario debe poder acceder usando link o código
- La conexión debe ser persistente durante la clase

**CCR2. Autenticación de usuarios**
- Debe usar las mismas credenciales que el servidor principal
- Autenticación debe ser verificada contra el servidor principal

**CCR3. Comunicación en vivo**
- Mensajes deben enviarse y recibirse sin retrasos considerables
- Debe soportar múltiples usuarios en la misma clase simultáneamente

**CCR4. Interfaz de usuario**
- Interfaz puede ser consola o web (elegimos consola para simplicidad)
- Debe mostrar mensajes en tiempo real

### 1.4 Requerimientos del Servidor Principal

**SCR1. Webhook en registro de clases**
- Al registrar en una clase, se debe poder agregar una URL de webhook opcional
- 1 minuto antes del inicio de la clase, el servidor debe llamar automáticamente al webhook
- La llamada debe ser HTTP POST asíncrona

### 1.5 Requerimientos de Tecnologías

- **gRPC**: Comunicación eficiente entre servicios internos
- **RabbitMQ**: Message-Oriented Middleware para desacoplamiento
- **REST API**: Acceso remoto estándar HTTP
- **WebSocket**: Comunicación bidireccional en tiempo real
- **Docker**: Despliegue de todas las aplicaciones
- **Docker Multi-stage Build**: Solo para el servidor principal

### 1.6 Requerimientos de Despliegue

- Todas las aplicaciones deben usar Docker
- RabbitMQ debe desplegarse usando Docker
- docker-compose para orquestar todas las imágenes
- Servidor principal debe usar multi-stage build con justificación

---

## 2. Arquitectura General

### 2.1 Visión General del Sistema

El sistema evoluciona de una arquitectura cliente-servidor simple a una **arquitectura de microservicios** con los siguientes componentes:

```
┌─────────────┐
│   Cliente   │  (Consola - TCP Socket)
└──────┬──────┘
       │ TCP Socket (Protocolo Personalizado)
       │
┌──────▼──────────────────────────────────────┐
│         Servidor Principal                  │
│  ┌────────────────────────────────────┐    │
│  │  - TCP Socket Server               │    │
│  │  - UserService                     │    │
│  │  - ClassService                    │    │
│  │  - WebhookService                  │    │
│  └────────────────────────────────────┘    │
│           │              │                  │
│           │              │                  │
│  ┌────────▼───┐  ┌──────▼─────────┐        │
│  │  RabbitMQ  │  │  gRPC Server   │        │
│  │  Producer  │  │  (Auth/Verify) │        │
│  └────────────┘  └────────────────┘        │
└─────────────────────────────────────────────┘
       │                    │
       │                    │
       │                    │
┌──────▼──────────┐  ┌──────▼──────────────┐
│ Servidor Logs   │  │ Servidor Chat       │
│                 │  │                     │
│ - RabbitMQ      │  │ - gRPC Client       │
│   Consumer      │  │ - WebSocket Server  │
│ - REST API      │  │ - ChatRoomManager   │
│ - Log Storage   │  └──────────┬──────────┘
└─────────────────┘             │
                                │ WebSocket
                          ┌─────▼─────────┐
                          │ Cliente Chat  │
                          │ (Consola)     │
                          └───────────────┘
```

### 2.2 Principios Arquitectónicos

1. **Separación de Responsabilidades**: Cada servicio tiene un propósito específico
2. **Desacoplamiento**: Uso de MOM (RabbitMQ) para desacoplar servicios
3. **Comunicación Eficiente**: gRPC para comunicación interna, REST para acceso externo
4. **Tiempo Real**: WebSocket para comunicación bidireccional instantánea
5. **Escalabilidad**: Arquitectura permite escalar componentes independientemente

---

## 3. Distribución de Tecnologías

### 3.1 Servidor Principal

**Tecnologías:**
- **TCP Socket**: Comunicación existente con cliente (mantiene compatibilidad)
- **RabbitMQ (Producer)**: Publica eventos/logs al broker
- **gRPC (Server)**: Expone servicios de autenticación y verificación
- **Background Service**: Ejecuta tareas programadas (webhooks)

**Justificación:**
- TCP Socket se mantiene para compatibilidad con clientes existentes
- RabbitMQ permite desacoplamiento total del sistema de logs
- gRPC es ideal para comunicación interna síncrona de bajo latencia
- Background Service maneja webhooks sin bloquear operaciones principales

**Eventos que se publican a RabbitMQ:**
- `user.registered`
- `user.logged_in`
- `user.logged_out`
- `class.created`
- `class.modified`
- `class.deleted`
- `class.enrolled`
- `class.enrollment_cancelled`
- `image.downloaded`

### 3.2 Servidor de Logs

**Tecnologías:**
- **RabbitMQ (Consumer)**: Consume eventos del broker
- **REST API**: Expone endpoints para consulta de logs filtrados
- **Almacenamiento In-Memory**: Colección thread-safe de logs

**Justificación:**
- RabbitMQ Consumer permite procesamiento asíncrono y desacoplado
- REST API es estándar para acceso remoto HTTP, fácil de probar con Postman
- Almacenamiento in-memory es suficiente para un sistema de logging académico, permite consultas rápidas con LINQ

**Estructura de logs almacenados:**
```csharp
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Usuario { get; set; }
    public int? ClaseId { get; set; }
    public string Evento { get; set; }  // "class.created", "user.enrolled", etc.
    public LogLevel Nivel { get; set; }  // INFO, WARNING, ERROR
    public string Mensaje { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}
```

### 3.3 Servidor de Chat

**Tecnologías:**
- **gRPC (Client)**: Se comunica con servidor principal para validación
- **WebSocket (Server)**: Maneja conexiones persistentes de clientes de chat
- **ChatRoomManager**: Gestiona múltiples salas de chat simultáneas

**Justificación:**
- gRPC Client permite validación rápida y síncrona con el servidor principal
- WebSocket es la tecnología estándar para chat en tiempo real, permite comunicación bidireccional
- ChatRoomManager mantiene estado de cada sala y sus participantes

**Servicios gRPC expuestos por el servidor principal:**
```protobuf
service AuthenticationService {
  rpc ValidateCredentials(ValidateRequest) returns (ValidateResponse);
  rpc VerifyClassLink(ClassLinkRequest) returns (ClassLinkResponse);
}
```

### 3.4 Cliente de Chat

**Tecnologías:**
- **WebSocket (Client)**: Conexión al servidor de chat
- **gRPC (Client)**: Autenticación inicial (opcional, puede ser REST)
- **Interfaz Consola**: Input/output de usuario

**Justificación:**
- WebSocket Client para comunicación en tiempo real con el servidor
- Consola es suficiente para este proyecto, mantiene simplicidad

### 3.5 Matriz de Tecnologías por Componente

| Componente | gRPC | RabbitMQ | REST API | WebSocket | TCP Socket |
|------------|------|----------|----------|-----------|------------|
| Servidor Principal | Server | Producer | - | - | Server |
| Servidor Logs | - | Consumer | Server | - | - |
| Servidor Chat | Client | - | - | Server | - |
| Cliente | - | - | - | - | Client |
| Cliente Chat | - | - | - | Client | - |

**✅ Todos los requerimientos tecnológicos cumplidos**

---

## 4. Decisiones de Diseño Detalladas

### 4.1 Sistema de Logging

#### 4.1.1 ¿Por qué RabbitMQ para logs?

**Decisión:** Usar RabbitMQ como Message-Oriented Middleware para desacoplar el servidor principal del servidor de logs.

**Justificación:**
1. **Desacoplamiento**: El servidor principal no necesita conocer la existencia del servidor de logs
2. **Confiabilidad**: RabbitMQ garantiza entrega de mensajes (durable queues)
3. **Escalabilidad**: Múltiples consumidores pueden procesar logs en paralelo
4. **Asincronía**: No bloquea operaciones del servidor principal
5. **Tolerancia a fallos**: Si el servidor de logs cae, los mensajes se acumulan en la cola

**Alternativas consideradas:**
- **Logging directo**: Rechazado por acoplamiento fuerte
- **File-based logging**: Rechazado por problemas de concurrencia y latencia
- **HTTP POST**: Rechazado por necesidad de manejo de errores complejo

#### 4.1.2 Estructura de Mensajes en RabbitMQ

**Decisión:** Usar formato JSON para mensajes en RabbitMQ.

**Justificación:**
- Legible y fácil de depurar
- Flexible para agregar campos sin romper compatibilidad
- Ampliamente soportado en .NET

**Ejemplo de mensaje:**
```json
{
  "timestamp": "2025-01-20T14:30:00Z",
  "usuario": "alice",
  "claseId": 123,
  "evento": "class.enrolled",
  "nivel": "INFO",
  "mensaje": "Usuario alice se registró en clase Programación",
  "metadata": {
    "claseNombre": "Programación",
    "cuposDisponibles": 5
  }
}
```

#### 4.1.3 Configuración de RabbitMQ

**Exchange:** `logs-exchange` (topic)  
**Queue:** `logs-queue` (durable)  
**Routing Key:** `log.*` (wildcard para todos los logs)

**Justificación Topic Exchange:**
- Permite routing flexible por tipo de evento
- Escalable para agregar nuevos tipos de logs
- Filtrado eficiente por routing key

### 4.2 Almacenamiento de Logs

#### 4.2.1 ¿In-Memory o Persistente?

**Decisión:** Almacenamiento in-memory usando `ConcurrentBag<LogEntry>`.

**Justificación:**
1. **Simplicidad**: No requiere configuración de base de datos
2. **Rendimiento**: Consultas muy rápidas con LINQ
3. **Suficiente para el alcance**: Sistema académico no requiere persistencia a largo plazo
4. **Fácil implementación**: .NET provee colecciones thread-safe nativas

**Limitaciones aceptadas:**
- Logs se pierden al reiniciar el servidor (aceptable para este proyecto)
- Memoria limitada (se puede implementar límite máximo de logs)

**Si fuera producción:** Usar base de datos (PostgreSQL, MongoDB) o Elasticsearch.

#### 4.2.2 Filtrado de Logs

**Decisión:** Filtrado usando LINQ sobre colección in-memory.

**Criterios de filtrado implementados:**
1. **Usuario** (`usuario`): Filtrar por nombre de usuario
2. **Clase** (`claseId`): Filtrar por ID de clase
3. **Fecha** (`fechaDesde`, `fechaHasta`): Rango de fechas
4. **Nivel** (`nivel`): INFO, WARNING, ERROR
5. **Evento** (`evento`): Tipo de evento (crear, registrar, etc.)

**Ejemplo de filtrado combinado:**
```csharp
var filteredLogs = _logs
    .Where(l => string.IsNullOrEmpty(usuario) || l.Usuario == usuario)
    .Where(l => !claseId.HasValue || l.ClaseId == claseId)
    .Where(l => (!fechaDesde.HasValue || l.Timestamp >= fechaDesde) &&
                (!fechaHasta.HasValue || l.Timestamp <= fechaHasta))
    .Where(l => string.IsNullOrEmpty(nivel) || l.Nivel.ToString() == nivel)
    .OrderByDescending(l => l.Timestamp)
    .ToList();
```

**Justificación:**
- LINQ es expresivo y fácil de mantener
- Performance suficiente para cientos/miles de logs
- Fácil de extender con nuevos criterios

### 4.3 REST API del Servidor de Logs

#### 4.3.1 Endpoints Propuestos

**GET /api/logs**
- Query parameters:
  - `usuario` (opcional): Filtrar por usuario
  - `claseId` (opcional): Filtrar por ID de clase
  - `fechaDesde` (opcional): Fecha inicio (ISO 8601)
  - `fechaHasta` (opcional): Fecha fin (ISO 8601)
  - `nivel` (opcional): INFO, WARNING, ERROR
  - `evento` (opcional): Tipo de evento
  - `limit` (opcional): Límite de resultados (default: 100)

**GET /api/logs/stats**
- Retorna estadísticas agregadas de logs

**Ejemplo de request:**
```
GET /api/logs?usuario=alice&claseId=123&fechaDesde=2025-01-20T00:00:00Z&nivel=INFO
```

**Ejemplo de response:**
```json
{
  "logs": [
    {
      "timestamp": "2025-01-20T14:30:00Z",
      "usuario": "alice",
      "claseId": 123,
      "evento": "class.enrolled",
      "nivel": "INFO",
      "mensaje": "Usuario alice se registró en clase Programación"
    }
  ],
  "total": 1,
  "filters": {
    "usuario": "alice",
    "claseId": 123,
    "nivel": "INFO"
  }
}
```

#### 4.3.2 Framework REST API

**Decisión:** ASP.NET Core Web API.

**Justificación:**
- Nativo en .NET, no requiere librerías externas
- Excelente soporte async/await
- Integración perfecta con otros componentes .NET
- Facilita testing y documentación

### 4.4 Sistema de Chat

#### 4.4.1 Arquitectura de Salas de Chat

**Decisión:** Un chat room por clase, identificado por link/código único.

**Estructura:**
```csharp
public class ChatRoom
{
    public string Link { get; set; }  // Link único de la clase
    public int ClassId { get; set; }
    public string ClassName { get; set; }
    public ConcurrentDictionary<string, WebSocket> Participants { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Justificación:**
- Una sala por clase mantiene el contexto claro
- Link único facilita acceso y validación
- ConcurrentDictionary permite acceso thread-safe a participantes

#### 4.4.2 Flujo de Autenticación en Chat

**Decisión:** Validación síncrona vía gRPC antes de permitir conexión WebSocket.

**Flujo:**
1. Cliente de chat solicita conexión con link/código y credenciales
2. Servidor de chat llama al servidor principal vía gRPC para validar:
   - Link/código es válido y la clase existe
   - Usuario está autenticado y tiene credenciales válidas
   - Usuario está inscrito en la clase (opcional pero recomendado)
3. Si válido, acepta conexión WebSocket y agrega a la sala
4. Si inválido, rechaza conexión con mensaje de error

**Justificación:**
- Validación antes de conectar evita conexiones innecesarias
- gRPC es rápido para validaciones síncronas
- Separación de responsabilidades: servidor principal autoriza, servidor chat gestiona comunicación

#### 4.4.3 Manejo de Mensajes WebSocket

**Decisión:** Broadcast de mensajes a todos los participantes de la sala (excepto el emisor).

**Formato de mensaje:**
```json
{
  "type": "message",
  "usuario": "alice",
  "mensaje": "Hola a todos",
  "timestamp": "2025-01-20T14:30:00Z",
  "claseId": 123
}
```

**Justificación:**
- Broadcast es el comportamiento esperado en un chat de clase
- WebSocket permite envío bidireccional eficiente
- Formato JSON es estándar y fácil de parsear

**Consideraciones:**
- Mensajes deben ser validados (evitar spam, longitud máxima)
- Manejo de desconexiones: remover participante de la sala
- Heartbeat para detectar conexiones muertas

### 4.5 Sistema de Webhooks

#### 4.5.1 Almacenamiento de Webhooks

**Decisión:** Agregar campo opcional `WebhookUrl` a `ClassSession`.

**Modificación a `ClassSession`:**
```csharp
public class ClassSession
{
    // ... campos existentes ...
    public string? WebhookUrl { get; set; }  // URL opcional de webhook
}
```

**Justificación:**
- Simple y directo: webhook pertenece a la clase
- Fácil de agregar en el registro de clase
- No requiere estructura adicional compleja

#### 4.5.2 Background Service para Webhooks

**Decisión:** Implementar `BackgroundService` que revisa clases periódicamente.

**Algoritmo:**
1. Cada 10 segundos, revisa todas las clases
2. Para cada clase con `WebhookUrl` y `StartDateTime` dentro de 1 minuto:
   - Si aún no se envió el webhook, envía HTTP POST asíncrono
   - Marca como enviado (usar flag o timestamp)
3. Payload del webhook:
```json
{
  "claseId": 123,
  "nombre": "Programación",
  "startDateTime": "2025-01-20T15:00:00Z",
  "enrolledUsers": ["alice", "bob"],
  "link": "class-abc123"
}
```

**Justificación:**
- BackgroundService es el patrón recomendado en .NET para tareas periódicas
- Revisión cada 10 segundos es suficiente (webhook se envía con ~60s de anticipación)
- HTTP POST asíncrono no bloquea el servidor

**Consideraciones:**
- Manejo de errores: reintentos, logging de fallos
- Prevenir envío múltiple: usar flag o timestamp de último envío
- Timeout en HTTP request (ej: 5 segundos)

#### 4.5.3 Modificación del Protocolo de Registro

**Decisión:** Extender `CMD_ENROLL_CLASS` para aceptar webhook URL opcional.

**Formato actual:**
```
REQ|20|0004|123
```

**Formato propuesto:**
```
REQ|20|0032|123|https://webhook.site/abc123
```

Si no se proporciona webhook, el segundo parámetro es vacío.

**Justificación:**
- Mantiene compatibilidad con clientes existentes (webhook es opcional)
- Simple de implementar: parsear parámetro adicional opcional

### 4.6 Integración gRPC

#### 4.6.1 Definición de Servicios gRPC

**Decisión:** Definir servicios en archivos `.proto` en Common.

**Estructura propuesta:**
```
Common/
  Proto/
    authentication.proto
    class_verification.proto
```

**authentication.proto:**
```protobuf
syntax = "proto3";

package common.proto;

service AuthenticationService {
  rpc ValidateCredentials(ValidateRequest) returns (ValidateResponse);
}

message ValidateRequest {
  string username = 1;
  string password = 2;
}

message ValidateResponse {
  bool valid = 1;
  string message = 2;
}
```

**class_verification.proto:**
```protobuf
syntax = "proto3";

package common.proto;

service ClassVerificationService {
  rpc VerifyClassLink(ClassLinkRequest) returns (ClassLinkResponse);
  rpc VerifyEnrollment(EnrollmentRequest) returns (EnrollmentResponse);
}

message ClassLinkRequest {
  string link = 1;
}

message ClassLinkResponse {
  bool valid = 1;
  int32 classId = 2;
  string className = 3;
  string message = 4;
}

message EnrollmentRequest {
  string username = 1;
  int32 classId = 2;
}

message EnrollmentResponse {
  bool enrolled = 1;
  string message = 2;
}
```

**Justificación:**
- Archivos `.proto` definen contrato claro entre servicios
- Genera código automáticamente para cliente y servidor
- Type-safe y eficiente

#### 4.6.2 Puerto gRPC

**Decisión:** Puerto 50051 para gRPC (estándar, no HTTP).

**Justificación:**
- Puerto estándar para gRPC
- Separado del puerto TCP (20000) y REST API

### 4.7 Docker y Despliegue

#### 4.7.1 Docker Multi-stage Build para Servidor Principal

**Decisión:** Implementar multi-stage build para reducir tamaño de imagen.

**Dockerfile propuesto:**
```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Server/Server.csproj Server/
COPY Common/Common.csproj Common/
RUN dotnet restore
COPY . .
WORKDIR /src/Server
RUN dotnet build -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Server.dll"]
```

**Justificación y números concretos:**
- **Single-stage (actual)**: ~500MB (incluye SDK)
- **Multi-stage**: ~150MB (solo runtime)
- **Ahorro**: ~350MB (70% reducción)
- **Ventajas**:
  - Imágenes más pequeñas = menos transferencia, menos almacenamiento
  - Menos superficie de ataque (sin herramientas de compilación)
  - Build más rápido en CI/CD (cache de stages)

#### 4.7.2 docker-compose.yaml Actualizado

**Servicios:**
1. `rabbitmq`: Contenedor oficial de RabbitMQ
2. `server`: Servidor principal (multi-stage build)
3. `logs-server`: Servidor de logs
4. `chat-server`: Servidor de chat
5. `client`: Cliente existente
6. `chat-client`: Cliente de chat (opcional en compose)

**Red:** Todos los servicios en la misma red Docker para comunicación interna.

---

## 5. Modelos de Datos

### 5.1 Modelo de Log Entry

```csharp
namespace LogsServer.Models;

public enum LogLevel
{
    INFO,
    WARNING,
    ERROR
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public int? ClaseId { get; set; }
    public string Evento { get; set; } = string.Empty;
    public LogLevel Nivel { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
```

### 5.2 Modelo de Chat Room

```csharp
namespace ChatServer.Models;

public class ChatRoom
{
    public string Link { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public ConcurrentDictionary<string, WebSocket> Participants { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? StartDateTime { get; set; }
}
```

### 5.3 Modelo de Mensaje de Chat

```csharp
namespace ChatServer.Models;

public class ChatMessage
{
    public string Type { get; set; } = "message";
    public string Usuario { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int ClassId { get; set; }
}
```

### 5.4 Modificación a ClassSession

```csharp
namespace Server.Data;

public class ClassSession
{
    // ... campos existentes ...
    
    /// <summary>
    /// URL opcional de webhook para notificaciones antes del inicio de la clase
    /// </summary>
    public string? WebhookUrl { get; set; }
    
    /// <summary>
    /// Timestamp del último webhook enviado (para prevenir envíos múltiples)
    /// </summary>
    public DateTime? WebhookSentAt { get; set; }
}
```

---

## 6. Protocolos de Comunicación

### 6.1 Protocolo RabbitMQ

**Exchange:** `logs-exchange` (topic)  
**Queue:** `logs-queue` (durable, auto-delete: false)  
**Routing Key Pattern:** `log.*`

**Formato de mensaje JSON:**
```json
{
  "timestamp": "2025-01-20T14:30:00Z",
  "usuario": "alice",
  "claseId": 123,
  "evento": "class.enrolled",
  "nivel": "INFO",
  "mensaje": "Usuario alice se registró en clase Programación",
  "metadata": {
    "claseNombre": "Programación",
    "cuposDisponibles": 5
  }
}
```

### 6.2 Protocolo REST API (Logs Server)

**Base URL:** `http://logs-server:5000/api`

**Endpoints:**

1. **GET /logs**
   - Query params: `usuario`, `claseId`, `fechaDesde`, `fechaHasta`, `nivel`, `evento`, `limit`
   - Response: `{ "logs": [...], "total": 10 }`

2. **GET /logs/stats**
   - Response: `{ "totalLogs": 100, "byLevel": {...}, "byEvent": {...} }`

### 6.3 Protocolo gRPC

**Puerto:** 50051

**Servicios:**
- `AuthenticationService.ValidateCredentials`
- `ClassVerificationService.VerifyClassLink`
- `ClassVerificationService.VerifyEnrollment`

### 6.4 Protocolo WebSocket

**URL:** `ws://chat-server:8080/chat?link={link}&username={username}&token={token}`

**Mensajes:**

**Cliente → Servidor:**
```json
{
  "type": "message",
  "mensaje": "Hola a todos"
}
```

**Servidor → Cliente:**
```json
{
  "type": "message",
  "usuario": "alice",
  "mensaje": "Hola a todos",
  "timestamp": "2025-01-20T14:30:00Z"
}
```

**Servidor → Cliente (sistema):**
```json
{
  "type": "system",
  "mensaje": "Usuario bob se unió al chat"
}
```

### 6.5 Protocolo Webhook

**Método:** HTTP POST  
**Content-Type:** `application/json`

**Payload:**
```json
{
  "claseId": 123,
  "nombre": "Programación",
  "description": "Clase de programación básica",
  "startDateTime": "2025-01-20T15:00:00Z",
  "durationMinutes": 60,
  "enrolledUsers": ["alice", "bob"],
  "link": "class-abc123",
  "webhookUrl": "https://webhook.site/abc123"
}
```

---

## 7. Plan de Implementación

### Fase 1: Infraestructura Base

1. **Configurar RabbitMQ en docker-compose**
   - Agregar servicio RabbitMQ
   - Configurar red Docker

2. **Implementar logging en Servidor Principal**
   - Crear `LoggingService` que publica a RabbitMQ
   - Integrar en `ClientHandler` para todos los eventos
   - Agregar eventos a operaciones existentes

### Fase 2: Servidor de Logs

1. **Crear proyecto LogsServer**
   - Nueva solución o agregar a existente
   - Configurar RabbitMQ Consumer
   - Implementar almacenamiento in-memory

2. **Implementar REST API**
   - Configurar ASP.NET Core Web API
   - Implementar endpoint `/api/logs` con filtros
   - Implementar endpoint `/api/logs/stats`
   - Agregar Dockerfile

3. **Integrar en docker-compose**
   - Agregar servicio `logs-server`
   - Configurar dependencias

### Fase 3: Integración gRPC

1. **Definir servicios en .proto**
   - Crear archivos `.proto` en Common
   - Generar código con `dotnet-grpc`

2. **Implementar gRPC Server en Servidor Principal**
   - Servicio de autenticación
   - Servicio de verificación de clases
   - Agregar configuración de puerto

3. **Implementar gRPC Client en Servidor de Chat**
   - Cliente de autenticación
   - Cliente de verificación

### Fase 4: Sistema de Webhooks

1. **Modificar ClassSession**
   - Agregar `WebhookUrl` y `WebhookSentAt`

2. **Modificar protocolo de registro**
   - Actualizar `HandleEnrollClass` para aceptar webhook URL
   - Validar URL antes de guardar

3. **Implementar BackgroundService**
   - Crear `WebhookService` que revisa clases periódicamente
   - Implementar HTTP POST asíncrono
   - Manejar errores y reintentos

### Fase 5: Servidor de Chat

1. **Crear proyecto ChatServer**
   - WebSocket server con ASP.NET Core
   - `ChatRoomManager` para gestionar salas
   - Validación vía gRPC antes de aceptar conexiones

2. **Implementar lógica de chat**
   - Broadcast de mensajes
   - Manejo de desconexiones
   - Heartbeat para detectar conexiones muertas

3. **Agregar Dockerfile y docker-compose**

### Fase 6: Cliente de Chat

1. **Crear proyecto ChatClient**
   - Interfaz de consola
   - WebSocket client
   - Autenticación inicial

2. **Implementar UI de consola**
   - Input de link/código
   - Input de credenciales
   - Visualización de mensajes en tiempo real

### Fase 7: Testing y Documentación

1. **Crear colección Postman**
   - Endpoints de REST API de logs
   - Documentar todos los endpoints

2. **Actualizar README**
   - Documentar nueva arquitectura
   - Instrucciones de despliegue
   - Ejemplos de uso

3. **Testing**
   - Probar flujo completo de logs
   - Probar chat con múltiples usuarios
   - Probar webhooks

---

## 8. Consideraciones Técnicas

### 8.1 Concurrencia y Thread Safety

- **RabbitMQ Consumer**: Debe ser thread-safe al agregar logs a la colección
- **ChatRoomManager**: Usar `ConcurrentDictionary` para participantes
- **BackgroundService Webhooks**: Usar locks al marcar webhooks como enviados

### 8.2 Manejo de Errores

- **RabbitMQ**: Implementar retry policy y dead letter queue
- **WebSocket**: Manejar desconexiones inesperadas
- **Webhooks**: Reintentos con exponential backoff, timeout de 5 segundos
- **gRPC**: Manejar errores de conexión y timeouts

### 8.3 Performance

- **Logs in-memory**: Implementar límite máximo (ej: 10,000 logs) con FIFO
- **WebSocket**: Heartbeat cada 30 segundos para detectar conexiones muertas
- **BackgroundService**: Revisar clases cada 10 segundos (balance entre precisión y carga)

### 8.4 Seguridad

- **gRPC**: Validar credenciales en cada request
- **WebSocket**: Validar token/sesión antes de aceptar conexión
- **REST API**: Considerar autenticación si se expone públicamente (no requerido en el proyecto)

### 8.5 Configuración

- **Puertos**:
  - TCP Socket: 20000
  - gRPC: 50051
  - REST API (Logs): 5000
  - WebSocket (Chat): 8080
  - RabbitMQ: 5672 (AMQP), 15672 (Management UI)

- **Variables de entorno**:
  - `RABBITMQ_HOST`: Host de RabbitMQ
  - `RABBITMQ_PORT`: Puerto de RabbitMQ
  - `GRPC_SERVER_HOST`: Host del servidor gRPC
  - `GRPC_SERVER_PORT`: Puerto del servidor gRPC

---

## 9. Diagramas de Flujo

### 9.1 Flujo de Logging

```
[Cliente] 
    │
    │ TCP: CMD_ENROLL_CLASS
    ▼
[Servidor Principal]
    │
    │ Ejecuta operación
    │
    ├─► Guarda en ClassService
    │
    └─► [LoggingService]
            │
            │ Serializa evento a JSON
            │
            ▼
        [RabbitMQ Producer]
            │
            │ Publica a exchange "logs-exchange"
            │
            ▼
        [RabbitMQ Broker]
            │
            │ Routing: "log.class.enrolled"
            │
            ▼
        [RabbitMQ Queue: "logs-queue"]
            │
            │ Consume mensaje
            │
            ▼
    [Servidor Logs - RabbitMQ Consumer]
            │
            │ Deserializa JSON
            │
            │ Agrega a ConcurrentBag<LogEntry>
            │
            ▼
        [LogStorageService]
```

### 9.2 Flujo de Consulta de Logs

```
[Cliente HTTP] (Postman/Browser)
    │
    │ GET /api/logs?usuario=alice&claseId=123
    │
    ▼
[Servidor Logs - REST API Controller]
    │
    │ Parsea query parameters
    │
    ▼
[LogFilterService]
    │
    │ Aplica filtros LINQ
    │
    ▼
[LogStorageService]
    │
    │ Retorna logs filtrados
    │
    ▼
[REST API Controller]
    │
    │ Serializa a JSON
    │
    ▼
[Cliente HTTP]
    │
    │ Recibe respuesta JSON
```

### 9.3 Flujo de Chat - Conexión Inicial

```
[Cliente Chat]
    │
    │ Solicita conexión con link + credenciales
    │
    ▼
[Servidor Chat - WebSocket Handler]
    │
    │ Extrae link, username, password
    │
    ▼
[gRPC Client]
    │
    │ ClassVerificationService.VerifyClassLink(link)
    │
    ▼
[Servidor Principal - gRPC Server]
    │
    │ Valida link contra ClassService
    │
    │ Retorna: { valid: true, classId: 123, className: "Programación" }
    │
    ▼
[gRPC Client]
    │
    ├─► Si link inválido: Rechaza conexión
    │
    └─► Si link válido:
            │
            │ AuthenticationService.ValidateCredentials(username, password)
            │
            ▼
        [Servidor Principal - gRPC Server]
            │
            │ Valida contra UserService
            │
            │ Retorna: { valid: true }
            │
            ▼
        [gRPC Client]
            │
            ├─► Si credenciales inválidas: Rechaza conexión
            │
            └─► Si credenciales válidas:
                    │
                    │ Acepta conexión WebSocket
                    │
                    │ Agrega a ChatRoom
                    │
                    ▼
                [ChatRoomManager]
                    │
                    │ Notifica a otros participantes
                    │
                    ▼
                [WebSocket Handler]
                    │
                    │ Retorna confirmación al cliente
```

### 9.4 Flujo de Chat - Envío de Mensaje

```
[Cliente Chat]
    │
    │ Usuario escribe mensaje
    │
    │ WebSocket Send: { "type": "message", "mensaje": "Hola" }
    │
    ▼
[Servidor Chat - WebSocket Handler]
    │
    │ Valida mensaje (longitud, formato)
    │
    │ Obtiene ChatRoom por link
    │
    ▼
[ChatRoomManager]
    │
    │ Itera sobre todos los participantes
    │
    │ (excepto el emisor)
    │
    ▼
[WebSocket Broadcast]
    │
    │ Envía a cada WebSocket:
    │ { "type": "message", "usuario": "alice", "mensaje": "Hola", "timestamp": "..." }
    │
    ▼
[Clientes Chat]
    │
    │ Reciben mensaje y lo muestran
```

### 9.5 Flujo de Webhook

```
[BackgroundService - WebhookService]
    │
    │ Cada 10 segundos:
    │
    ▼
[ClassService.GetAllClasses()]
    │
    │ Filtra clases con WebhookUrl != null
    │ Y StartDateTime - DateTime.Now <= 1 minuto
    │ Y WebhookSentAt == null
    │
    ▼
[Para cada clase encontrada:]
    │
    ├─► Construye payload JSON
    │
    ├─► HTTP POST asíncrono a WebhookUrl
    │   (timeout: 5 segundos)
    │
    ├─► Si éxito:
    │   │
    │   │ Marca WebhookSentAt = DateTime.Now
    │   │
    │   │ Guarda en ClassService
    │   │
    │   └─► Log: "Webhook enviado exitosamente"
    │
    └─► Si error:
        │
        │ Log: "Error enviando webhook: {error}"
        │
        │ (No marca como enviado, reintentará en próxima iteración)
        │
        ▼
    [Próxima iteración (10 segundos después)]
```

---

## 10. Resumen de Decisiones Clave

### Tecnologías y Justificaciones

| Decisión | Tecnología | Justificación |
|----------|-----------|---------------|
| Logging | RabbitMQ | Desacoplamiento, confiabilidad, asincronía |
| Consulta Logs | REST API | Estándar HTTP, fácil de probar con Postman |
| Validación Chat | gRPC | Baja latencia para comunicación interna síncrona |
| Chat en Vivo | WebSocket | Comunicación bidireccional en tiempo real |
| Almacenamiento Logs | In-Memory | Simplicidad, suficiente para alcance académico |
| Webhooks | BackgroundService | Tareas periódicas sin bloquear servidor principal |
| Docker | Multi-stage Build | Reducción de 70% en tamaño de imagen |

### Arquitectura de Comunicación

```
TCP Socket (Cliente ↔ Servidor Principal)
    ↓
RabbitMQ (Servidor Principal → Servidor Logs)
    ↓
REST API (Cliente HTTP ↔ Servidor Logs)
    ↓
gRPC (Servidor Chat ↔ Servidor Principal)
    ↓
WebSocket (Cliente Chat ↔ Servidor Chat)
```

### Principios Aplicados

1. **Separation of Concerns**: Cada servicio tiene responsabilidad única
2. **Loose Coupling**: RabbitMQ desacopla servicios
3. **Right Tool for the Job**: Tecnología apropiada para cada caso de uso
4. **Async First**: Operaciones no bloqueantes donde sea posible
5. **Fail-Safe**: Manejo robusto de errores y desconexiones

---

## 11. Próximos Pasos

1. Revisar y aprobar este diseño
2. Crear issues/tareas para cada fase de implementación
3. Configurar infraestructura base (RabbitMQ en docker-compose)
4. Implementar según plan por fases
5. Testing continuo durante desarrollo
6. Documentación actualizada en cada fase

---

**Fin del Documento de Diseño Arquitectónico**
