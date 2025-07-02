FROM ubuntu:24.04

LABEL maintainer="JesusDF"

RUN mkdir -p /app \
 && mkdir -p /app/fonts

# Additional fonts
COPY fonts/* /app/fonts/

RUN adduser -D crystal \
        && apt update \
        && apt install -y software-properties-common && apt update \
        && apt install -y openjdk-17-jdk fonts-dejavu ttf-mscorefonts-installer \
        && cp /app/fonts/* /usr/share/fonts/truetype/msttcorefonts/ \
        && cp /app/fonts/* /usr/share/fonts/ \
        && fc-cache -rf \
        && rm -rf /var/lib/apt/lists/* \
        && ln -s /usr/share/fonts/truetype/msttcorefonts /usr/lib/jvm/default-jvm/jre/lib/fonts

WORKDIR /app

# JAVA application
ENV JAVA_HOME=/usr/lib/jvm/default-jvm
COPY app/CrystalCmd.jar /app/

EXPOSE 4321
USER crystal
CMD ["java", "-jar", "/app/CrystalCmd.jar"]

