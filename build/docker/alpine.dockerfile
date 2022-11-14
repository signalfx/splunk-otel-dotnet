FROM mcr.microsoft.com/dotnet/sdk:6.0.403-alpine3.16

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
    && echo "2c51739e843231e9829a777fc6038e50b31d420e12a593edb9e858d3587ea361  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 3.1.425 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
