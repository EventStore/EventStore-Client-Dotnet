# BUILDER
FROM mcr.microsoft.com/dotnet/sdk:3.1-alpine AS builder

WORKDIR /app

# restore
COPY ./secure-with-tls.Docker.csproj ./
RUN dotnet restore ./secure-with-tls.Docker.csproj

# build
COPY ./Program.cs ./
RUN dotnet build -c Release --no-restore ./secure-with-tls.Docker.csproj

# package
RUN dotnet publish -c Release --no-build -o ./out ./secure-with-tls.Docker.csproj

# HOST
FROM mcr.microsoft.com/dotnet/runtime:3.1-buster-slim

WORKDIR /app

# copy binaries
COPY --from=builder /app/out .

# run
COPY ./certs/ca/ca.crt /usr/local/share/ca-certificates/eventstoredb_ca.crt
RUN chmod 644 /usr/local/share/ca-certificates/eventstoredb_ca.crt && update-ca-certificates

ENTRYPOINT ["dotnet", "secure-with-tls.Docker.dll"]
