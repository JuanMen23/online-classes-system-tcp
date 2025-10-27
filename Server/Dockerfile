# Imagen base
FROM mcr.microsoft.com/dotnet/runtime:9.0

# Carpeta de trabajo
WORKDIR /app

# Copiar los archivos publicados desde el host
COPY Server/out/ .

# Crear carpeta para imágenes
RUN mkdir -p Image

# Comando de inicio
ENTRYPOINT ["dotnet", "Server.dll"]
