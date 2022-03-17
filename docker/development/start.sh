#!/bin/sh
dotnet build /app/src/SIL.Machine.WebApi
dotnet run --project /app/src/SIL.Machine.WebApi.Server