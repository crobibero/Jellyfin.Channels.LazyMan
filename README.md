# Jellyfin.Channels.LazyMan

LazyMan Channel for Jellyfin

Get started with LazyMan at https://reddit.com/r/LazyMan

Hostsfile directions: https://www.reddit.com/r/LazyMan/wiki/hostsfile

You must edit your hosts file to use this plugin!
for docker either use `--add-host` or `extra_hosts`

Steps to install:
1. Download [latest release](https://github.com/crobibero/Jellyfin.Channels.LazyMan/releases/latest).
2. Extract Jellyfin.Channels.LazyMan.dll to the Jellyfin plugins directory
3. Add host entries
4. Restart Jellyfin

or add repository:
https://raw.githubusercontent.com/crobibero/Jellyfin.Channels.LazyMan/master/manifest.json

docker compose sample:

```
jellyfin:
    container_name: jellyfin
    image: jellyfin/jellyfin:latest
    restart: always
    extra_hosts:
        - "mf.svc.nhl.com:{ip}"
        - "mlb-ws-mf.media.mlb.com:{ip}"
        - "playback.svcs.mlb.com:{ip}"
```
