#!/bin/bash

# Script para compilar, construir y preparar el entorno interactivo

set -e

echo "🔨 1. Compilando proyectos con código más reciente..."
dotnet publish Server/Server.csproj -c Release -o Server/out
dotnet publish Client/Client.csproj -c Release -o Client/out

echo ""
echo "🐳 2. Reconstruyendo imágenes Docker..."
docker-compose build --no-cache

echo ""
echo "🛑 3. Deteniendo contenedores anteriores..."
docker-compose down

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
echo "Abre 3 TERMINALES SEPARADAS y ejecuta en cada una:"
echo ""
echo "📟 Terminal 1 - Servidor:"
echo "   ./scripts/start-server.sh"
echo ""
echo "📟 Terminal 2 - Cliente 1:"
echo "   ./scripts/start-client1.sh"
echo ""
echo "📟 Terminal 3 - Cliente 2:"
echo "   ./scripts/start-client2.sh"
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "💡 Tip: Los contenedores están corriendo en background."
echo "   Cuando ejecutes los scripts, se conectarán interactivamente."
echo ""

