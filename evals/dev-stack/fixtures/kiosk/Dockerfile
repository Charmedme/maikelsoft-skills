FROM mcr.microsoft.com/dotnet/sdk:10.0.100-rc.1 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Kiosk.Api/Kiosk.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0.0-rc.1
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080
ENV Jwt__SigningKey=kiosk-test-only-jwt-signing-key-container-5b8e21
USER root
EXPOSE 8080
ENTRYPOINT ["dotnet", "Kiosk.Api.dll"]
