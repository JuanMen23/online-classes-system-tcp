FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Common/Common.csproj Common/
COPY ChatServer/ChatServer.csproj ChatServer/
RUN dotnet restore ChatServer/ChatServer.csproj

COPY . .
WORKDIR /src/ChatServer
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV GRPC_SERVER_URL=http://server:50051

EXPOSE 8080

ENTRYPOINT ["dotnet", "ChatServer.dll"]

