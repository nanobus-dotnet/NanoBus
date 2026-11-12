
dotnet pack src/NanoBus.Abstractions/NanoBus.Abstractions.csproj -c Release -o ./nupkg
dotnet pack src/NanoBusCore/NanoBusCore.csproj -c Release -o ./nupkg
dotnet pack src/NanoBus.Transports.Demo/NanoBus.Transports.Demo.csproj -c Release -o ./nupkg
dotnet pack src/NanoBus.Transports.InMemory/NanoBus.Transports.InMemory.csproj -c Release -o ./nupkg
