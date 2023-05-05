FROM mcr.microsoft.com/dotnet/sdk:7.0.201-alpine3.16

RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang=13.0.1-r1 \
        cmake=3.23.5-r0 \
        make=4.3-r0 \
        bash=5.1.16-r2 \
        alpine-sdk=1.0-r1 \
        protobuf=3.18.1-r3 \
        protobuf-dev=3.18.1-r3 \
        grpc=1.46.3-r0

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "e7e05ef4c1980e4d75dd5c27c1c387ff0dac8931595583b9ff6fa362da7c2de9  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.406 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
