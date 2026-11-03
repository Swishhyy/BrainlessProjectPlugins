# Stats API (Core)

Core serves your XP/playtime stats through a small HTTP API:

- **Website leaderboard:** public, read straight from the browser ([BrainlessWebsite](https://github.com/Swishhyy/BrainlessWebsite)).
- **Discord bot:** signed private endpoints for `/pb-stats`, `/pb-link`, `/pb-optout` and so on ([BrainlessDiscordBot](https://github.com/Swishhyy/BrainlessDiscordBot), a free Cloudflare Worker).
- **Cloudflare (free)** gives the API a real `https://` address and encrypts it for visitors.

```
Website ──https──▶ Cloudflare ──▶ game server :port  (public leaderboard)
Discord ──https──▶ bot Worker ──https──▶ Cloudflare ──▶ game server  (signed requests)
```

**What is protected:** browsers and Discord talk to Cloudflare over HTTPS. Requests from the bot are signed with a shared secret (HMAC-SHA256 with a timestamp and one-time nonce). The secret itself is never sent, and a captured request can't be replayed or changed. The public leaderboard never includes Steam IDs. Cloudflare reaches the game server over plain HTTP (see step B3), so the leaderboard and stats replies are readable on that last hop. The secret and control of anyone's account are not exposed there.

You need a domain on Cloudflare (any cheap domain works; Cloudflare's plan is free).

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

## C. Connect the Discord bot and website

- **Discord bot:** follow the [BrainlessDiscordBot README](https://github.com/Swishhyy/BrainlessDiscordBot#readme). It needs `https://api.yourdomain.com` and the same secret as `shared_secret`.
- **Website:** set `statsApiUrl` in the website's `public/assets/js/config.js`, and set `allowed_origin` above to the website's address.

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
| GET | `/api/v1/status` (`online`, `players`, `maxPlayers`, `roundStarted`, `roundSeconds`) | Public |
| GET | `/api/v1/leaderboard?sort=xp\|playtime&limit=1-100` | Public |
| GET | `/api/v1/discord/{discordId}` | Signed |
| POST | `/api/v1/discord/{discordId}/link?code=CODE` | Signed |
| POST | `/api/v1/discord/{discordId}/optout` | Signed |

Signed requests carry `X-PB-Timestamp` (unix seconds, within 5 minutes), `X-PB-Nonce` (16–64 random letters/digits, never reused) and `X-PB-Signature` = lowercase hex `HMAC-SHA256(secret, timestamp + "\n" + nonce + "\n" + METHOD + "\n" + pathAndQuery)`. The bot implements it in `src/api.js`.

Both this repo (`tests/Brainless.Tests/StatsApiTests.cs`) and the bot (`test/bot.test.mjs`) check the same fixed signature value, so the two sides can't drift apart. Breaking changes would go to a new `/api/v2` path while `v1` keeps working.
