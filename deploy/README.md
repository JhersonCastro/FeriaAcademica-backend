# Despliegue en la VM (feria-academica.simplebucket.app)

Feria Académica corre en la VM compartida de simplebucket, detrás del Traefik del repositorio `proxy`. Son tres contenedores: **Postgres** (red interna `datos`, sin salida al proxy), la **API** (alias `feria-api`) y el **frontend** (nginx, alias `feria-web`). El proxy manda `/api` a la API y todo lo demás al frontend, así que comparten dominio y la cookie de sesión funciona sin ajustes.

## Estructura en la VM

```
~/proxy/                                         ← repositorio «proxy»
~/feria-academica/FeriaAcademica-backend/        ← este repositorio (aquí está el compose)
~/feria-academica/FeriaAcademica-frontend/       ← el frontend, al lado
```

## Primera vez

1. El registro DNS tipo A de `feria-academica.simplebucket.app` ya debe apuntar a la VM.
2. Clonar los dos repositorios:
   ```bash
   mkdir -p ~/feria-academica && cd ~/feria-academica
   git clone https://github.com/JhersonCastro/FeriaAcademica-backend.git
   git clone https://github.com/JhersonCastro/FeriaAcademica-frontend.git
   ```
3. Crear los secretos de la app (no van en Git):
   ```bash
   cd ~/feria-academica/FeriaAcademica-backend/deploy
   cp .env.example .env     # completar contraseñas, SMTP y súper admin
   ```
4. Agregar la función `feria` a `~/.bashrc` (ver «Funciones de la VM» en el README del `proxy`) y `source ~/.bashrc`.
5. En `~/proxy`: `git pull`, y agregar `FERIA_DOMINIO=feria-academica.simplebucket.app` a su `.env`. Luego `proxy up -d` para que tome la variable.
6. Levantar la app: `feria up -d --build`. Al arrancar, la API aplica las migraciones y crea el súper admin (`Database__MigrateOnStartup=true`).
7. Comprobar: `curl -sI https://feria-academica.simplebucket.app/` y `feria logs --tail=50 api`. Si el certificado tarda, `proxy logs --tail=30 proxy`.

## Actualizar

```bash
cd ~/feria-academica/FeriaAcademica-backend && git pull
cd ~/feria-academica/FeriaAcademica-frontend && git pull
feria up -d --build
```

## Notas

- **Correo**: Brevo solo acepta envíos desde las IPs autorizadas; la IP de la VM ya está autorizada.
- **Archivos subidos**: viven en el volumen `uploads` (`Storage__Path=/data/uploads`).
- **Respaldo de la base** (un `pg_dump` programado, guardado fuera de la VM):
  ```bash
  feria exec -T postgres pg_dump -U feria feria_academica | gzip > feria-$(date +%F).sql.gz
  ```
- Las variables `__` del compose equivalen a las claves con `:` de `appsettings` y user-secrets (`Smtp__Host` = `Smtp:Host`).
