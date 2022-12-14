FROM mcr.microsoft.com/dotnet/sdk:7.0.101-alpine3.16

RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang \
        cmake \
        make \
        bash \
        alpine-sdk \
        protobuf \
        protobuf-dev \
        grpc

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "3d5a87bc29fb96e8dac8c2f88d95ff619c3a921903b4c9ff720e07ca0906d55e  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.404 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
