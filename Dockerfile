FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src ./src
RUN dotnet publish src/FeriaAcademica.Api -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
# Uploaded files live in /data/uploads (Storage__Path); mount a volume there.
# The Data Protection keys (auth cookie, password links) are kept in ~/.aspnet/DataProtection-Keys: mount a volume there too,
# or every new container invalidates sessions and the links already emailed.
RUN mkdir -p /data/uploads /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /data/uploads /home/app/.aspnet
# The image has no fonts and PDFsharp needs a complete sans-serif family (regular, bold, italic, bold italic) to draw certificates and reports.
RUN apt-get update \
    && apt-get install -y --no-install-recommends fonts-liberation \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FeriaAcademica.Api.dll"]
