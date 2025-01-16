FROM ubuntu:20.04

LABEL maintainer "JesusDF"

RUN mkdir -p /app \
        && apt update \
        && apt-get install -y software-properties-common -y \
        && apt update \
        && apt install openjdk-16-jdk ttf-mscorefonts-installer -y \
        && rm -rf /var/lib/apt/lists/*
WORKDIR /app

# JAVA application
COPY app/CrystalCmd.jar /app/
# Additional fonts
COPY fonts/* /usr/share/fonts/truetype/msttcorefonts/

EXPOSE 4321
CMD ["java", "-jar", "/app/CrystalCmd.jar"]

