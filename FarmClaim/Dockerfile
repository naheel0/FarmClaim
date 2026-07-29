FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FarmClaim.slnx ./
COPY FarmClaim.API/*.csproj FarmClaim.API/
COPY FarmClaim.Application/*.csproj FarmClaim.Application/
COPY FarmClaim.Domain/*.csproj FarmClaim.Domain/
COPY FarmClaim.Infrastructure/*.csproj FarmClaim.Infrastructure/
RUN dotnet restore FarmClaim.API/FarmClaim.API.csproj

COPY . .
RUN dotnet publish FarmClaim.API/FarmClaim.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y netcat-openbsd && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
COPY FarmClaim.API/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 5161

ENTRYPOINT ["/entrypoint.sh"]
