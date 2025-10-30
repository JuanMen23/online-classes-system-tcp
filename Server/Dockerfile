# Server/Dockerfile (single-stage, usa runtime)
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

# Copiar los archivos publicados (por eso publicamos localmente)
COPY Server/out/ .

# Crear carpeta donde el host podrá ver las imágenes
RUN mkdir -p /app/Images
ENV SERVER_IMAGE_DIRECTORY=/app/Images
EXPOSE 20000

ENTRYPOINT ["dotnet", "Server.dll"]
