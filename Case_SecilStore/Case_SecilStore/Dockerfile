# Hata ayıklama kapsayıcınızı özelleştirme ve Visual Studio'nun daha hızlı hata ayıklama için görüntülerinizi derlemek üzere bu Dockerfile'ı nasıl kullandığı hakkında bilgi edinmek için https://aka.ms/customizecontainer sayfasına bakın.

# Bu aşama, VS'den hızlı modda çalıştırıldığında kullanılır (Hata ayıklama yapılandırması için varsayılan olarak ayarlıdır)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80


# Bu aşama, hizmet projesini oluşturmak için kullanılır
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Case_SecilStore/Case_SecilStore.csproj", "Case_SecilStore/"]
COPY ["SecilStore_ConfigLibrary/SecilStore_ConfigLibrary.csproj", "SecilStore_ConfigLibrary/"]
COPY ["SecilStore_ConfigLibrary.Tests/SecilStore_ConfigLibrary.Tests.csproj", "SecilStore_ConfigLibrary.Tests/"]
RUN dotnet restore "Case_SecilStore/Case_SecilStore.csproj"
COPY . .
WORKDIR "/src/Case_SecilStore"
RUN dotnet build "Case_SecilStore.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Bu aşama, son aşamaya kopyalanacak hizmet projesini yayımlamak için kullanılır
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Case_SecilStore.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Bu aşama üretimde veya VS'den normal modda çalıştırıldığında kullanılır (Hata Ayıklama yapılandırması kullanılmazken varsayılan olarak ayarlıdır)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Case_SecilStore.dll"]