FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /src
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

FROM gitlab.gretzki.ddns.net/laszlo/alpine-dotnet-sdk:latest as build AS build
COPY DocSearchAIO.sln .
COPY DocSearchAIO ./DocSearchAIO/
COPY DocSearchAIO_Test ./DocSearchAIO_Test/
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

#FROM build as test
RUN dotnet test --verbosity normal

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocSearchAIO.dll"]
