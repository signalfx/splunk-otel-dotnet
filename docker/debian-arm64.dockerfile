FROM mcr.microsoft.com/dotnet/sdk:9.0.102-bookworm-slim

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "19b0a7890c371201b944bf0f8cdbb6460d053d63ddbea18cfed3e4199769ce17  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.405 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

# uid 1000 is the uid of the user in our environement
# we should execute build process in the same context
# to have priviliges to modify data
RUN useradd -m -d /home/user -u 1000 user
USER user

WORKDIR /project
