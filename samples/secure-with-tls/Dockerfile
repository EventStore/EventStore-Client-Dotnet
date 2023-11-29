# BUILDER
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

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
RUN dotnet publish -c Release --no-build -o ./out ./secure-with-tls.csproj /p:UseAppHost=false

# HOST
FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app

# Copy binaries
COPY --from=builder /app/out .

# Copy pregenerated certificates into usr local CA location
COPY ./certs/ca/ca.crt /usr/local/share/ca-certificates/eventstoredb_ca.crt
# Set permissions and install certificates
RUN chmod 644 /usr/local/share/ca-certificates/eventstoredb_ca.crt && update-ca-certificates

# Run
ENTRYPOINT ["dotnet", "secure-with-tls.dll"]

#
#FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
#USER $APP_UID
#WORKDIR /app
#
#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#COPY ["secure-with-tls/secure-with-tls.csproj", "secure-with-tls/"]
#
## we need comment out project reference and uncomment the package reference
## If you're using the package reference those lines are not needed
#RUN sed -i 's/<ProjectReference\(.*\)\/>/<!-- <ProjectReference\1 -->/' secure-with-tls.csproj && \
#    sed -i 's/<!-- <PackageReference\(.*\)-->/<PackageReference\1/' secure-with-tls.csproj \
#    
#RUN dotnet restore "secure-with-tls/secure-with-tls.csproj"
#COPY . .
#WORKDIR "/src/secure-with-tls"
#RUN dotnet build "secure-with-tls.csproj" -c $BUILD_CONFIGURATION -o /app/build
#
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "secure-with-tls.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#
## Copy pregenerated certificates into usr local CA location
#COPY ./certs/ca/ca.crt /usr/local/share/ca-certificates/eventstoredb_ca.crt
## Set permissions and install certificates
#RUN chmod 644 /usr/local/share/ca-certificates/eventstoredb_ca.crt && update-ca-certificates
#
#ENTRYPOINT ["dotnet", "secure-with-tls.dll"]

