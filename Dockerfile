FROM bitnami/aspnet-core:6
RUN apt update && apt install p7zip-full rename -y
RUN wget https://github.com/Myster-Tee/TinfoilWebServer/releases/download/v1.6.0/TinfoilWebServer_v1.6.0_Framework-Dependent-linux-x64.zip
RUN 7z x TinfoilWebServer_v1.6.0_Framework-Dependent-linux-x64.zip
RUN rm -f TinfoilWebServer_v1.6.0_Framework-Dependent-linux-x64.zip
RUN rename -f 's/TinfoilWebServer_v1.6.0_Framework-Dependent-linux-x64\\TinfoilWebServer/TinfoilWebServer/' *
RUN chmod +x /app/TinfoilWebServer
WORKDIR "/app"
CMD ["/app/TinfoilWebServer"]
