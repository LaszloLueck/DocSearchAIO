FROM nexus.gretzki.ddns.net:10501/alpine-dotnet-sdk:latest AS base
WORKDIR /app

COPY DocSearchAIO.sln .
COPY DocSearchAIO ./DocSearchAIO/

RUN dotnet --version
RUN dotnet restore

RUN dotnet publish -c Release -o /app/out

FROM nexus.gretzki.ddns.net:10501/alpine-dotnet-runtime:latest
WORKDIR /app
COPY --from=build-env /app/out .
ENV ASPNETCORE_URLS=http://+:5000
RUN dotnet --list-runtimes
ENTRYPOINT ["dotnet", "DocSearchAIO.dll"]
