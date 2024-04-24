FROM mcr.microsoft.com/dotnet/sdk:8.0.204-alpine3.18

RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang=16.0.6-r1 \
        cmake=3.26.5-r0 \
        doas=6.8.2-r4 \
        make=4.4.1-r1 \
        bash=5.2.15-r5 \
        alpine-sdk=1.0-r1 \
        protobuf=3.21.12-r2 \
        protobuf-dev=3.21.12-r2 \
        grpc=1.54.2-r0 \
        grpc-plugins=1.54.2-r0

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "170a3ec239a351f8d7c14bec424b286bd9468f4d928bdb7600f6424ea7f13927  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.421 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 7.0.408 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

RUN adduser -D user -G wheel -u 1000; \
    echo 'user:123' | chpasswd; \
    echo 'permit :wheel as root' > /etc/doas.d/doas.conf
USER user

WORKDIR /project