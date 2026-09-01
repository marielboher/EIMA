FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Eima.API.sln ./
COPY Eima.API/Eima.API.csproj Eima.API/
COPY AccesoDatos/AccesoDatos.csproj AccesoDatos/
COPY Controladores/Controladores.csproj Controladores/
COPY Entidades/Entidades.csproj Entidades/

RUN dotnet restore Eima.API/Eima.API.csproj

COPY . .
RUN dotnet publish Eima.API/Eima.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Eima.API.dll"]
