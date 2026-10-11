# ======================
# BUILD STAGE
# ======================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# copy entire repository
COPY . .

# Restore only the API dependency graph. The solution also contains the
# Windows Forms employee agent, which is not part of this Linux service.
RUN dotnet restore Skolenza.api/Skolenza.api.csproj

# publish api
RUN dotnet publish Skolenza.api/Skolenza.api.csproj \
    --no-restore \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# ======================
# RUNTIME STAGE
# ======================
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# Fix Supabase Npgsql kerberos dependency
RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# copy build output
COPY --from=build /app/publish .

# Render port binding
ENV ASPNETCORE_URLS=http://+:${PORT}

EXPOSE 8080

ENTRYPOINT ["dotnet", "Skolenza.api.dll"]
