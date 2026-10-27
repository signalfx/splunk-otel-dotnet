FROM mcr.microsoft.com/dotnet/sdk:10.0.401-alpine3.23@sha256:3f9c03432d664163a90d20e3ed0a3784d0aa82c1b9cbd1a7dd4609fede95669e

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
# Update the GitHub CLI version and architecture-specific checksum together.
ENV GITHUB_CLI_VERSION="2.101.0"
ENV GITHUB_CLI_ARCH="arm64"
ENV GITHUB_CLI_SHA256="b57e8063f18862647c9d22727c32e9da1b963f8bf9db648fe123a6975695640f"

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

RUN curl -sSfL "https://github.com/cli/cli/releases/download/v${GITHUB_CLI_VERSION}/gh_${GITHUB_CLI_VERSION}_linux_${GITHUB_CLI_ARCH}.tar.gz" --output /tmp/gh.tar.gz \
    && echo "${GITHUB_CLI_SHA256}  /tmp/gh.tar.gz" | sha256sum -c \
    && tar -xzf /tmp/gh.tar.gz -C /tmp \
    && mv "/tmp/gh_${GITHUB_CLI_VERSION}_linux_${GITHUB_CLI_ARCH}/bin/gh" /usr/local/bin/gh \
    && rm -rf /tmp/gh.tar.gz "/tmp/gh_${GITHUB_CLI_VERSION}_linux_${GITHUB_CLI_ARCH}"

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "082f7685e156738a1b2e2ed8381a621870d4ce8e8c59278034556f05c186eb2e  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 9.0.316 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.423 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
