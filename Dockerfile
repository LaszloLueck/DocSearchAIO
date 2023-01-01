FROM registry.gitlab.com/laszlo.lueck/containerruntimeglobal/containerruntimeglobal_build-full_x64:latest AS build-env
WORKDIR /app
COPY ./DocSearchAIO ./

RUN dotnet --info
RUN dotnet restore
RUN dotnet clean

RUN dotnet build --no-restore
RUN dotnet publish --no-restore -c Release -o out

FROM registry.gitlab.com/laszlo.lueck/containerruntimeglobal/containerruntimeglobal_runtime-full_x64:latest
WORKDIR /app
COPY --from=build-env /app/out .
EXPOSE 5000
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5000;https://+:5002
ENTRYPOINT ["dotnet", "DocSearchAIO.dll"]
