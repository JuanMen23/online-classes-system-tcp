# Etapa 1: build (compilación)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar los archivos de proyecto (.csproj)
COPY Common/Common.csproj Common/
COPY Client/Client.csproj Client/

# Restaurar dependencias
WORKDIR /src/Client
RUN dotnet restore Client.csproj

# Copiar el código fuente (sin duplicar carpetas)
COPY Common/ /src/Common/
COPY Client/ /src/Client/

# Compilar y publicar
RUN dotnet publish /src/Client/Client.csproj -c Release -o /app/publish

# Etapa 2: runtime (ejecución)
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Client.dll"]
