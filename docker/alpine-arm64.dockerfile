FROM mcr.microsoft.com/dotnet/sdk:10.0.101-alpine3.22@sha256:0fba99926e4f12405c78f37941312f00c1aadb178bd63616f5d96fc2af5a26a9

RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang=20.1.8-r0 \
        cmake=3.31.7-r1 \
        make=4.4.1-r3 \
        bash=5.2.37-r0 \
        alpine-sdk=1.1-r0 \
        protobuf=29.4-r0 \
        protobuf-dev=29.4-r0 \
        grpc=1.72.0-r0 \
        grpc-plugins=1.72.0-r0

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "102a6849303713f15462bb28eb10593bf874bbeec17122e0522f10a3b57ce442  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.308 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.416 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
