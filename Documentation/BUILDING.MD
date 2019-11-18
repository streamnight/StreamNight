# Compiling StreamNight

## Getting Ready

### Build Prerequisites

* [.NET Core 3.0 SDK with ASP.NET Core](https://dotnet.microsoft.com/download/dotnet-core/3.0)
* [DSharpPlus Nightly Builds](https://nuget.emzi0767.com/gallery/package/DSharpPlus/4.0.0-nightly-00619)
* Microsoft LibMan ([.NET Core CLI](https://docs.microsoft.com/en-us/aspnet/core/client-side/libman/libman-cli?view=aspnetcore-3.0)/[Visual Studio](https://docs.microsoft.com/en-us/aspnet/core/client-side/libman/libman-vs?view=aspnetcore-3.0))

DSharpPlus requires some extra work because we're using nightly builds from their SlimGet instance.

Other dependencies should be automatically installed through NuGet.

#### Installing DSharpPlus

You'll need to add the NuGet feed to [your NuGet configuration](https://docs.microsoft.com/en-us/nuget/consume-packages/configuring-nuget-behavior). As of 2019-10-04, that's adding this line to your NuGet.Config inside `packageSources`:

    <add key="DSharpPlus" value="https://nuget.emzi0767.com/api/v3/index.json" />

If you're using Visual Studio, right-click the project, press `Manage NuGet Packages...`, press the cog next to `Package Source`, and create a new NuGet source.

You should now be able to [restore](https://docs.microsoft.com/en-us/nuget/consume-packages/package-restore) StreamNight's NuGet dependencies without running into issues. The .NET Core SDK should automatically run this, but it's best to make sure everything's working as intended before moving on.

#### Installing SignalR Client

StreamNight's repository should already have the SignalR client code present in the `wwwroot/lib` folder, but you can update the version used by running LibMan's restore command. 

See the link in the prerequisites section for more information.

## Compiling StreamNight

StreamNight uses [publish profiles](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/visual-studio-publish-profiles?view=aspnetcore-3.0) to simplify the build process down to one command or three clicks in Visual Studio.

This table lists the current settings in the shipping publish profiles:

| Name           | Build Configuration | Architecture                | Target Framework | Requires Runtime | Binary Location                  |
| -------------- | ------------------- | --------------------------- | ---------------- | ---------------- | -------------------------------- |
| `nc-rel-30-fd` | Release             | Any CPU (32-bit and 64-bit) | .NET Core 3.0    | Yes              | `bin\pub\Release\netcoreapp3.0\` |

It's possible to have a [self-contained deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/index) that doesn't require the .NET Core runtime, but these instructions assume you're creating a framework-dependent deployment.

#### Visual Studio (Windows-only)

1. Right-click the project in Solution Explorer
2. Click `Publish...`. It should have a blue globe icon in Visual Studio 2017/2019.
3. Select the publish profile. The default should be `nc-rel-30-fd`, check the table above for more information.
4. Press `Publish`.

#### .NET Core CLI (Cross-platform)

**Unfortunately, publish profiles don't seem to be working so you'll need to manually specify the options on the command-line.**

1. Open a terminal in the project folder.
2. Run `dotnet publish -c Release -f netcoreapp3.0 -o bin\pub\Release\netcoreapp3.0\`

After compiling StreamNight, follow the instructions for pre-built binaries on the installation page.