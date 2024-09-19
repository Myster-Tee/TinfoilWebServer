An example as to how I am running this container on my synology NAS. It is configured to run on port 50000.

docker run -d --name=tinfoilwebserver -p 50000:8080 -e APP_UID=1041 -e APP_GID=65538 -e TZ=America/Toronto -v /volume1/docker/tinfoilwebserver:/app/config -v /volume1/ConsoleGames/ROMs/Nintendo\ Switch:/host --restart always danifunker/tinfoilwebserver:latest
