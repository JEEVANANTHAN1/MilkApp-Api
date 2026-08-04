FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MilkApp.Api.csproj ./
RUN dotnet restore MilkApp.Api.csproj

COPY . .
RUN dotnet publish MilkApp.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

CMD ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet MilkApp.Api.dll
