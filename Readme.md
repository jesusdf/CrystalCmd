
Forked and rebased from https://github.com/majorsilence/CrystalCmd to meet custom needs.

If you are looking for a production crystal reports server look into [SAP Crystal Server](https://www.sap.com/canada/products/technology-platform/crystal-server.html).

# What is CrystalCmd

**CrystalCMD** is a C#/dotnet and Java program that loads JSON files into Crystal Reports to produce PDFs. Initially an experimental proof of concept, it demonstrates generating Crystal Reports on Linux using Java and .NET framework (wine).

The main focus is the c#/dotnet implementation running within Windows/IIS and Linux/Wine.

**Key Features:**

- PDF Generation: Converts JSON (with embedded csv) data into PDF reports with Crystal Reports templates.
- Command Line & Server Modes: Supports both modes; server mode is recommended for better performance.
- Cross-Platform: Works on Linux and can run .NET implementations using Wine.

# Use cases
- Provide a path for porting a asp.net framework site from windows and iis to asp.net dotnet core net6.0 or newer on linux
- Provide support for developers to work on projects that use dotnet crystal reports while using a mac
- Provide a way to fence off legacy crystal reports behind a web api

# Development
CrystalCMD is developed with the following work flow:

* Nothing happens for months/years
* Someone needs it to do something it doesn't already do
* That person implements that something and submits a pull request
* Repeat if it doesn't have a feature that you want it to have, add it
    * If it has a bug you need fixed, fix it


# Example usage

Note that, when using the command line option, this is very slow and highly recommended to use the server option.

test server: c.majorsilence.com

- https://c.majorsilence.com/status
- https://c.majorsilence.com/export

Example running from base CrystalCmd folder.

```bash
curl https://c.majorsilence.com/status

curl -u "username:password" -F "reportdata=@test.json" -F "reporttemplate=@the_dataset_report.rpt" https://c.majorsilence.com/export --output testout.pdf

# test localhost
curl -u "username:password" -F "reportdata=@test.json" -F "reporttemplate=@the_dataset_report.rpt" http://127.0.0.1:4321/export --output testout.pdf
```

## Server mode


1. http://localhost:4321/status
1. http://localhost:4321/export
   - Returns pdf as bytestream
   - Must be passed two post variables as byte arrays
     - reporttemplate
     - reportdata

# Postman Collection

[Majorsilence.CrystalCMD.postman_collection.json](Majorsilence.CrystalCMD.postman_collection.json)

# Dotnet

Use this project to generate test data from c# program

See [Crystal Reports, Developer for Visual Studio Downloads](https://help.sap.com/docs/SUPPORT_CONTENT/crystalreports/3354091173.html).

- Download the Crystal Reports .net runtime from: [https://origin.softwaredownloads.sap.com/public/site/index.html](https://origin.softwaredownloads.sap.com/public/site/index.html)
  - CR for Visual Studio SP35 CR Runtime 64-bit
  - CR for Visual Studio SP35 CR Runtime 32-bit

- Majorsilence.CrystalCmd.NetframeworkConsoleServer
    - a net48 embedio based console app/webserver
    - can be run on Linux using wine


See the [dotnet/Readme.md](https://github.com/majorsilence/CrystalCmd/tree/main/dotnet) file for more info..

```mermaid
flowchart TD
    subgraph "Client Applications"
        A[".NET Application"] --> C["Majorsilence.CrystalCMD.Client"]
        B["Mac/Linux Application"] --> C
    end

    subgraph "Server Options"
        D["Windows Service Majorsilence.CrystalCmd.NetframeworkConsoleServer"]
        E["Linux Docker Container with Wine + .NET 4.8 + Crystal Reports 13.0.35"]
    end

    C --"1 - Send Crystal Report Template, 2. Send Data 3. Request PDF"--> F{CrystalCmd Service}
    F --> D
    F --> E
    
    subgraph "Processing"
        D --> G["Crystal Reports Engine"]
        E --> G
        G --> H["Generate PDF"]
    end
    
    H --"Return PDF"--> C
    C --> I["Client receives PDF"]
    
    classDef client fill:#d1f0ff,stroke:#333,stroke-width:1px
    classDef server fill:#ffe6cc,stroke:#333,stroke-width:1px
    classDef process fill:#e6ffcc,stroke:#333,stroke-width:1px
    
    class A,B,C client
    class D,E,F server
    class G,H,I process
```


# Crystal report examples

https://wiki.scn.sap.com/wiki/display/BOBJ/Crystal+Reports+Java++SDK+Samples#CrystalReportsJavaSDKSamples-Database


# Java

Basic info on the java version.   

## command line mode

CrystalCmd supports running as a command line tool. Pass in path to report, data, and output fileand a pdf is generated.

```bash
java -jar CrystalCmd.jar -reportpath "/path/to/report.rpt" -datafile "/path/to/data.json" -outpath "/path/to/generated/file.pdf"
```

example 2

```bash
java -jar CrystalCmd.jar -reportpath "/home/peter/Projects/CrystalCmd/the_dataset_report.rpt" -datafile "/home/peter/Projects/CrystalCmd/test.json" -outpath "/home/peter/Projects/CrystalCmd/java/CrystalCmd/build/output.pdf"
```



## Run the server

CrystalCmd supports running in server mode. If you run it with no command line arguments it
starts a web server listening on port 4321. There are two end points that can be called.


```bash
java -jar CrystalCmd.jar
```

Call the server.

```bash
curl -u "username:password" -F "reporttemplate=@the_dataset_report.rpt" -F "reportdata=@test.json" http://localhost:4321/export > myoutputfile.pdf
```

### Example using docker

```bash
docker run -p 2005:4321 -t majorsilence/crystalcmd
```

Or run it as a daemon.

```bash
docker run -p 2005:4321 -d majorsilence/crystalcmd
```

Now check the status in your browser:

- http://localhost:2005/status

### Example of using the installed snap

install

```bash
snap install ./java/CrystalCmd/build/CrystalCmd.snap --force-dangerous --classic
```

run

```bash
crystalcmd -reportpath "/home/peter/Projects/CrystalWrapper/the_dataset_report.rpt" -datafile "/home/peter/Projects/CrystalWrapper/test.json" -outpath "/home/peter/Projects/CrystalWrapper/Java/build/output.pdf"
```

# Building Snaps

```bash
sudo ./build_snap.sh
```

## dev setup

```bash
sudo apt-get install openjdk-11-jdk
```

### Eclipse

Download [eclipse java edition](http://www.eclipse.org/downloads/eclipse-packages/).

Setup eclipse with [crystal references](https://archive.sap.com/documents/docs/DOC-29757).

Import java/CrystalCmd folder as ecplise project (Eclipse -> File -> Open Projects from File System).

### IntelliJ

Download [intelliJ community edition](https://www.jetbrains.com/idea/).

## Runtime setup

```bash
sudo apt-get install openjdk-11-jre
```

## Export Jar

- Eclipse -> File -> Export -> Java -> Runnable Jar File

Package required libraries into generated JAR

output as "CrystalCmd.jar" in folder ./CrystalCmd/java/CrystalCmd/build

# Crystal reports Eclipse JAR library downloads

https://origin.softwaredownloads.sap.com/public/site/index.html

# OpenJDK PDF Export Problem, fonts

https://answers.sap.com/questions/676449/nullpointerexception-in-opentypefontmanager.html?childToView=708783&answerPublished=true#answer-708783

> After some experimentation, a workaround was to create the fonts folder in the AdoptOpenJDK JRE (jre\lib\fonts) and copy a single font file from the **Linux msttcorefonts** mentioned above into the newly created fonts folder. My document uses all Arial font, but it doesn't seem to matter what font file is in the fonts folder. I copied Webdings.ttf. The file does have to be a real font file. I tried making a dummy text file and rename it to Webdings.ttf, but the NPE occurred with the dummy font file.
>
> Once a real font is copied to jre\lib\fonts, The PDF is created just fine with the Arial font embedded. It seems that there just has to be a one real font at jre\lib\fonts to get started, and then crjava/AdoptOpenJDK will eventually use fontconfig to find the correct Windows font.

Copy a file from

## Windows Example:

Copy a file to **C:\Users\[UserName]\.jdks\openjdk-16.0.1\lib\fonts** from **C:\Windows\Fonts**.

## Mac Example

copy '/System/Library/Fonts' into '/Users/[UserName]]/Library/Java/JavaVirtualMachines/[JavaVersion]/Contents/Home/lib/fonts'

## Linux Example:

```bash
# try the ubuntu or fedora way first
# https://answers.sap.com/questions/676449/nullpointerexception-in-opentypefontmanager.html
apk add --no-cache msttcorefonts-installer && update-ms-fonts && fc-cache -f && ln -s /usr/share/fonts/truetype/msttcorefonts /usr/lib/jvm/default-jvm/jre/lib/fonts
```

# ubuntu

```bash
apt install fonts-dejavu fontconfig msttcorefonts-installer && update-ms-fonts && fc-cache -f
ln -s /usr/share/fonts/truetype/msttcorefonts /usr/lib/jvm/java-1.11.0-openjdk-amd64/lib/fonts
```

# fedora

dnf install fontconfig dejavu-sans-fonts dejavu-serif-fonts


# Alternatives

- [CrystalReportsRunner](https://github.com/gerardo-lijs/CrystalReportsRunner)
- [SAP Crystal Server](https://www.sap.com/canada/products/technology-platform/crystal-server.html)
- [RptToXml](https://github.com/ajryan/RptToXml)
