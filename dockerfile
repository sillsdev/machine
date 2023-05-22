FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build-env
WORKDIR /app

RUN apt-get update && apt-get install -y g++ curl cmake

# Copy everything
COPY . .
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish ./src/SIL.Machine.Serval.EngineServer/SIL.Machine.Serval.EngineServer.csproj -c Release -o out_engine_server
RUN dotnet publish ./src/SIL.Machine.Serval.JobServer/SIL.Machine.Serval.JobServer.csproj -c Release -o out_job_server

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 as production
# libgomp needed for thot.
RUN apt-get update && apt-get install -y libgomp1
WORKDIR /app
COPY --from=build-env /app/out_engine_server ./engine_server
COPY --from=build-env /app/out_job_server ./job_server

CMD ["bash"]