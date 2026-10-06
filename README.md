# MVC 5 with Keycloak

Keycloak runs in a Linux Docker container. The existing .NET Framework 4.7.2 MVC app runs in Visual Studio / IIS Express on Windows.

## Run

1. Start Docker Desktop with Linux containers enabled.
2. From this folder, run `docker compose up -d`.
3. Wait until `http://localhost:8080/realms/mvc-demo/.well-known/openid-configuration` responds. Use `docker compose logs -f keycloak` to see startup progress.
4. Open `mvc-keycklock-old.slnx` in Visual Studio, restore NuGet packages, select `keycloak-sample` as the startup project, and run with HTTPS at `https://localhost:44317/`.
5. The app redirects automatically to Keycloak. Sign in with **demo / Demo123!**. After successful login you return to the MVC page, which displays your username.

If the app reports `IDX20803`, Keycloak is not ready or Docker has stopped. Wait for the discovery URL in step 3 to respond, then restart the MVC app.

Every MVC action is protected by a global authorization filter. OWIN handles `/signin-oidc`, validates the login response, exchanges the authorization code using PKCE, and issues a secure local authentication cookie. Already signed-in users can open the app directly until their session expires. Use a private browser window to repeat the initial login flow.

## Local settings

- Keycloak admin console: `http://localhost:8080/admin`, credentials **admin / admin-local-password**.
- Imported realm: `mvc-demo`; confidential client: `mvc-app`.
- App settings: `keycloak-sample/Web.config`.
- Realm and demo user: `docker/keycloak/mvc-demo-realm.json`.
- If changing the MVC HTTPS port, update `Keycloak:RedirectUri`, the client's redirect URI and web origin in Keycloak, and the IIS Express URL together. HTTP app URLs are unsupported because authentication cookies require HTTPS.
- Keycloak persists its data in a Docker volume. Realm imports only apply on first creation. For later edits, use the admin console; `docker compose down -v` deletes all local Keycloak data and allows a fresh import.
- Stop Keycloak with `docker compose down`.

These credentials and HTTP Keycloak settings are for local development. For deployment, configure HTTPS for Keycloak, set `Keycloak:RequireHttpsMetadata` to `true`, and replace the demo credentials and client secret.

Container setup follows the [Keycloak container guide](https://www.keycloak.org/server/containers).
