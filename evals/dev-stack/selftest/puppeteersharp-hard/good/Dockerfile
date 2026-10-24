# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Orders.Api/Orders.Api.csproj -c Release -o /app

# chrome-headless-shell, pinned to Chrome.DefaultBuildId of PuppeteerSharp 25.14.0.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS chrome
ARG CHROME_BUILD=155.0.8059.79
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl unzip \
    && curl -fsSLo /tmp/shell.zip "https://storage.googleapis.com/chrome-for-testing-public/${CHROME_BUILD}/linux64/chrome-headless-shell-linux64.zip" \
    && unzip -q /tmp/shell.zip -d /opt \
    && rm /tmp/shell.zip

# Full aspnet image (not chiseled): Chrome needs system libraries and fonts.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends tini fonts-liberation fonts-noto-core \
       libasound2t64 libatk-bridge2.0-0t64 libatk1.0-0t64 libcups2t64 libdbus-1-3 libgbm1 \
       libnss3 libpango-1.0-0 libxcomposite1 libxdamage1 libxfixes3 libxkbcommon0 libxrandr2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=chrome /opt/chrome-headless-shell-linux64 /opt/chrome-headless-shell
ENV PUPPETEER_EXECUTABLE_PATH=/opt/chrome-headless-shell/chrome-headless-shell \
    LANG=C.UTF-8
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
# Chrome runs with its sandbox on: run the container with a seccomp profile that allows clone, clone3 and unshare.
USER $APP_UID
ENTRYPOINT ["/usr/bin/tini", "--", "dotnet", "Orders.Api.dll"]
