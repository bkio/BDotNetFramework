FROM microsoft/dotnet:3.0-runtime
WORKDIR /app

# Install production dependencies.
# Copy csproj and restore as distinct layers.
COPY *.csproj ./
RUN dotnet restore

# Copy local code to the container image.
COPY . ./
WORKDIR /app

# Build a release artifact.
RUN dotnet publish -c Release -o out

# Use Microsoft's official runtime .NET image.
# https://hub.docker.com/_/microsoft-dotnet-core-aspnet/
FROM microsoft/dotnet:3.0-runtime
WORKDIR /app
COPY --from=build /app/out ./

EXPOSE 8080
ENTRYPOINT [ "dotnet", "BSecretManagementService.dll" ]