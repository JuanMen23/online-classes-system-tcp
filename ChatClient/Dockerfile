FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ChatClient/ChatClient.csproj ChatClient/
RUN dotnet restore ChatClient/ChatClient.csproj

COPY . .
WORKDIR /src/ChatClient
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV CHAT_SERVER_URL=ws://chat-server:8080/chat

ENTRYPOINT ["dotnet", "ChatClient.dll"]

