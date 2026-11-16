FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src ./src
RUN dotnet publish src/FeriaAcademica.Api -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
# Uploaded files live in /data/uploads (Storage__Path); mount a volume there.
RUN mkdir -p /data/uploads && chown app:app /data/uploads
COPY --from=build /app/publish .
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FeriaAcademica.Api.dll"]
