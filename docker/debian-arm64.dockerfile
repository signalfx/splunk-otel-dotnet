FROM mcr.microsoft.com/dotnet/sdk:8.0.300-bookworm-slim

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "fcce8126a0fac2aa826f0bdf0f3c8e65f9c5f846ee1ab0774a03a7c56267556c  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 6.0.422 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 7.0.409 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

# uid 1000 is the uid of the user in our environement
# we should execute build process in the same context
# to have priviliges to modify data
RUN useradd -m -d /home/user -u 1000 user
USER user

WORKDIR /project
