# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

ARG BUILD_CONFIGURATION=Release

# copy only the csproj first (better cache)
COPY Hydra.Api/Hydra.Api.csproj Hydra.Api/

# restore
RUN dotnet restore Hydra.Api/Hydra.Api.csproj

# copy the rest of the source
COPY . .

# build & publish
WORKDIR /src/Hydra.Api
RUN dotnet build Hydra.Api.csproj -c $BUILD_CONFIGURATION -o /app/build --no-restore
RUN dotnet publish Hydra.Api.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Hydra.Api.dll"]
