FROM mcr.microsoft.com/dotnet/sdk:9.0.304-bookworm-slim@sha256:ae000be75dac94fc40e00f0eee903289e985995cc06dac3937469254ce5b60b6

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "9d310fe9c949b9635f27c2d59c4c742101ebad52a16db5265ecc0fdb29ec7f45  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.413 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
