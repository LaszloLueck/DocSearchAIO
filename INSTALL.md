# Installation 

There are multiple options where you can install the application.

## Docker for Windows

**Attention: Currently, Docker for windows can only access native drives (ssd or hdd on a NVMe / Sata-Port). 
It cannot be used with usb-drives because of missing usb-passthrough. If you need to run the App with an usb-drive (encrypted or not), look
into the workaround section.**

The easiest way and if you have also a running docker-environment and docker-compose on your machine, 
is to get the package compose.zip here from this project.  
https://gitlab.com/Laszlo.Lueck/docsearchaio/-/blob/master/DocSearchAIO/Docker/compose.zip

Extract the archive in a folder of your choice.

Now change in to the folder where you have extracted the archive and edit some files.

1. docker-compose.yaml
Because the easiest way to run this app is docker, let's start with docker-compose.
The docker-compose.yaml file there is already up to start all we need for the app.
Except the entry

`  - e:/:/app/data`

in the volumes-section.
Depending on your directory which you will indexing, you must set the correct path to the directory.
For test purposes, we use a directory with 4 files.
````
   Documents
   |__ word
   |   |__ wordfile.docx
   |
   |__ excel
   |   |__ excelfile.xlsx
   |
   |__ powerpoint
   |   |__ powerpointfile.pptx
   |
   |__ pdf
       |__ pdffile.pdf
````

The folder is located under (as windows path) `c:\temp\`.

In summary, the correct path for docsearchaio to scan is `c:\temp\Documents`
Because docker is more a linux-tool we must change the folder to more linux-style: `c:/temp/Documents`

So the complete block should look:
````yaml
.
.
.
    volumes:
      - c:/Temp/Documents:/app/data
      - .comparer:/app/Resources/comparer
      - .config:/app/Resources/config
      - .statistics:/app/Resources/statistic
    restart: always
.
.
.
````
all necessary folders are mounted outside of the containers, so a `docker-compose down` would not remove the files, because they are stored outside of the containers.

Save the docker-compose.yaml and stay in the folder where docker-compose.yaml file is located.

All the other steps, indexing, reindexing, searching, you could read in the README.md

2. Start the apps with the command `docker-compose up` or `docker-compose up -d` as the `-d` runs the docker container in background. For testing reasons, i suggest to run first time without `-d`.

You can always show all logfiles from the container of the docker-compose you can type

`docker-compose logs -f`

This works like the linux-command `tail -f`, but only for the running docker-container for the compose file.

When starting the application with docker-compose for the first time, this startup can take a longer time because the required containers have to be downloaded first.

After that, the setup of the containers for use is finished.

---

## Further adjustments for launching under Docker within WSL / WSL2.
The use of the application within WSL / WSL2 has the decisive advantage that drives that are not directly connected (USB drives, Thunderbolt drives) can also be used for indexing with DocSearch.

However, this solution also has a decisive disadvantage.
The write and read performance is very poor under Windows with WSL.
This slows down the indexing and search process decisively.
Of course, this depends on the amount of documents to be indexed.

### Mount a not standard drive (usb, thunderbolt)
Execute if the directory to be searched is not the standard C drive (e.g. ext. hard disk).
 ````shell
  sudo mkdir /mnt/f --> where f can be any letter
  sudo mount -t drvfs e: /mnt/f --> e is the Windows drive letter and /mnt/x is the mount created above
  ````

Now you should see the contents of the drive with an `ls -lah /mnt/f`.
The best way to do this is to change to the directory on the drive you want to index and then simply run a
`pwd`. Then copy the line and edit the docker-compose.yaml in the zip file.

Here you change to the section `volumes` of the container config `docsearchaio`.

And enter the copied directory in the place `/mnt/f/scandir`:

````yaml
 volumes:
   - /mnt/f/scandir:/app/data
   - .comparer:/app/Resources/comparer
   - .config:/app/Resources/config
   - .statistics:/app/Resources/statistic
restart: always

````

2. Permission-settings for the elasticsearch data directory.

By default, the indexed data is stored outside the Docker container so that it is preserved after a restart of the Docker container.

In order for elasticsearch to write the data outside the Docker container, the ownership rights for the data directory must be set.

Before the first start of the container, a new directory `.elasticData` can be created in the target directory.

After that, the owner of the directory must be set again for user and group. This is done with the following commands:

`````shell
mkdir .elasticData
sudo chown -R 1000:1000 .elasticData
`````

## Running nativ under Windows / Linux / MacOSX.
The application also works natively on all major operating systems (Windows, Linux, MacOS) or wherever Dotnet Core 6 can be installed.

The best way to do this is to download the application from the Gitlab repository and compile it accordingly in the operating system.

In the config.json (in the directory `/Resources/config/config.json`) you have to set where the Elasticsearch instance can be reached.

Run
````shell
dotnet build
dotnet run
````
to start the application.

## Other useful settings in docker-compose.yaml

## Elasticsearch
If you have enough Memory (RAM) in your computer, you can extend the amount of usable Heap for elasticsearch.  
The line

`- "ES_JAVA_OPTS=-Xms512m -Xmx4096m"`

describes how many Memory elasticsearch can use. The value -Xmx4096m describes, that elastic can use 4GByte RAM. If you have enough RAM, you can
increase this value e.g. 8192m or 16384m or in other typing 4g, 8g or 16g

A big improvement is, when you use elasticsearch as an external instance even much more, when there is an elastic-cluster with more machines.
