FROM mcr.microsoft.com/dotnet/sdk:9.0.102-alpine3.20

RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang=17.0.6-r1 \
        cmake=3.29.3-r0 \
        make=4.4.1-r2 \
        bash=5.2.26-r0 \
        alpine-sdk=1.0-r1 \
        protobuf=24.4-r1 \
        protobuf-dev=24.4-r1 \
        grpc=1.62.1-r0 \
        grpc-plugins=1.62.1-r0

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "16142ad5999ffdb168a578f5f74d7c76dd72f13ce2e914e8e9eb87f5fcc63629  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.405 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

# uid 1000 is the uid of the user in our environement
# we should execute build process in the same context
# to have priviliges to modify data
RUN addgroup -S appgroup && adduser -S user -G appgroup -u 1000
USER user

WORKDIR /project
