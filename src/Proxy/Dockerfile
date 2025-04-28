FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-noble-chiseled-extra AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-noble AS build
WORKDIR /src
COPY ["src/Proxy/Proxy.csproj", "src/Proxy/"]
RUN dotnet restore "src/Proxy/Proxy.csproj"
COPY . .
WORKDIR "/src/src/Proxy"
RUN dotnet build "Proxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Proxy.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Proxy.dll"]