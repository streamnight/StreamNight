# Compiling StreamNight

## Getting Ready

### Build Prerequisites

* [.NET 5.0 SDK with ASP.NET Core](https://dotnet.microsoft.com/download/dotnet/5.0)
* [DSharpPlus 4.0](https://www.nuget.org/packages/DSharpPlus/4.0.0-rc1)
* Microsoft LibMan ([.NET Core CLI](https://docs.microsoft.com/en-us/aspnet/core/client-side/libman/libman-cli?view=aspnetcore-5.0)/[Visual Studio](https://docs.microsoft.com/en-us/aspnet/core/client-side/libman/libman-vs?view=aspnetcore-5.0))

Other dependencies should be automatically installed through NuGet.

#### Installing SignalR Client

StreamNight's repository should already have the SignalR client code present in the `wwwroot/lib` folder, but you can update the version used by running LibMan's restore command. 

See the link in the prerequisites section for more information.

## Compiling StreamNight

StreamNight uses [publish profiles](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/visual-studio-publish-profiles?view=aspnetcore-5.0) to simplify the build process down to one command or three clicks in Visual Studio.

This table lists the current settings in the shipping publish profiles:

| Name           | Build Configuration | Architecture                | Target Framework | Requires Runtime | Binary Location                  |
| -------------- | ------------------- | --------------------------- | ---------------- | ---------------- | -------------------------------- |
| `net50-rel-fd` | Release             | Any CPU (32-bit and 64-bit) | .NET 5.0         | Yes              | `bin\pub\Release\net5.0\` |

It's possible to have a [self-contained deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/index) that doesn't require the .NET Core runtime, but these instructions assume you're creating a framework-dependent deployment.

#### Visual Studio (Windows-only)

1. Right-click the project in Solution Explorer
2. Click `Publish...`. It should have a blue globe icon in Visual Studio 2017/2019.
3. Select the publish profile. The default should be `nc-rel-31-fd`, check the table above for more information.
4. Press `Publish`.

#### .NET Core CLI (Cross-platform)

**Unfortunately, publish profiles don't seem to be working so you'll need to manually specify the options on the command-line.**

1. Open a terminal in the project folder.
2. Run `dotnet publish -c Release -f net5.0 -o bin\pub\Release\net5.0\`

After compiling StreamNight, follow the instructions for pre-built binaries on the installation page.