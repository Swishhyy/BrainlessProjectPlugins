# Project Brainless — Stats API and Discord bot

Three pieces work together:

- **Core plugin (game server):** keeps the XP/playtime stats and serves them through a small API (`stats_api` in the Core config).
- **Cloudflare (free):** gives the API a real `https://` address and encrypts it for visitors.
- **This folder:** the Discord bot as a free Cloudflare Worker. It answers `/pb-stats`, `/pb-playtime`, `/pb-top`, `/pb-link` and `/pb-optout` by asking the API. Nothing runs between commands, so there is no bot process to host or keep online.

```
Website ──https──▶ Cloudflare ──▶ game server :port  (public leaderboard)
Discord ──https──▶ Worker (this folder) ──https──▶ Cloudflare ──▶ game server  (signed requests)
```

**What is protected:** browsers and Discord talk to Cloudflare over HTTPS. Requests from the bot to the API are signed with a shared secret (HMAC-SHA256 with a timestamp and one-time nonce). The secret itself is never sent, and a captured request can't be replayed or changed. The public leaderboard never includes Steam IDs. Cloudflare reaches the game server over plain HTTP (see step B3), so the leaderboard and stats replies are readable on that last hop. The secret and control of anyone's account are not exposed there.

You need a domain on Cloudflare (any cheap domain works; Cloudflare's plan is free) and Node.js 20+ on your own PC for the one-time setup.

---

## A. Turn on the API (game server)

1. **Pick a TCP port** that your host forwards to the server. SCP:SL only uses UDP, so most panels also forward TCP on the game port: try the game port first (yours is `9025`). If that doesn't work, add a second port/allocation in your host panel.
2. **Make a secret** of at least 32 characters. On your PC run one of:
   ```bash
   node -e "console.log(require('crypto').randomBytes(32).toString('hex'))"
   ```
   ```bash
   openssl rand -hex 32
   ```
3. **Edit the Core config** (`project_brainless_core`):
   ```yaml
   stats_api:
     enabled: true
     port: 9025                      # the TCP port from step 1
     bind_host: '*'
     shared_secret: 'PASTE_THE_SECRET'
     allowed_origin: 'https://yourwebsite.com'   # or * while testing
     trust_cloudflare_headers: true
     requests_per_minute: 60
     leaderboard_cache_seconds: 30
     discord_command_prefix: pb
   ```
   You can leave `shared_secret` empty and put the value in the `BRAINLESS_API_SECRET` environment variable instead.
4. **Fully restart** the server. In Remote Admin, `pbcore status` should say `stats API: on (port …, Discord bot endpoints enabled)`.
5. Check `http://YOUR_SERVER_IP:PORT/api/v1/health` in a browser. You should see `{"ok":true,"bot":true}`.

## B. Put HTTPS in front (Cloudflare)

1. **DNS → Add record:** type `A`, name `api`, IPv4 = your game server's IP, **Proxied** (orange cloud on).
2. **Rules → Origin Rules → Create rule:** *Hostname equals* `api.yourdomain.com` → **Destination Port: Rewrite to** your API port. (Cloudflare only connects to a few ports by itself; this rule sends traffic to yours. It's available on the free plan.)
3. **Rules → Configuration Rules → Create rule:** *Hostname equals* `api.yourdomain.com` → **SSL: Flexible**. The plugin speaks plain HTTP, so Cloudflare must not expect a certificate from the game server. Doing this as a rule keeps the rest of your domain on its current SSL mode.
4. Open `https://api.yourdomain.com/api/v1/leaderboard`. You should get JSON with a padlock in the browser.

Optional hardening: if your host lets you firewall the port, allow only [Cloudflare's IP ranges](https://www.cloudflare.com/ips/). If someone can reach the port directly, they can bypass the per-IP rate limit by faking `CF-Connecting-IP`; in that case set `trust_cloudflare_headers: false` (everyone behind Cloudflare then shares one rate-limit bucket, so raise `requests_per_minute`).

## C. Deploy the Discord bot (Cloudflare Worker)

1. **Discord Developer Portal** → [Applications](https://discord.com/developers/applications) → *New Application*. If you already used the old built-in bot, reuse that application.
   - *General Information*: copy the **Application ID** and **Public Key**.
   - *Bot*: *Reset Token* and copy it. You only need it once, for step 5. Don't put it anywhere else.
   - *Installation* (or *OAuth2 → URL Generator*): scope `applications.commands` (add `bot` if you want it listed as a member) → open the link → add it to your server.
2. **Edit `wrangler.toml`:** set `API_URL` to `https://api.yourdomain.com`, and `GUILD_ID` to your Discord server ID (right-click the server with Developer Mode on → *Copy Server ID*). Keep `COMMAND_PREFIX` the same as `discord_command_prefix` in the plugin.
3. **Deploy** from this folder:
   ```bash
   npx wrangler@4 login
   ```
   ```bash
   npx wrangler@4 secret put DISCORD_PUBLIC_KEY
   ```
   ```bash
   npx wrangler@4 secret put API_SECRET
   ```
   ```bash
   npm run deploy
   ```
   Paste the Public Key and the secret from A2 when asked. The deploy prints a URL like `https://brainless-discord-bot.<you>.workers.dev`.
4. **Developer Portal → General Information → Interactions Endpoint URL:** paste that Worker URL and *Save Changes*. Discord tests it straight away; it only saves if the Public Key is right.
5. **Register the slash commands** (once, and again only if you change the prefix):
   ```bash
   DISCORD_TOKEN=xxx DISCORD_APPLICATION_ID=123 DISCORD_GUILD_ID=456 npm run register
   ```
   On Windows PowerShell:
   ```powershell
   $env:DISCORD_TOKEN="xxx"; $env:DISCORD_APPLICATION_ID="123"; $env:DISCORD_GUILD_ID="456"; npm run register
   ```
6. **Test:** in game, F1 `.pblink` → in Discord `/pb-link code:XXXXXXXX` → `/pb-stats`, `/pb-top`.

Replies are private (only the person who ran the command sees them), and mentions are disabled. If the game server is offline, commands answer "Stats are unavailable right now" instead of failing.

## Website leaderboard

From any page:

```js
const res = await fetch('https://api.yourdomain.com/api/v1/leaderboard?limit=25&sort=xp'); // or sort=playtime
const { updated, players } = await res.json();
// players: [{ rank, name, level, xp, playtimeHours }]
```

Set `allowed_origin` to your website's address so browsers allow the request. Results are cached for `leaderboard_cache_seconds`.

## API reference

| Method | Path | Access |
| --- | --- | --- |
| GET | `/api/v1/health` | Public |
| GET | `/api/v1/leaderboard?sort=xp\|playtime&limit=1-100` | Public |
| GET | `/api/v1/discord/{discordId}` | Signed |
| POST | `/api/v1/discord/{discordId}/link?code=CODE` | Signed |
| POST | `/api/v1/discord/{discordId}/optout` | Signed |

Signed requests carry `X-PB-Timestamp` (unix seconds, within 5 minutes), `X-PB-Nonce` (16–64 random letters/digits, never reused) and `X-PB-Signature` = lowercase hex `HMAC-SHA256(secret, timestamp + "\n" + nonce + "\n" + METHOD + "\n" + pathAndQuery)`. See `src/api.js`.

## Developing

```bash
npm test
```

The tests share a fixed signature with the plugin's C# tests, so the two sides can't drift apart.
