# Sistema de Gestión de Clases Online

Aplicación cliente-servidor en **.NET 9** para la creación, gestión e inscripción a clases virtuales, desarrollada para la materia Programación de Redes (Universidad ORT Uruguay). Evolucionó a lo largo de tres entregas hasta incorporar comunicación TCP con protocolo propio, un servicio gRPC, un microservicio de logs desacoplado vía RabbitMQ y notificaciones por webhook.

## Arquitectura

Arquitectura en capas con biblioteca compartida entre cliente y servidor:

```
Client/     → Presentation + lógica de UI (menús, validación de entrada)
Server/     → Recepción de conexiones, lógica de negocio, persistencia
Common/     → Protocolo de comunicación, configuración compartida
LogsServer/ → Microservicio independiente (ASP.NET Core) que expone los logs vía REST/Swagger
```

## Funcionalidades

- Autenticación de usuarios (registro, login, logout) y gestión de sesión.
- CRUD completo de clases: creación, modificación, baja, búsqueda y listado, con imágenes.
- Sistema de inscripciones con validación de cupos y de tiempo.
- Reportes de actividad diaria generados desde consola del servidor (comando `REPORT`).
- Shutdown controlado del servidor, desconectando a todos los clientes de forma ordenada.

## Comunicación y protocolo

Protocolo de aplicación propio sobre TCP con estructura fija:

```
HEADER(3) | CMD(2) | LARGO(4) | DATOS(variable)
```

- `HEADER`: `REQ` (cliente → servidor) o `RES` (servidor → cliente).
- `CMD`: comando de dos dígitos (00–99).
- `LARGO`: longitud en bytes del payload, como entero de 32 bits.
- `DATOS`: contenido en UTF-8.

Además del canal TCP, el servidor expone un **servicio gRPC** para operaciones adicionales, y un servicio de **notificaciones por webhook** que revisa periódicamente el estado de las clases y notifica a un endpoint externo ante cambios relevantes.

## Concurrencia

- El servidor acepta clientes de forma asíncrona (`AcceptAsync`) y atiende a cada uno en una `Task` independiente (`HandleClientAsync`), evitando el modelo de un thread por cliente.
- Los recursos compartidos (clases, usuarios, clientes conectados) se protegen con `ConcurrentDictionary`.
- Las inscripciones a una misma clase usan **locking granular** con `SemaphoreSlim` por clase (`ClassService`), evitando condiciones de carrera sin bloquear al resto del sistema.
- Los servicios de dominio (`ClassService`, `UserService`, `ClientManager`) están implementados como Singleton thread-safe (`Lazy<T>`).

## Microservicio de logs (RabbitMQ)

`LogsServer` es una API REST independiente (ASP.NET Core + Swagger) que:

- Consume eventos publicados por el servidor principal en una cola de **RabbitMQ** (`logs-exchange`, tipo *topic*).
- Persiste y expone los logs de actividad vía endpoints REST filtrables por usuario, clase, nivel, evento, fecha o texto.
- Es tolerante a fallos: si RabbitMQ no está disponible, el servidor principal continúa funcionando y registra en consola en su lugar.

## Tecnologías

.NET 9 · C# · TCP Sockets · gRPC · RabbitMQ · ASP.NET Core (LogsServer) · Docker · async/await y Task Parallel Library · SemaphoreSlim / ConcurrentDictionary · Postman (colección de pruebas incluida en `/postman`)

## Cómo ejecutar

### Servidor principal
```bash
cd Server
dotnet run
```
El servidor queda escuchando en `0.0.0.0:20000` (configurable por variables de entorno `SERVER_IP` / `SERVER_PORT`).

### Cliente
```bash
cd Client
dotnet run
```

### Con Docker
```bash
docker build -t server-app ./Server
docker run -it -p 20000:20000 -e SERVER_IP=0.0.0.0 -e SERVER_PORT=20000 server-app
```

### Colección de Postman
Ver `/postman/README.md` para instrucciones de importación y pruebas del `LogsServer`.

## Autores

Matías Acevedo, Romina Valiunas y Juan Diego Meneses — Universidad ORT Uruguay.
