# Guía Rápida - Docker Interactivo

## Para Usar el Sistema con Docker (3 Terminales)

### Paso 1: Preparar el entorno

```bash
./scripts/start-all.sh
```

Este script:
- ✅ Compila el código más reciente
- ✅ Reconstruye las imágenes Docker (sin caché)
- ✅ Levanta los contenedores en background

### Paso 2: Conectarse interactivamente (abre 3 terminales)

**Terminal 1 - Servidor:**
```bash
./scripts/start-server.sh
```

**Terminal 2 - Cliente 1:**
```bash
./scripts/start-client1.sh
```

**Terminal 3 - Cliente 2:**
```bash
./scripts/start-client2.sh
```

Eso es todo. Ahora puedes usar el servidor y los clientes de forma interactiva.

## ¿Cómo funciona?

1. Los contenedores se levantan con `sleep infinity` (están vivos pero no ejecutan la app)
2. Los scripts `start-*.sh` te conectan interactivamente y ejecutan la aplicación
3. Tienes control total: escribir comandos, ver salida en tiempo real
4. Si cierras una terminal, el contenedor sigue vivo; puedes reconectarte

## Si cambias el código

Simplemente ejecuta de nuevo y reconecta:

```bash
./scripts/start-all.sh       # Recompila y actualiza todo
./scripts/start-server.sh    # Terminal 1
./scripts/start-client1.sh   # Terminal 2
./scripts/start-client2.sh   # Terminal 3
```

## Comandos útiles

```bash
docker-compose ps          # Ver estado
docker-compose logs -f     # Ver logs
docker-compose down        # Detener todo
```

## Transferencia de imágenes (clientes ↔ servidor)

- Las carpetas `Client/Uploads` y `Client/Downloads` del host están montadas dentro de cada cliente Docker como `/app/Uploads` y `/app/Downloads`.
- Para **subir** una imagen al servidor, copia el archivo a `Client/Uploads` y, en la UI del cliente, indica solo el nombre del archivo (por ejemplo `historia.png`). También puedes poner rutas absolutas dentro del contenedor (por ejemplo `/app/Uploads/historia.png`).
- Para **descargar** imágenes, el cliente las guardará automáticamente en `/app/Downloads`, por lo que aparecerán en `Client/Downloads` del host. El destino puede personalizarse con la variable `CLIENT_DOWNLOAD_DIR`.
- Si necesitas cambiar las rutas internas, ajusta `CLIENT_UPLOAD_DIR` o `CLIENT_DOWNLOAD_DIR` en `docker-compose.yaml`.

## Solución de problemas

**Los cambios no se reflejan:**
```bash
./scripts/start-all.sh  # Ya recompila todo con --no-cache
```

**Puerto ocupado:**
```bash
docker-compose down
./scripts/start-all.sh
```

**Contenedores no están corriendo:**
```bash
docker-compose up -d
```

## Colección de Postman

El repositorio incluye una colección de Postman para probar el Servidor de Logs.

**Ubicación:** `postman/LogsServer_Collection.json`

**Cómo usar:**
1. Importar la colección en Postman (File → Import)
2. Asegurarse de que el servidor de logs esté corriendo
3. La variable `base_url` está configurada a `http://localhost:5001`

**Documentación completa:** Ver `postman/README.md`

La colección incluye ejemplos para:
- Obtener todos los logs
- Filtrar por usuario, clase, nivel, evento, fecha, texto
- Combinar múltiples filtros
- Agregar logs de prueba

