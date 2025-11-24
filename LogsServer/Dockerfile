FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Common/Common.csproj Common/
COPY LogsServer/LogsServer.csproj LogsServer/
RUN dotnet restore LogsServer/LogsServer.csproj

COPY . .
WORKDIR /src/LogsServer
RUN dotnet build -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .

EXPOSE 5001
CMD ["dotnet", "LogsServer.dll"]

