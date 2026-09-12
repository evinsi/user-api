# 1. AŞAMA: derleme (SDK imajı — derleyiciyi içerir)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Önce sadece csproj'u kopyala ve paketleri restore et (katman önbelleği için)
COPY UserApi.csproj ./
RUN dotnet restore

# Sonra tüm kaynağı kopyala ve yayına derle
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# 2. AŞAMA: çalıştırma (küçük runtime imajı — sadece çalıştırır)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Container içinde 8080 portunu dinle
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UserApi.dll"]
