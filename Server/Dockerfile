FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Server/ Server/
COPY Assets/Scripts/ArenaSimulation.cs Assets/Scripts/ArenaMap.cs Assets/Scripts/ArenaNavigation.cs Assets/Scripts/NetworkProtocol.cs Assets/Scripts/
RUN dotnet restore Server/PharmaBrawl.Server.csproj --configfile Server/NuGet.Config && dotnet publish Server/PharmaBrawl.Server.csproj -c Release --no-restore -o /app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENV PORT=8787
EXPOSE 8787
ENTRYPOINT ["dotnet","PharmaBrawl.Server.dll"]
