FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["DocSearchAIO.csproj", "DocSearchAIO/"]
RUN dotnet restore "DocSearchAIO/DocSearchAIO.csproj"
WORKDIR "/src/DocSearchAIO"
COPY . ./
RUN dotnet build "DocSearchAIO.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocSearchAIO.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocSearchAIO.dll"]
