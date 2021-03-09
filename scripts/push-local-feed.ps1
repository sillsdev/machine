$feed = $args[0].TrimEnd('\', '/')

$packagesExist = Test-Path $feed\SIL.Machine.*.nupkg
if ( $packagesExist )
{
	Remove-Item $feed\SIL.Machine.*.nupkg
	dotnet nuget locals global-packages --clear	
}

Push-Location ..
dotnet pack src\SIL.Machine\SIL.Machine.csproj -o $feed
dotnet pack src\SIL.Machine.Morphology.HermitCrab\SIL.Machine.Morphology.HermitCrab.csproj -o $feed
dotnet pack src\SIL.Machine.Translation.Thot\SIL.Machine.Translation.Thot.csproj -o $feed
dotnet pack src\SIL.Machine.WebApi\SIL.Machine.WebApi.csproj -o $feed
dotnet pack src\SIL.Machine.Tool\SIL.Machine.Tool.csproj -o $feed
dotnet pack src\SIL.Machine.Plugin\SIL.Machine.Plugin.csproj -o $feed
Pop-Location
