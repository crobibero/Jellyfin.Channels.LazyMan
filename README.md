# Jellyfin.Channels.LazyMan

LazyMan Channel for Jellyfin

Get started with LazyMan at https://reddit.com/r/LazyMan

Hostsfile directions: https://www.reddit.com/r/LazyMan/wiki/hostsfile

Just extract the release to the plugins folder and restart Jellyfin.

You must edit your hosts file to use this plugin!
for docker either use `--add-host` or `extra_hosts`

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
