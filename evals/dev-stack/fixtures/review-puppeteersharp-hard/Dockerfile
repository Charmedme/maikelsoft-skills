FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Orders.Api/Orders.Api.csproj -c Release -o /app

# Full aspnet image (not chiseled): Chrome needs system libraries and fonts.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Libraries for Chrome. ChromeInstaller downloads the browser at the first start.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates tini fonts-liberation fonts-noto-core \
       libnss3 libatk-bridge2.0-0t64 libatk1.0-0t64 libcups2t64 libdbus-1-3 libgbm1 \
       libxcomposite1 libxdamage1 libxfixes3 libxrandr2 libxkbcommon0 libpango-1.0-0 libasound2t64 \
    && rm -rf /var/lib/apt/lists/*
ENV LANG=C.UTF-8
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
# Chrome keeps its sandbox: the app runs as the non-root user of the .NET image.
USER $APP_UID
ENTRYPOINT ["/usr/bin/tini", "--", "dotnet", "Orders.Api.dll"]
