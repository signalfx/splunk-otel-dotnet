FROM mcr.microsoft.com/dotnet/sdk:9.0.304-alpine3.21@sha256:430bd56f4348f9dd400331f0d71403554ec83ae1700a7dcfe1e1519c9fd12174

RUN apk update \
    && apk upgrade \
    && apk add --no-cache --update \
        clang=19.1.4-r0 \
        cmake=3.31.1-r0 \
        make=4.4.1-r2 \
        bash=5.2.37-r0 \
        alpine-sdk=1.1-r0 \
        protobuf=24.4-r4 \
        protobuf-dev=24.4-r4 \
        grpc=1.62.1-r2 \
        grpc-plugins=1.62.1-r2

ENV IsAlpine=true
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

# Install older sdks using the install script
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "16142ad5999ffdb168a578f5f74d7c76dd72f13ce2e914e8e9eb87f5fcc63629  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.413 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

# uid 1000 is the uid of the user in our environement
# we should execute build process in the same context
# to have priviliges to modify data
RUN addgroup -S appgroup && adduser -S user -G appgroup -u 1000
USER user

WORKDIR /project
