#!/bin/bash
dotnet tool restore
dotnet restore
dotnet csharpier --check .
if [ $? -ne 0 ]; then
  exit 1
fi
dotnet build --no-restore -c Release
if [ $? -ne 0 ]; then
  exit 1
fi
dotnet test --verbosity normal
