# MVC 5 with Keycloak

Keycloak runs in a Linux Docker container. The existing .NET Framework 4.7.2 MVC app runs in Visual Studio / IIS Express on Windows.

## Prerequisites

- Windows with Visual Studio and the ASP.NET and web development workload.
- .NET Framework 4.7.2 targeting pack.
- Docker Desktop running with Linux containers enabled.
- Git for cloning the repository.
- Available local ports: `8080` for Keycloak and `44317` for MVC HTTPS.

MVC remains an ASP.NET MVC 5 application hosted by IIS Express. Only Keycloak is containerized.

## Clone and run

```powershell
git clone https://github.com/rafimk/Keycloak-mvc-old.git
cd Keycloak-mvc-old
docker compose up -d
```

1. Start Docker Desktop with Linux containers enabled.
2. From this folder, run `docker compose up -d`.
3. Wait until `http://localhost:8080/realms/mvc-demo/.well-known/openid-configuration` responds. Use `docker compose logs -f keycloak` to see startup progress.
4. Open `mvc-keycklock-old.slnx` in Visual Studio, restore NuGet packages, select `keycloak-sample` as the startup project, and run with HTTPS at `https://localhost:44317/`.
5. The app redirects automatically to Keycloak. Sign in with **demo / Demo123!**. After successful login you return to the MVC page, which displays your username.

Visual Studio must support the `.slnx` solution format. If your version does not, open `keycloak-sample/keycloak-sample.csproj` directly. Restore packages through Visual Studio's NuGet package restore before building. Use F5 to debug or Ctrl+F5 to run without debugging. Use the project's HTTPS URL so the secure authentication cookies work.

## Changes made

| File | Change and purpose |
| --- | --- |
| `keycloak-sample/Startup.cs` | Added OWIN startup, cookie authentication, and OpenID Connect authentication against Keycloak. Uses the authorization code flow with PKCE, redeems the code, validates issuer and audience, and uses `preferred_username` as the signed-in user's name. |
| `keycloak-sample/App_Start/FilterConfig.cs` | Added a global `AuthorizeAttribute`, requiring an authenticated user for every MVC action. |
| `keycloak-sample/Web.config` | Registered OWIN startup, added Keycloak settings, and set ASP.NET authentication to `None` so OWIN handles authentication. |
| `keycloak-sample/keycloak-sample.csproj` | Included `Startup.cs` and the required OWIN and IdentityModel assembly references. |
| `keycloak-sample/packages.config` | Added OWIN hosting, cookies, OpenID Connect, and the supporting IdentityModel NuGet packages. |
| `keycloak-sample/Views/Home/Index.cshtml` | Added a signed-in message displaying the authenticated Keycloak username. |
| `compose.yaml` | Added Keycloak 26.8.0 in development mode, automatic realm import, a localhost-only port mapping, and a persistent data volume. |
| `docker/keycloak/mvc-demo-realm.json` | Added the `mvc-demo` realm, confidential `mvc-app` client, exact HTTPS callback URL, required S256 PKCE, and a demo user. |
| `.gitignore` | Excluded local tools, Visual Studio settings, downloaded packages, build output, and user-specific project settings. |
| `README.md` | Added setup, configuration, verification, troubleshooting, and start/stop instructions. |

The OWIN packages use version `4.2.3`; supporting IdentityModel/JWT packages use `5.3.0`, and Owin uses `1.0`. Package restore downloads the dependencies; they are not committed to Git.

## Login flow

1. The browser opens an MVC page, such as `/` or `/Home/About`.
2. The global authorization filter detects that the user is not signed in.
3. OWIN redirects the browser to the Keycloak login page in the `mvc-demo` realm.
4. Keycloak checks the credentials and returns the browser to `https://localhost:44317/signin-oidc`.
5. OWIN handles this callback, exchanges the authorization code, validates the response, and creates an HTTPS-only, HTTP-only local authentication cookie.
6. The browser returns to the originally requested MVC page. The home page displays the username.

`/signin-oidc` is handled by the authentication middleware; it does not need an MVC controller or view. Login failures do not grant access to protected MVC pages. Existing authenticated sessions may open MVC directly, and an existing Keycloak session may sign users in without showing the password form. Use a private browser session to test the initial login experience.

If the app reports `IDX20803`, Keycloak is not ready or Docker has stopped. Wait for the discovery URL in step 3 to respond, then restart the MVC app.

Every MVC action is protected by a global authorization filter. OWIN handles `/signin-oidc`, validates the login response, exchanges the authorization code using PKCE, and issues a secure local authentication cookie. Already signed-in users can open the app directly until their session expires. Use a private browser window to repeat the initial login flow.

## Local settings

| Setting in `keycloak-sample/Web.config` | Local value |
| --- | --- |
| `owin:AppStartup` | `keycloak_sample.Startup` |
| `Keycloak:Authority` | `http://localhost:8080/realms/mvc-demo` |
| `Keycloak:ClientId` | `mvc-app` |
| `Keycloak:ClientSecret` | `mvc-local-development-secret` |
| `Keycloak:RedirectUri` | `https://localhost:44317/signin-oidc` |
| `Keycloak:RequireHttpsMetadata` | `false` |

- Keycloak admin console: `http://localhost:8080/admin`, credentials **admin / admin-local-password**.
- Imported realm: `mvc-demo`; confidential client: `mvc-app`.
- App settings: `keycloak-sample/Web.config`.
- Realm and demo user: `docker/keycloak/mvc-demo-realm.json`.
- If changing the MVC HTTPS port, update `Keycloak:RedirectUri`, the client's redirect URI and web origin in Keycloak, and the IIS Express URL together. HTTP app URLs are unsupported because authentication cookies require HTTPS.
- Keycloak persists its data in a Docker volume. Realm imports only apply on first creation. For later edits, use the admin console; `docker compose down -v` deletes all local Keycloak data and allows a fresh import.
- The client uses the `openid profile email` scopes. Password grant and implicit flow are disabled.

## Stop and restart

1. Stop MVC using **Stop Debugging** in Visual Studio. If you ran without debugging, stop the site's IIS Express process from its system tray menu. Closing the browser alone does not stop the server.
2. Stop Keycloak while keeping its container and data:

```powershell
docker compose stop
```

Restart Keycloak with `docker compose up -d`, wait for it to be ready, and run MVC again from Visual Studio. To remove the container and network while keeping the database volume, use `docker compose down`.

## Verify the integration

1. Start Keycloak and MVC using the steps above.
2. Open a private browser window and visit `https://localhost:44317/Home/About`. Confirm that you are redirected to Keycloak before seeing the MVC page.
3. Sign in with `demo` and `Demo123!`. Confirm that you return to `/Home/About`.
4. Open the MVC home page and confirm that it displays **Signed in through Keycloak as demo**.
5. Repeat in a fresh private browser session with an incorrect password. Confirm that Keycloak rejects the login and MVC remains protected.

Validation performed during implementation: Debug and Release builds passed, the Docker Compose configuration validated, Keycloak imported the realm successfully, protected MVC pages returned an authentication redirect, and a browser login using the demo user returned successfully to the MVC home page with the username displayed. The incorrect-password step above is an additional manual check.

## Troubleshooting

| Symptom | What to check |
| --- | --- |
| Docker reports that it cannot connect to the engine | Start Docker Desktop and wait for the Linux engine to be ready, then run `docker compose up -d` again. |
| MVC shows `IDX20803` | Check that Keycloak is running and its discovery URL responds. Wait for startup to finish, then restart MVC. |
| Keycloak reports an invalid redirect URI | Confirm that the MVC HTTPS port and `Keycloak:RedirectUri` match the client's allowed redirect URI exactly. |
| Login repeats or the app does not retain the session | Open MVC over HTTPS at port `44317`; its authentication cookies require HTTPS. |
| Realm JSON changes do not appear | Existing realms are skipped during startup import. Update the realm using the admin console, or deliberately reset the local data volume as described above. |
| NuGet assemblies are missing during build | Restore the packages listed in `packages.config` through Visual Studio, then rebuild. |
| Port `8080` is already in use | Stop the conflicting service or change the Docker host port and the app's Keycloak authority together. |

View Keycloak status and logs with:

```powershell
docker compose ps
docker compose logs -f keycloak
```

## Deployment settings

These credentials and HTTP Keycloak settings are for local development. For deployment, configure HTTPS for Keycloak, set `Keycloak:RequireHttpsMetadata` to `true`, and replace the demo credentials and client secret.

Container setup follows the [Keycloak container guide](https://www.keycloak.org/server/containers).
