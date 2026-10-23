FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Orders.Api/Orders.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Libraries for Chrome. ChromeInstaller downloads the browser at the first start.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates libnss3 libatk-bridge2.0-0t64 libcups2t64 libgbm1 \
       libxcomposite1 libxdamage1 libxrandr2 libxkbcommon0 libpango-1.0-0 libasound2t64 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
