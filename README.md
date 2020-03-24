# StreamNight

[Discord Guild](https://discord.gg/c35kB4h)

*Note: This software is licensed for non-commercial use. Join the Discord server if you'd like a custom license.*

## Introduction

StreamNight is a HLS streaming frontend with Discord integration.

Features include:

* Two-way Discord integration, with support for both Unicode emoji and server emotes
* Fully responsive UI that naturally scales from phones to PCs
* Theoretically unlimited users
* A reliable video player (video.js) with a manual quality switcher
* Works on all up-to-date modern browsers. Partially works on Internet Explorer 11

## System Requirements

StreamNight will run on anything that supports [.NET Core 3.1](https://github.com/dotnet/core/blob/master/release-notes/3.1/3.1-supported-os.md), but it's best to host it on a machine with a fast, reliable internet connection.

It'll generally use 400-500MB of RAM on a machine with memory to spare.

## Installation

See the wiki or INSTALL.MD for installation instructions.

## Release Information

This repository is the public version of the stream software, and most of the dev work happens in a private repo.

I'm not planning to explicitly mark anything as stable until I write proper tests, but it should be fine for day-to-day use.

## Other Credit

[DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) is an excellent Discord wrapper for .NET projects

[ASP.NET Core SignalR](https://github.com/aspnet/AspNetCore/tree/master/src/SignalR) is great for real-time web apps

[Video.js](https://github.com/videojs/video.js) is super simple to use and extend

[Pure.CSS](https://github.com/pure-css/pure/) is a fantastic lightweight set of CSS modules that works almost everywhere
