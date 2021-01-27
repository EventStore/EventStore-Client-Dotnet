# BUILDER
FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS builder

WORKDIR /app

# Copy csproj and restore
COPY ./secure-with-tls.csproj ./

# we need comment out project reference and uncomment the package reference
# If you're using the package reference those lines are not needed
RUN sed -i 's/<ProjectReference\(.*\)\/>/<!-- <ProjectReference\1 -->/' secure-with-tls.csproj && \
    sed -i 's/<!-- <PackageReference\(.*\)-->/<PackageReference\1/' secure-with-tls.csproj

RUN dotnet restore ./secure-with-tls.csproj

# Copy source code and build
COPY ./Program.cs ./
RUN dotnet build -c Release --no-restore ./secure-with-tls.csproj

# Package
RUN dotnet publish -c Release --no-build -o ./out ./secure-with-tls.csproj

# HOST
FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim

WORKDIR /app

# Copy binaries
COPY --from=builder /app/out .

# Copy pregenerated certificates into usr local CA location
COPY ./certs/ca/ca.crt /usr/local/share/ca-certificates/eventstoredb_ca.crt
# Set permissions and install certificates
RUN chmod 644 /usr/local/share/ca-certificates/eventstoredb_ca.crt && update-ca-certificates

# Run
ENTRYPOINT ["dotnet", "secure-with-tls.dll"]
