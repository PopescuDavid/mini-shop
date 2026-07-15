FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Shop.Api/Shop.Api.csproj", "src/Shop.Api/"]
RUN dotnet restore "src/Shop.Api/Shop.Api.csproj"
COPY src/ src/
RUN dotnet publish "src/Shop.Api/Shop.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Shop.Api.dll"]
