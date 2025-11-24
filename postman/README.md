# Colección de Postman - Servidor de Logs

Esta carpeta contiene la colección de Postman para probar el Servidor de Logs del Sistema de Clases Online.

## 📋 Contenido

- **LogsServer_Collection.json**: Colección completa de Postman con todos los endpoints y ejemplos

## 🚀 Cómo Importar la Colección

### Opción 1: Postman Desktop App

1. Abre Postman
2. Click en **Import** (botón superior izquierdo)
3. Selecciona **File** o **Upload Files**
4. Navega a `postman/LogsServer_Collection.json`
5. Click en **Import**

### Opción 2: Postman Web

1. Abre [Postman Web](https://web.postman.co/)
2. Click en **Import** (botón superior izquierdo)
3. Selecciona **File**
4. Sube el archivo `LogsServer_Collection.json`
5. Click en **Import**

### Opción 3: Desde URL (si está en Git)

1. En Postman, click en **Import**
2. Selecciona **Link**
3. Ingresa la URL del archivo raw desde GitHub/GitLab
4. Click en **Continue** → **Import**

## ⚙️ Configuración

### Variable de Entorno

La colección usa una variable `base_url` que por defecto es:
```
http://localhost:5001
```

**Para cambiar la URL base:**
1. En Postman, selecciona la colección "Logs Server API"
2. Click en la pestaña **Variables**
3. Modifica el valor de `base_url` según tu entorno:
   - Local: `http://localhost:5001`
   - Docker: `http://localhost:5001` (puerto mapeado)
   - Remoto: `http://<ip>:5001`

## 📚 Endpoints Incluidos

### GET /api/logs

Obtiene logs con filtros opcionales. La colección incluye ejemplos para:

1. **Obtener todos los logs** - Sin filtros
2. **Filtrar por usuario** - `?usuario=romi`
3. **Filtrar por clase** - `?claseId=1`
4. **Filtrar por nivel** - `?nivel=INFO`
5. **Filtrar por evento** - `?evento=login`
6. **Filtrar por rango de fechas** - `?fechaDesde=...&fechaHasta=...`
7. **Buscar texto** - `?contiene=clase`
8. **Limitar resultados** - `?limit=10`
9. **Filtros combinados** - Múltiples ejemplos

### POST /api/logs/test

Agrega un log de prueba al servidor. Incluye:
- Ejemplo completo con todos los campos
- Ejemplo mínimo con solo campos requeridos

## 🔍 Parámetros de Filtrado

Todos los parámetros son **opcionales** y se pueden **combinar**:

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `usuario` | string | Filtrar por nombre de usuario | `?usuario=romi` |
| `claseId` | int | Filtrar por ID de clase | `?claseId=1` |
| `nivel` | string | Filtrar por nivel (INFO, WARNING, ERROR) | `?nivel=INFO` |
| `evento` | string | Filtrar por tipo de evento | `?evento=login` |
| `fechaDesde` | DateTime | Fecha de inicio (ISO 8601) | `?fechaDesde=2025-01-20T00:00:00Z` |
| `fechaHasta` | DateTime | Fecha de fin (ISO 8601) | `?fechaHasta=2025-01-20T23:59:59Z` |
| `contiene` | string | Buscar texto en mensajes | `?contiene=clase` |
| `limit` | int | Límite de resultados (default: 100) | `?limit=50` |

## 📝 Ejemplos de Uso

### Ejemplo 1: Obtener logs de un usuario específico
```
GET http://localhost:5001/api/logs?usuario=romi
```

### Ejemplo 2: Obtener logs de una clase con nivel INFO
```
GET http://localhost:5001/api/logs?claseId=1&nivel=INFO
```

### Ejemplo 3: Buscar logs de creación de clases en una fecha
```
GET http://localhost:5001/api/logs?evento=class_create&fechaDesde=2025-01-20T00:00:00Z&fechaHasta=2025-01-20T23:59:59Z
```

### Ejemplo 4: Agregar un log de prueba
```json
POST http://localhost:5001/api/logs/test
Content-Type: application/json

{
    "evento": "test_event",
    "mensaje": "Este es un log de prueba",
    "usuario": "testuser",
    "nivel": "INFO",
    "claseId": 1
}
```

## ✅ Prerrequisitos

Antes de usar la colección, asegúrate de que:

1. **El servidor de logs esté corriendo:**
   ```bash
   docker-compose ps logs-server
   ```

2. **El puerto 5001 esté accesible:**
   ```bash
   curl http://localhost:5001/api/logs
   ```

3. **Hay logs en el servidor** (si no hay, primero usa el cliente para generar actividad o agrega logs de prueba)

## 🧪 Testing

### Flujo de Prueba Recomendado

1. **Verificar que el servidor responde:**
   - Ejecuta "1. Obtener todos los logs"

2. **Probar filtros individuales:**
   - Ejecuta los requests 2-8 para probar cada filtro

3. **Probar filtros combinados:**
   - Ejecuta los requests 9-10 para ver combinaciones

4. **Agregar logs de prueba:**
   - Ejecuta "11. Agregar log de prueba" o "12. Agregar log de prueba (mínimo)"
   - Luego verifica que aparezcan con "1. Obtener todos los logs"

## 📊 Formato de Respuesta

### GET /api/logs

```json
{
    "logs": [
        {
            "timestamp": "2025-01-20T14:30:00Z",
            "usuario": "romi",
            "evento": "login",
            "nivel": "INFO",
            "mensaje": "Usuario inició sesión",
            "claseId": null,
            "metadata": {}
        }
    ],
    "total": 1,
    "filters": {
        "usuario": "romi",
        "claseId": null,
        "nivel": null,
        "evento": null,
        "Desde": null,
        "Hasta": null,
        "Contiene": null,
        "limit": 100
    }
}
```

### POST /api/logs/test

```json
{
    "message": "Log agregado",
    "log": {
        "timestamp": "2025-01-20T14:30:00Z",
        "usuario": "testuser",
        "evento": "test_event",
        "nivel": "INFO",
        "mensaje": "Este es un log de prueba",
        "claseId": 1,
        "metadata": {}
    }
}
```

## 🔗 Enlaces Útiles

- **Postman Documentation**: https://learning.postman.com/docs/getting-started/introduction/
- **Servidor de Logs**: http://localhost:5001
- **RabbitMQ Management UI**: http://localhost:15672 (admin/admin)

## 📝 Notas

- Los logs se almacenan en memoria, por lo que se perderán al reiniciar el servidor
- El límite por defecto es 100 logs, pero se puede cambiar con el parámetro `limit`
- Las fechas deben estar en formato ISO 8601 (ej: `2025-01-20T14:30:00Z`)
- Todos los filtros son case-sensitive excepto `contiene` que busca en el mensaje

