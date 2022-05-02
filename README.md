# DocSearchAIO

## Installation
For some installation informations and how to setup the project, look at <a href="https://gitlab.com/Laszlo.Lueck/docsearchaio/-/blob/master/INSTALL.md">Install.md</a>

## What it is?
DocSearchAIO (All in one) is a project, written in C#.
It scans documents (currently word, excel, powerpoint, pdf) for text and put this text in an searchindex.

Within a configuration, you can specify a folder. In this folder and all subfolders, the configured documents where scanned and indexed.

There is a simple main searchform (like google) and some forms for configuration, statistcs and so on.

Currently the ui is written in plain html, javascript (with jQuery) and css, i switch the whole thing to angular.

## Running?
Let this thing run where you want.

Native everywhere where dotnet core could be run (windows, mac, linux, wsl, arm, x86, ...)

Or in a docker container. You don't want to build up the whole docker stuff?

Allright, go here and use it:

`docker push laszlo/docsearchaio:latest`


## Why CSharp and not super dooper trooper hipster language?
- I like it very much (coding since 20 years with it)
- I write C# in a very functional manner, sometimes it is harder to read but hey, thats a challenge!
- Extensibility - With method extensions i can structure the code much cleaner
- Very funny lambdas
- LinQ
- Async / Await and a huge possibility for parallelity
- Functional extensions from outside. As i wrote code in scala for some years there are some very useful coding patterns (pattern matchings, options instead of nullable handling, ....). All the things you can inherit as open source technics from outside. Examples?
    - https://github.com/louthy/language-ext
    - https://github.com/vkhorikov/CSharpFunctionalExtensions
    - https://github.com/nlkl/Optional


## Why?
Why not? I know, there are many products they do exactly the same in a much more professional manner but:
- I do this for learning by myself
- External tools are mostly expensive
- External tools could be slowdown while indexing oder searching
- External tools could be indexing the text into the cloud and the documents that i using are often closed for secure reasons.


## Performance
Indexing for word-documents is (with an 12 Core Intel I7 CPU) 115 documents per second
PDF like 20 - 30 documents per second

When you searching, the speed is (in dependency where the elastic-instance is using (native on os, or docker, or wsl(2))) often in any case lower than 100ms.

## Screenshots?
Slowly, but shurely! Yes, screenshots are comming!

