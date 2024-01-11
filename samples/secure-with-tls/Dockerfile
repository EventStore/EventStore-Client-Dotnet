ARG BUILDER_IMG=mcr.microsoft.com/dotnet/sdk:8.0-jammy
ARG RUNNER_IMG=mcr.microsoft.com/dotnet/runtime:8.0.0-jammy
ARG APP_NAME="secure-with-tls"
ARG BUILD_CONFIGURATION=Release

#---------------------------------------------
# Prepare runtime image
#---------------------------------------------
FROM $RUNNER_IMG AS run
ARG APP_NAME

# copy pregenerated certificates into usr local CA location
COPY ./certs/ca/ca.crt /usr/local/share/ca-certificates/eventstoredb_ca.crt

# set permissions
RUN chmod 644 /usr/local/share/ca-certificates/eventstoredb_ca.crt

# install certificates
RUN update-ca-certificates

# configure entrypoint
WORKDIR /app
ENV ENTRYPOINT "dotnet ${APP_NAME}.dll"
ENTRYPOINT $ENTRYPOINT

#---------------------------------------------
# Build application
#---------------------------------------------
FROM $BUILDER_IMG AS build
ARG APP_NAME
ARG BUILD_CONFIGURATION

WORKDIR /src

# copy project file
COPY ${APP_NAME}.csproj /src/${APP_NAME}/

# helper var
ENV PROJECT_FILE_PATH=$APP_NAME/$APP_NAME.csproj

# we need comment out project reference and uncomment the package reference
# if you're using the package reference those lines are not needed
RUN sed -i 's/<ProjectReference\(.*\)\/>/<!-- <ProjectReference\1 -->/' $PROJECT_FILE_PATH && \
    sed -i 's/<!-- <PackageReference\(.*\)-->/<PackageReference\1/' $PROJECT_FILE_PATH

# restore all packages
RUN dotnet restore ${APP_NAME} --verbosity minimal

# keep the modified project file and restore it after the copy
RUN mv $PROJECT_FILE_PATH $PROJECT_FILE_PATH.bak
COPY . $APP_NAME/
RUN mv $PROJECT_FILE_PATH.bak $PROJECT_FILE_PATH

# build application
RUN dotnet build ${APP_NAME} -c $BUILD_CONFIGURATION --verbosity minimal

# set default work dir 
WORKDIR /src/$APP_NAME

#---------------------------------------------
# Publish application
#---------------------------------------------
FROM build AS publish
RUN dotnet publish -o /app/publish /p:UseAppHost=false --no-build

#---------------------------------------------
# Create final runtime image
#---------------------------------------------
FROM run AS app
COPY --from=publish /app/publish .


