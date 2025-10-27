# Client/Dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

COPY Client/out/ .

# variables por defecto (pueden ser sobreescritas por docker-compose)
ENV SERVER_IP=server-container
ENV SERVER_PORT=20000
ENTRYPOINT ["dotnet", "Client.dll"]
