# Installation

## Using Pre-built Binaries

### Runtime Prerequisites

* [.NET Core 3.0 with ASP.NET Core](https://dotnet.microsoft.com/download)

### Configuring StreamNight

You'll need to edit and place two configuration files alongside the DLLs in the program's working directory.

#### appsettings.json

This file contains the settings for the frontend.

| Property                        | Edit            | Example                            | Source                                                                                                            | Notes                                                                                                                                                                                                 |
| ------------------------------- | --------------- | ---------------------------------- | ----------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Anything in `Logging`           | Not recommended | N/A                                | [Microsoft Documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.2) | Change `LogLevel` if you're having issues with the application and need extra debug information.                                                                                                      |
| `AllowedHosts`                  | Yes             | "stream.example.com;localhost"     | Your stream's public hostname, plus `localhost`.                                                                  | This enables ASP.NET's host filtering middleware. See [this](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-2.2#host-filtering) page for more information. |
| Anything in `ConnectionStrings` | Not recommended | N/A                                | N/A                                                                                                               | `StreamNightContextConnection` controls where the user database is stored. It's generally safe to leave this at its default.                                                                          |
| `Discord:AppId`                 | Yes             | "123456789012345678"               | Your Discord application's client ID.                                                                             |                                                                                                                                                                                                       |
| `Discord:AppSecret`             | Yes             | "abcdefghijklmnopqrstuvwxyz123456" | Your Discord application's client secret.                                                                         |                                                                                                                                                                                                       |
| `Discord:Callback`              | Not recommended | "/signin-discord"                  | N/A                                                                                                               | This is where Discord's authentication redirects back to after signing in. The value has to match what you've entered in your bot's OAuth redirects in Discord's developer portal.                    |
| `TokenPassword`                 | Yes             | Any string                         | A securely generated random string.                                                                               | This is used to encrypt login links from the bot.                                                                                                                                                     |
| `TokenKey`                      | Yes             | Any string                         | A securely generated random string.                                                                               | This is used to verify login links from the bot.                                                                                                                                                      |
| `HmacKey`                       | Yes             | Any string                         | A securely generated random string.                                                                               | This is used to verify messages from the bot.                                                                                                                                                         |
| `PlaylistPath`                  | Yes             | master.m3u8                        | Your master HLS playlist's path, relative to StreamNight's working directory.                                     | This is the HLS master playlist that is served to clients. The file *must* exist.                                                                                                                     |

#### botconfig.json

| Property            | Edit  | Example                                                       | Source                                            | Notes                                                                                                                       |
| ------------------- | ----- | ------------------------------------------------------------- | ------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `token`             | Yes   | "abcdefghijklmnopqrstuvwxyz123456abcdefghijklmnopqrstuvwxyz1" | Your Discord bot's token.                         |                                                                                                                             |
| `guild`             | Yes   | 123456789012345678                                            | Your Discord guild's ID.                          | Should be a number and not a string.                                                                                        |
| `channel`           | Yes   | 123456789012345678                                            | Your bridged channel's ID.                        | Should be a number and not a string. Must match the webhook's channel.                                                      |
| `stream_role`       | Yes   | "Stream Control"                                              | The name of the role that can control the stream. | Make sure only trusted members have this role. Holders are able to enable/disable the stream and remotely refresh browsers. |
| `admin_role`        | Yes   | "Administrators"                                              | The name of the role that can change the config.  | Make sure only admins have this role. Holders have stream control permissions and can also view the bot's config file.      |
| `use_server_logo`   | Maybe | true/false                                                    | N/A                                               | This property sets whether StreamNight will attempt to use your server's logo in place of the static one defined later.     |
| `stream_name`       | Yes   | "Powered by StreamNight"                                      | The title of your stream.                         | This property is used in the page titles.                                                                                   |
| `short_server_name` | Yes   | "StreamNight"                                                 | A short, one-word version of your server's name.  | This property is used to set prefixes for server emotes in the picker.                                                      |
| `hmac_key`          | Yes   | Any string                                                    | A securely generated random string.               | This is used to generate verification codes for messages. It must be identical to the one set in `appsettings.json`.         |
| `token_key`         | Yes   | Any string                                                    | A securely generated random string.               | This is used to generate verification codes for login links. It must be identical to the one set in `appsettings.json`.     |
| `token_password`    | Yes   | Any string                                                    | A securely generated random string.               | This is used to encrypt login links. It must be identical to the one set in `appsettings.json`.                             |
| `api_url`           | Yes   | "https://stream.example.com/"                                 | Your stream's public hostname.                    | This is used to generate login links. It must have the trailing forward slash.                                              |
| `webhook_url`       | Yes   | "https://discordapp.com/api/webhooks/123456789012345678/key"  | The webhook URL for your bridged channel.         | This is where chat messages are sent. It must feed to the channel set above.                                                |

### Adding Files

#### Default Mobile Logo

Although StreamNight can display your server's icon on the mobile header, it still needs a default one to fall back to if it can't download it or if you'd prefer to use a static icon.

Place the PNG icon at `wwwroot/images/staticlogo.png`.

#### HLS Master Playlist

Create the file specified in `appsettings.json` and paste the contents of your master playlist.

A typical playlist might look like this:

```
#EXTM3U
#EXT-X-VERSION:3
#EXT-X-STREAM-INF:PROGRAM-ID=1,CLOSED-CAPTIONS=NONE,BANDWIDTH=4192000,RESOLUTION=1920x1080
https://stream.example.com/hls/1080.m3u8
#EXT-X-STREAM-INF:PROGRAM-ID=1,CLOSED-CAPTIONS=NONE,BANDWIDTH=2628000,RESOLUTION=1280x720
https://stream.example.com/hls/720.m3u8
#EXT-X-STREAM-INF:PROGRAM-ID=1,CLOSED-CAPTIONS=NONE,BANDWIDTH=1596000,RESOLUTION=960x540
https://stream.example.com/hls/480.m3u8
#EXT-X-STREAM-INF:PROGRAM-ID=1,CLOSED-CAPTIONS=NONE,BANDWIDTH=564000,RESOLUTION=640x360
https://stream.example.com/hls/360.m3u8
```

The quality selector will display the vertical resolution of each entry.

### Running StreamNight

Run `dotnet StreamNight.dll --server.urls=http://0.0.0.0:5050` to start StreamNight listening on all interfaces at port `5050`.

While it's possible to use StreamNight's internal HTTP server (ASP.NET's Kestrel) instead of using another web server such as Microsoft IIS, Apache, or Nginx, we recommend using a reverse proxy as StreamNight doesn't currently support HTTPS.

We recommend [Caddy](https://caddyserver.com/) as it gives you automatic HTTPS for free, but any web server with support for WebSocket proxying should work.

##### Sample Caddy Configuration

```
stream.example.com {
    tls PUT YOUR EMAIL HERE!
    gzip
    proxy / stream.example.com:5050 {
        transparent
        websocket
        header_downstream Access-Control-Allow-Origin "*"
    }
}
```

Note: This assumes you're not hosting the HLS files on the same machine. You'll need to add another location block if you're doing so.

## Compiling StreamNight

This content has been moved to a dedicated page.