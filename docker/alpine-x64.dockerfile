FROM mcr.microsoft.com/dotnet/sdk:10.0.302-alpine3.23@sha256:d8ee39817ca03a3757288e83c37ed73cc969a286c603b827c7cbe33add1c2d1c

# renovate: datasource=repology depName=cmake
ENV CMAKE_VERSION="4.1.3-r0"
# renovate: datasource=repology depName=clang
ENV CLANG_VERSION="21.1.2-r2"
# renovate: datasource=repology depName=make
ENV MAKE_VERSION="4.4.1-r3"
# renovate: datasource=repology depName=bash
ENV BASH_PACKAGE_VERSION="5.3.3-r1"
# renovate: datasource=repology depName=alpine-sdk
ENV ALPINE_SDK_VERSION="1.1-r0"
# renovate: datasource=repology depName=protobuf
ENV PROTOBUF_VERSION="31.1-r1"
# renovate: datasource=repology depName=protobuf-dev
ENV PROTOBUF_DEV_VERSION="31.1-r1"
# renovate: datasource=repology depName=grpc
ENV GRPC_VERSION="1.76.0-r2"
# renovate: datasource=repology depName=grpc-plugins
ENV GRPC_PLUGINS_VERSION="1.76.0-r2"

RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        cmake="${CMAKE_VERSION}" \
        clang="${CLANG_VERSION}" \
        make="${MAKE_VERSION}" \
        bash="${BASH_PACKAGE_VERSION}" \
        alpine-sdk="${ALPINE_SDK_VERSION}" \
        protobuf="${PROTOBUF_VERSION}" \
        protobuf-dev="${PROTOBUF_DEV_VERSION}" \
        grpc="${GRPC_VERSION}" \
        grpc-plugins="${GRPC_PLUGINS_VERSION}"

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "082f7685e156738a1b2e2ed8381a621870d4ce8e8c59278034556f05c186eb2e  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.313 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.420 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
