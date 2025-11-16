#!/bin/bash

# Script para ejecutar el ChatClient dentro del contenedor

docker exec -it chat-client dotnet ChatClient.dll

