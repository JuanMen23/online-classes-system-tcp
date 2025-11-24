# Server/Dockerfile (multi-stage build)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Common/Common.csproj Common/
COPY Server/Server.csproj Server/
RUN dotnet restore Server/Server.csproj

COPY . .
WORKDIR /src/Server
RUN dotnet publish -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .

RUN mkdir -p /app/Images
ENV SERVER_IMAGE_DIRECTORY=/app/Images
EXPOSE 20000

ENTRYPOINT ["dotnet", "Server.dll"]
