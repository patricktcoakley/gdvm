FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl gnupg \
    && curl -fsSL https://apt.fury.io/nushell/gpg.key \
        | gpg --dearmor -o /etc/apt/trusted.gpg.d/fury-nushell.gpg \
    && echo "deb https://apt.fury.io/nushell/ /" \
        > /etc/apt/sources.list.d/fury.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends nushell \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

ADD . /app
WORKDIR /app
