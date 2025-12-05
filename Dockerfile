# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY MultiVendorEcommerce.csproj ./
RUN dotnet restore

# copy the rest of the source
COPY . ./

# publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Render and many PaaS platforms expose the port via PORT env var
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

COPY --from=build /app/publish .

# Default port for local use
EXPOSE 8080

ENTRYPOINT ["dotnet", "MultiVendorEcommerce.dll"]
