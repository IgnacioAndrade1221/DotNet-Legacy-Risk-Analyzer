FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY ["src/Dna.Cli/Dna.Cli.csproj", "src/Dna.Cli/"]
COPY ["src/Dna.Core/Dna.Core.csproj", "src/Dna.Core/"]
COPY ["src/Dna.Engine/Dna.Engine.csproj", "src/Dna.Engine/"]
COPY ["src/Dna.Infrastructure/Dna.Infrastructure.csproj", "src/Dna.Infrastructure/"]

RUN dotnet restore "src/Dna.Cli/Dna.Cli.csproj"

COPY . .

WORKDIR "/app/src/Dna.Cli"
RUN dotnet publish "Dna.Cli.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Dna.Cli.dll"]