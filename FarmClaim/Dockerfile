FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FarmClaim.slnx ./
COPY FarmClaim.API/*.csproj FarmClaim.API/
COPY FarmClaim.Application/*.csproj FarmClaim.Application/
COPY FarmClaim.Domain/*.csproj FarmClaim.Domain/
COPY FarmClaim.Infrastructure/*.csproj FarmClaim.Infrastructure/
RUN dotnet restore FarmClaim.API/FarmClaim.API.csproj

COPY . .
RUN dotnet publish FarmClaim.API/FarmClaim.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --no-create-home appuser
USER appuser

COPY --from=build /app/publish .
COPY FarmClaim.API/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 5161
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD curl -f http://localhost:5161/api/v1/health || exit 1

ENTRYPOINT ["/entrypoint.sh"]
