FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SecilStore_ConfigWebUI/SecilStore_ConfigWebUI.csproj", "SecilStore_ConfigWebUI/"]
COPY ["SecilStore_ConfigLibrary/SecilStore_ConfigLibrary.csproj", "SecilStore_ConfigLibrary/"]
RUN dotnet restore "SecilStore_ConfigWebUI/SecilStore_ConfigWebUI.csproj"
COPY . .
WORKDIR "/src/SecilStore_ConfigWebUI"
RUN dotnet build "SecilStore_ConfigWebUI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SecilStore_ConfigWebUI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SecilStore_ConfigWebUI.dll"] 