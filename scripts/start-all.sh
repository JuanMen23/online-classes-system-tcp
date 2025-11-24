#!/bin/bash

# Script para compilar, construir y preparar el entorno interactivo

set -e

echo "🔨 1. Compilando proyectos con código más reciente..."
dotnet publish Server/Server.csproj -c Release -o Server/out
dotnet publish Client/Client.csproj -c Release -o Client/out
dotnet publish LogsServer/LogsServer.csproj -c Release -o LogsServer/out
dotnet publish ChatServer/ChatServer.csproj -c Release -o ChatServer/out
dotnet publish ChatClient/ChatClient.csproj -c Release -o ChatClient/out

echo ""
echo "🐳 2. Reconstruyendo imágenes Docker (sin caché)..."
docker-compose build --no-cache

echo ""
echo "🛑 3. Limpiando volúmenes y contenedores anteriores..."
docker-compose down -v --remove-orphans

echo ""
echo "🚀 4. Levantando contenedores en background..."
docker-compose up -d

echo ""
echo "✅ Entorno preparado con el código más reciente!"
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  PARA USAR DE FORMA INTERACTIVA"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "Abre 4+ TERMINALES SEPARADAS:"
echo ""
echo "📟 Terminal 1 - Ver LogsServer en VIVO:"
echo "   docker-compose logs -f logs-server"
echo ""
echo "📟 Terminal 2 - Ver Servidor Principal en VIVO:"
echo "   docker-compose logs -f server"
echo ""
echo "📟 Terminal 3 - Servidor (interactivo):"
echo "   ./scripts/start-server.sh"
echo ""
echo "📟 Terminal 4 - Cliente 1 (interactivo):"
echo "   ./scripts/start-client1.sh"
echo ""
echo "📟 Terminal 5 - Cliente 2 (interactivo):"
echo "   ./scripts/start-client2.sh"
echo ""
echo "📟 Terminal 6 (OPCIONAL) - Ver LogsServer REST API:"
echo "   curl -s http://localhost:5001/api/logs | jq ."
echo ""
echo "📟 Terminal 7 - Chat Client (interactivo):"
echo "   ./scripts/start-chat-client.sh"
echo ""
echo "📟 Terminal 8 (OPCIONAL) - Ver logs del ChatServer:"
echo "   docker-compose logs -f chat-server"
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "🚀 RESUMEN DEL FLUJO:"
echo "   1. T1 & T2: Ver logs en tiempo real"
echo "   2. T3: Levantar servidor"
echo "   3. T4: Hacer login/logout desde cliente"
echo "   4. T7: Conectarse al ChatClient con link de clase (WebSocket)"
echo "   5. T1: Ver los logs llegando a LogsServer ✅"
echo "   6. T6: Consultar REST API para ver logs guardados"
echo "   7. T8: Observar actividad del ChatServer"
echo ""

