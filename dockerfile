FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build-env
WORKDIR /app

RUN apt-get update && apt-get install -y g++ curl cmake

# Copy everything
COPY . .
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 as production
WORKDIR /app
COPY --from=build-env /app/out .
CMD ["bash"]