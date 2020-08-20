$feed = $args[0].TrimEnd('\', '/')
Remove-Item $feed\SIL.Machine.*.nupkg

Push-Location ..
dotnet pack src\SIL.Machine\SIL.Machine.csproj -o $feed
dotnet pack src\SIL.Machine.Morphology.HermitCrab\SIL.Machine.Morphology.HermitCrab.csproj -o $feed
dotnet pack src\SIL.Machine.Translation.Thot\SIL.Machine.Translation.Thot.csproj -o $feed
dotnet pack src\SIL.Machine.WebApi\SIL.Machine.WebApi.csproj -o $feed
Pop-Location

dotnet nuget locals global-packages --clear