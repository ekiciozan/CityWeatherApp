FROM mcr.microsoft.com/mssql/server:2019-latest

# mssql-tools ve diğer bağımlılıkları yükle
RUN apt-get update && \
    apt-get install -y curl apt-transport-https && \
    curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - && \
    curl https://packages.microsoft.com/config/ubuntu/20.04/prod.list > /etc/apt/sources.list.d/mssql-release.list && \
    apt-get update && \
    ACCEPT_EULA=Y apt-get install -y msodbcsql17 mssql-tools unixodbc-dev && \
    echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bashrc && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# SQL Server başlangıç komutunu ayarla
ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=123456O.
EXPOSE 1433
CMD ["/opt/mssql/bin/sqlservr"]


# Uygulamanızı build etmek için kullanılan .NET SDK imajı
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Proje dosyasını kopyala ve restore et
COPY *.csproj ./
RUN dotnet restore

# Tüm kaynak kodları kopyala ve projeyi derle
COPY . ./
RUN dotnet publish -c Release -o out

# Çalışma zamanı için .NET runtime imajı
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Uygulamayı başlat
ENTRYPOINT ["dotnet", "CityWeatherApp.dll"]
