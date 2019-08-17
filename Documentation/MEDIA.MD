# Media Sources

**This is a draft document. Contents are subject to change without notice.**

**There is no license granted for this document. Do not redistribute.**

StreamNight supports any HLS server. 

Configuration and support for third-party software is out of scope for this project, but one example of a compatible server is here to help deployers get a working setup as quickly as possible.

This document assumes you have the ability to install StreamNight as documented in INSTALL.MD and BUILDING.MD.

Transcoding video needs a considerable amount of CPU power. The table below should only serve as a rough guideline of potential requirements.

| Different Quality Levels | CPU                                          | RAM    |
|--------------------------|----------------------------------------------|--------|
| 1 (passthrough)          | Any modern dual-core CPU                     | 1.5GB+ |
| 2 (re-encode)            | A modern quad-core CPU. 2.5GHz+ recommended* | 2.5GB+ |
| 4 (re-encode)            | A fast quad-core CPU. 3GHz+ recommended*     | 4GB+   |
| 8 (re-encode)            | A fast octa-core CPU. 3GHz+ recommended*     | 4GB+   |

*Clock speed is not an indicator of performance and only serves as a rough estimate. The CPU requirements will vary depending on the encoding settings. Core counts are physical cores (not hyper-threaded or with SMT).

## Installing a Streaming Server

There are many HLS servers available, such as Wowza, IIS Media Services, nginx's `nginx-rtmp-module`, Adobe Media Server, and even VLC Media Player.

We'll go with [SRS](https://github.com/ossrs/srs) because it's open-source and extremely powerful, but still simple enough to configure within a few minutes. 

**You'll need to be using Linux.** WSL inside Windows 10 may work. Recommended distributions are:

* Ubuntu
* CentOS

This guide was created using Ubuntu 18.04 on Windows 10 1903 (WSL).

### Why not use the binary releases?

Although you can get away with using the binary releases, they're compiled for extremely old operating systems that aren't supported.

If you're happy with that, or if you want to use [the Docker releases](https://github.com/ossrs/srs-docker/) then skip to the configuration section once you get it working. Make sure you use the FFmpeg builds.

### Build Prerequisites

We'll be compiling SRS ourselves to run on modern systems, so you'll need to install a few more packages beforehand.

* `python`
* `nasm`
* `libbz2-dev`

Additionally, install the dependencies listed in FFmpeg's [compilation guide for your distribution](https://trac.ffmpeg.org/wiki/CompilationGuide).

The configure script should automatically install the other prerequisites.

### Compiling SRS

Get the latest stable code from [the releases page](https://github.com/ossrs/srs/releases). As of the time of writing, it's [SRS v2.0.263](https://github.com/ossrs/srs/releases/tag/v2.0-r6).

Extract it to a suitable location.

#### Fixing the FFmpeg build script

Open `trunk/auto/build_ffmpeg.sh` and make the following changes:

Add `export PKG_CONFIG_PATH=${ff_release_dir}/lib/pkgconfig/` to under `echo "SRS_JOBS: ${SRS_JOBS}"` (around line 17). It should look like this:

```bash
echo "SRS_JOBS: ${SRS_JOBS}"
export PKG_CONFIG_PATH=${ff_release_dir}/lib/pkgconfig/

mkdir -p ${ff_build_dir}
```

Add `CXXFLAGS='-std=gnu++98'` to the `configure` command for `libfdk-aac`. It should look like this:

```bash
    cd fdk-aac-0.1.3 && bash autogen.sh && ./configure CXXFLAGS='-std=gnu++98' --prefix=${ff_release_dir} --enable-static && make ${SRS_JOBS} && make install
```

Add `--enable-pic` to the `configure` command for `x264 core.157`. It should look like this:

```bash
./configure --prefix=${ff_release_dir} --disable-opencl --bit-depth=all \
--enable-static --disable-avs  --disable-swscale  --disable-lavf \
--disable-ffms  --disable-gpac --disable-cli --enable-pic && 
```

Move `-lm -ldl` from the `configure` command for `ffmpeg-4.1` from `--extra-ldflags` to `--extra-libs`. It should look like this:

```bash
--extra-ldflags='-L${ffmpeg_exported_release_dir}/lib' \
[removed]
--enable-pthreads --extra-libs='-lpthread -lm -ldl' \
```

#### Running the build

Open a terminal in the extracted folder, navigate into the `trunk` directory and run these commands:

You should change the job count to one more than your CPU's core count.

```bash
./configure --with-ffmpeg --jobs=5 && make -j5
```

Go get a coffee and come back in a few minutes. Compiling the prerequisites takes a very long time.

## Configuring SRS

### Getting basic HLS working

```nginx
listen              1935; # Sets SRS to listen on port 1935
max_connections     1000; # Sets the maximum connections to 1000
daemon              off; # Starts SRS in interactive mode (console)
srs_log_tank        console; # Sends logging to the console

vhost __defaultVhost__ {
    min_latency     off; # Disables minimum latency mode. Set it to true to override some of the options below.

    gop_cache       on; # Enables fast playback when a client connects
                        # gop_cache enables I-frame caching so clients don't have to wait until the next I-frame before starting to decode data
    queue_length    10; # Sets the max cached GOP data to 10s
    mr {
        enabled     on; # Enables merged reads, increases performance at the cost of latency
        latency     350; # Sets latency to 350ms
    }
    mw_latency      350; # Merged writes are always on, sets latency to 350ms
                         # Minimum latency is at least merged read + merged write
    tcp_nodelay     off; # Disables TCP_NODELAY (default behaviour), which disables some optimisations to reduce small packets

    hls {
        enabled         on; # Enables HLS
        hls_fragment    5;  # Sets fragments to 5s each
        hls_window      20; # Retains 20s worth of fragments
        hls_path        [YOUR WEB SERVER's DOCUMENT PATH HERE]; # Sends HLS output to this path
                                                 # The app name is "hls", which is inside the default Vhost.
        hls_m3u8_file   [app]/[stream].m3u8;     # Sets the variant playlist path to hls/[stream key].m3u8
        hls_ts_file     [app]/[stream]-[seq].ts; # Sets the fragment path to hls/[stream key]-[sequence number].ts
    }
}
```

This configuration will output a single HLS stream to the specified HLS path.

### Adding transcoding

Modify the following configuration and add it to the existing one inside the desired Vhost.

**Do not copy the configuration snippet into your config file as-is.**

Vhosts can have multiple engines, in which case all of the enabled ones will be active simultaneously. Keep this in mind when estimating your CPU usage.

You may wish to modify the indentation to make it easier to read.

```nginx
transcode {
    enabled     on; # Enables transcoding
    ffmpeg      [PATH TO FFMPEG]; # FFmpeg path relative to the working directory
                                  # It's normally `./objs/ffmpeg/bin/ffmpeg`, but may be different if you used your system's FFmpeg install instead of compiling it manually.
    engine [ENGINE NAME] {
        iformat     flv; # Input format of the stream, use `flv` for RTMP (OBS's default output). You may specify `off` if you want FFmpeg to guess.
        vcodec      libx264; # Sets the video codec to libx264, this enables transcoding.
                             # You can also choose `copy` to remux it or `vn` for no video track.
        vbitrate    8000; # Sets the video bitrate in kbps, use the table in the OBS video section as a rough guideline.
        vfps        30; # Sets the video framerate in FPS.
        vwidth      1920; # Sets the video width in pixels.
        vheight     1080; # Sets the video height in pixels.
        vprofile    main; # Sets the video profile.
        vpreset     medium; # Sets the video preset. 
                            # Change this if your server's CPU is overloaded or if you want better quality. 
                            # See the FFmpeg documentation for more details.
        vparams {
            # Other FFmpeg/libx264 video parameters
            g       150; # Sets the video GOP to 5 seconds to stabilise fragment length
        }

        acodec      libfdk_aac; # Sets the audio encoder to `libfdk_aac`.
                                # You may need to use `aac` instead if you didn't build libfdk_aac with FFmpeg.
        abitrate    256; # Sets the audio bitrate in kbps.
        achannels   2; # Sets the number of audio channels to 2.
        aparams {
            # Other FFmpeg/libfdk_aac audio parameters
        }

        output      rtmp://127.0.0.1:[port]/[app]?vhost=[vhost]-HLS/[stream]_1080; # Sets the transcoder output location
                         # Note: Doesn't affect the HLS output location as long as [app] and [stream] are intact.
                         # Appends `_1080` to the end of the stream to signify it's a 1080p stream.
    }
}
```

### Making a master playlist

Unfortunately, SRS needs you to manually create the master playlist.

*Note: StreamNight may eventually support using a single variant playlist in place of a master playlist if you've only got one variant. You'll need to manually create one for now.*

A HLS master playlist is a short M3U8 file that points to the playlists SRS automatically creates (variant playlists).

A typical playlist might look like this:

```m3u8
#EXTM3U
#EXT-X-VERSION:3
#EXT-X-STREAM-INF:PROGRAM-ID=1,CLOSED-CAPTIONS=NONE,BANDWIDTH=16000000,RESOLUTION=1920x1080,FRAME-RATE=60.000
/hls/stream_1080-60fps.m3u8
#EXT-X-STREAM-INF:PROGRAM-ID=1,CLOSED-CAPTIONS=NONE,BANDWIDTH=8000000,RESOLUTION=1920x1080,FRAME-RATE=30.000
/hls/stream_1080.m3u8
#EXT-X-STREAM-INF:PROGRAM-ID=1,CLOSED-CAPTIONS=NONE,BANDWIDTH=6400000,RESOLUTION=1280x720,FRAME-RATE=30.000
/hls/stream_720.m3u8
#EXT-X-STREAM-INF:PROGRAM-ID=1,CLOSED-CAPTIONS=NONE,BANDWIDTH=2600000,RESOLUTION=960x540,FRAME-RATE=30.000
/hls/stream_540.m3u8
#EXT-X-STREAM-INF:PROGRAM-ID=1,CLOSED-CAPTIONS=NONE,BANDWIDTH=1600000,RESOLUTION=640x360,FRAME-RATE=30.000
/hls/stream_360.m3u8
```

Let's break it down into its component parts.

```
#EXTM3U
#EXT-X-VERSION:3
```

This is the HLS header. It simply tells reading applications that it's a M3U playlist (version 3).

```
#EXT-X-STREAM-INF:PROGRAM-ID=1,CLOSED-CAPTIONS=NONE,BANDWIDTH=16000000,RESOLUTION=1920x1080,FRAME-RATE=60.000
```

This is a pretty long string, so we'll break this one down again.

| Property             | Meaning                                                                                 |
|----------------------|-----------------------------------------------------------------------------------------|
| #EXT-X-STREAM-INF:   | This denotes the beginning of a stream information line                                 |
| PROGRAM-ID=1         | Master playlists can have multiple programs, so this tells the player which one's which |
| CLOSED-CAPTIONS=NONE | The stream doesn't have closed captions                                                 |
| BANDWIDTH=16000000   | The upper bound of bandwidth for the described stream, in bits per second               |
| RESOLUTION=1920x1080 | The resolution of the video, width x height                                             |
| FRAME-RATE=60.000    | The framerate of the video                                                              |

```
/hls/stream_1080-60fps.m3u8
```

This tells the player where to find the variant stream. Since it starts with a `/`, it's a "rooted" path which means the player looks for it from the end of the hostname instead of from inside the same folder.

The playlist then goes on to list 4 other streams, all of which follow the same pattern as the stream explained above.

Create your own playlist using the same format specified above. `FRAME-RATE` is optional but recommended if you have multiple transcoded framerates.

**The variant playlists use the stream key set in OBS instead of `stream`.**

## Starting SRS

Navigate to `trunk` and run 

```bash
./objs/srs -c [PATH TO THE CONFIG FILE]
```

## Configuring OBS

### Setting up OBS's encoders

*This guide uses some data from [this excellent OBS Project blog post](https://obsproject.com/blog/streaming-with-x264).

#### Video

1. Open the Settings menu from the buttons in the lower right corner of OBS's main screen
2. Select the `Output` tab
3. Change the `Output Mode` to `Advanced`
4. Select an encoder
    * If you have a strong CPU, use x264 as it offers the best quality.
    * If you have a weaker CPU and modern GPU, use a GPU-accelerated encoder in the following preferred order
        * `NVENC H.264` (preferred on Pascal (GTX 10xx) and above)
        * `QuickSync H.264` (if you have an older Nvidia card/only Intel integrated graphics)
        * `H.264/AVC Encoder (AMD Advanced Media Framework)` (for users with only AMD hardware)
        * **It's important to note that x264 is significantly better in terms of quality and should be used whenever possible.**
        * *This guide assumes you're using x264, although the general recommendations are applicable to all encoders.*
5. Pick a rate control method. `CBR` is recommended for general-purpose streaming, but you may choose others if you're sure you know what you're doing.
6. Pick a bitrate. This is dependent on your intended use-case and resolution but this is a reasonable place to start.

| Resolution | Bitrate            | FPS |
|------------|--------------------|-----|
| 853x480    | 800 - 1200 kbps    | 30  |
| 1024x576   | 1000 - 3000 kbps   | 30  |
| 1280x720   | 3000 - 5000 kbps   | 30  |
| 1920x1080  | 5000 - 8000 kbps   | 30  |
| 2560x1440  | 8000 - 12000 kbps  | 30  |
| 3840x2160  | 12000 - 20000 kbps | 30  |

*Note: The bitrate should be less than 80% of your total upload bandwidth.*

7. Set the `Keyframe Interval` to 3 seconds.
8. Set the `CPU Usage Preset` to `medium`. You may need to tweak this if it's too heavy for your CPU, but if you need a preset faster than `veryfast` then it's probably better to use a GPU-accelerated encoder.
9. Set the `Profile` to `main`. You may set it to `high` if you're using transcoding on the SRS side.
10. Set the `Tune` to the one that best matches your source media. If you're streaming something that doesn't fit into any of the categories, leave it at `(None)`.

#### Audio

Open the Settings window by following the instructions under the Video section.

##### Output Tab

**The options here are applied to both streams and recordings.**

You'll probably only want to use one audio track for the vast majority of the time.

A reasonable streaming bitrate is `192` to `256`. Setting it higher won't result in any audible quality increases for stereo audio.

##### Audio Tab

1. The `Sample Rate` should automatically your system audio, but just double-check it anyway to make sure.
2. Set the `Channels` to Stereo. Most people don't have more than two speakers anyway.
3. Set `Desktop Audio` to `Default`. This will capture everything that plays through your PC's default audio source, which is the preferred option unless you're using JACK or Virtual Audio Cables, which are both beyond the scope of this guide.

### Setting the resolution and framerate

Open the Settings window by following the instructions under the Video section.

1. Click on the `Video` tab.
2. Set the `Base (Canvas) Resolution` to the highest one listed. It should match your monitor.
3. Set the `Output (Scaled) Resolution` to the resolution you want to stream to SRS. 
    * SRS may do its own transcoding, so you'd generally want to set the output resolution equal to or higher than SRS's highest transcoding resolution.
4. Set the `Downscale Filter` to `Lanczos (Sharpened scaling, 32 samples)`. It's more CPU intensive than the other options but has much better clarity.
5. Set the FPS value to match your source media, or simply whatever you want.

### Setting the stream options

Open the Settings window by following the instructions under the Video section.

1. Click on the `Stream` tab.
2. Set the `Service` to `Custom...`
3. Set the `Server` to `rtmp://` with the public IP or hostname of your SRS server, plus the port number and `/ingress`.
    * If you're hosting SRS on the same machine you're using OBS on, use `127.0.0.1` or `localhost` with the port number and `/ingress` appended.
    * It should look like `rtmp://stream.example.com:1935/ingress`
4. Set the stream key to any string without spaces. You'll need this for the master playlist.

## Using OBS as a HLS server

**Reminder: These instructions do not have any warranty, and the authors are not responsible if anything happens as a result of following them. Do this at your own risk.**

**SRS is the recommended option.**

### Setting up OBS

#### Getting HLS output working

1. Click the `Settings` button in the bottom right corner of the main window and go to the `Output` tab
2. Inside the `Output` tab, change the `Output Mode` to `Advanced` and click the `Recording` tab
3. Change the `Type` to `Custom Output (FFmpeg)`
4. Change the `FFmpeg Output Type` to `Output to File`
5. Set the `File path or URL` to a folder called `hls`. The location of this folder doesn't matter.
6. Change the `Container Format` to `hls`
7. Add the following string to the `Muxer Settings`:

```
 hls_flags=delete_segments hls_time=5 hls_list_size=5 hls_allow_cache=0
```

8. Change the `Video Bitrate` to your desired bitrate
9. Change the `Keyframe interval` to 5x your framerate
10. Add the following string to the `Video Encoder Settings`:

```
profile=high level=4.0 preset=veryfast
```

You may change the preset if your CPU is struggling or if you want better quality at the same bitrate.

#### Changing the name of the playlists and fragments

**Warning: Revert these changes if you want to start recording videos normally.**

1. Click the `Settings` button in the bottom right corner of the main window and go to the `Advanced` tab
2. Under `Recording`, change the `Filename Formatting` to a static string. The default is `%CCYY-%MM-%DD %hh-%mm-%ss`
3. Enable `Overwrite if file exists`. This lets you restart your stream without having to manually delete the playlist and fragment files.

## Setting up the web server

**This section assumes you're using the Caddy configuration specified inside `INSTALL.MD`.**

Check your web server's documentation if you're using something else.

1. Add `except /hls` to inside the `proxy` directive. It should then look like this:

```
    proxy / stream.example.com:5050 {
        [omitted]
        except /hls
    }
```

2. Change the `gzip` directive to a block and add `not /hls`

3. Add the following string to your site directive:

```
root [fully rooted path to the folder containing the HLS output folder]
```

On Windows, a fully rooted path would look like this: 

| OBS Output Folder       | Fully Rooted Path   |
|-------------------------|---------------------|
| `C:\OBS\Streaming\hls\` | `C:\OBS\Streaming\` |

On Linux, a fully rooted path would look like this:

| OBS Output Folder | Fully Rooted Path |
|-------------------|-------------------|
| `/tmp/obs/hls/`   | `/tmp/obs/`       |

Your Caddyfile should look like this:

```
stream.example.com {
    tls PUT YOUR EMAIL HERE!
    gzip {
        not /hls
    }
    proxy / stream.example.com:5050 {
        transparent
        websocket
        header_downstream Access-Control-Allow-Origin "*"
        except /hls
    }
    root [fully rooted HLS output path]
}
```
