FROM nexus.gretzki.ddns.net:10501/alpine-dotnet-sdk:latest AS build-env
WORKDIR /app
COPY ./DocSearchAIO ./

RUN dotnet --version
RUN dotnet restore

RUN dotnet clean
#RUN dotnet nuget locals all --clear

RUN dotnet build
RUN dotnet publish -c Release -o out

FROM nexus.gretzki.ddns.net:10501/alpine-dotnet-runtime:latest
WORKDIR /app
COPY --from=build-env /app/out .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

RUN dotnet --list-runtimes
ENTRYPOINT ["dotnet", "DocSearchAIO.dll"]
