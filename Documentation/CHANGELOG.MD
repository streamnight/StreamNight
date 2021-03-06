# StreamNight Changelog

This document describes the notable changes made to StreamNight. 

Releases are currently referred to by date.

**This document follows the ISO 8601 date format.**

# 2020-02-17

### Bug Fixes

* Fix race condition in UserHandler

### Improvements

* Updated to .NET 5.0
* Updated dependencies

*Forked from development commit ad33d35522bbf03a17224e31a4e17126169ec87a*

## 2020-11-17

### Bug Fixes

* Fixed compatibility with DSharpPlus alpha packages
* Fixed custom property names on the status page not using the correct names
* Fixed Discord logins through reverse proxies
* Fixed Twitch chat integration
* Fixed Discord API endpoint URLs

### Improvements

* Migrated to ASP.NET Core Authorization for SignalR endpoints
* Minor code style changes

### New Features

* Added the ability to move the video player to the center of the free space
* Added SignalR connection logs - change the UserLogger class if you'd like to remove this

*Forked from development commit e104333a287789dcd51971a7ca920980f3362f6d*

## 2020-03-26

### Bug Fixes

* Fixed translation of multiple emotes in a single message
* Fixed overlay of profile pictures appearing outside the picture's boundaries
* Fixed OAuth logins on Chrome

**Note: If your logins are failing with a cookie correlation error, ensure your reverse proxy is using a HTTPS connection to Kestrel**

### Improvements

* Upgraded to .NET Core 3.1
* Updated video.js to enable picture-in-picture support
* Increased contrast of volume slider
* Minor code cleanups

### New Features

* Added an error page so it's clearer when StreamNight breaks

*Forked from development commit 910a1caa46267b13558e6eb1431be66e01e5b6c2*

## 2019-11-18

### Bug Fixes

* Fixed online stream status image exceeding player boundaries
* Fixed automatic quality text updating when manually changing quality
* Fixed labels appearing on top of menu on mobile devices
* Fixed the value of a manually selected quality incorrectly changing upon loading
* Fixed viewport exceeding screen boundaries on some mobile browsers
* Fixed mobile header icon not downloading correctly

### Improvements

* Changed bot control buttons to red to warn administrators of a potentially dangerous action
* Changed Discord client's public fields to properties
* Status checks now use reflection instead of hardcoding properties
* Updated chat styling to match latest Discord desktop release
* Increased size and margins of chat toggle
* Added new styling for mobile devices with a soft keyboard open
* Upgraded to .NET Core 3.0
* Fixed some inconsistent paddings and border radiuses
* Fixed new lines in chat entry

### New Features

* Added optional Twitch integration
* Added Discord presence configuration
* Added playlist redirection
* Added debug panel
* Website now serves mobile header logo as favicon
* Added playlist redirection
* Added debug panel

### Miscellaneous

* Removed unused configuration property
* Added static analysis to build process
* Updated internal documentation
* Removed dependency on Discord.OAuth2

*Forked from development commit 91702d5da615f7e9fac06bd6ec3f67f2ff9e3160 on 2019-11-18*

## 2019-08-18

This release changes the license used for the documentation to allow for per-file licenses. It's currently only used for the media server draft.

### Miscellaneous

* Added media server documentation
* Updated documentation license

## 2019-08-11

This release adds mandatory configuration options. See `INSTALL.MD` for more information.

### Bug Fixes

* The options panel now works in Internet Explorer
* Viewer icons are displayed correctly in Internet Explorer
* Heartbeats for "ghost" clients are now handled correctly
* Video should no longer overflow the vertical height on Chrome when only chat is hidden
* Discord channel names should automatically update instead of remaining fixed from when the bot was started
* Emotes in the picker are now centered instead of tiling
* Fixed chat overflowing on mobile devices
* Fixed registration for users not in the guild
* Fixed emoji picker clipping
* Fixed Discord logins not working after registering with a bot link
* Fixed styling of dismissible alerts

### Improvements

* Enabled animated emotes from the web chat side
* Moved to ASP.NET layouts for pages instead of per-page HTML
* Cleaned up CSS and JavaScript
* Replaced placeholder account management UI with a new one based on StreamNight's existing theme
* Darkened the login and profile backgrounds on mobile devices to match the desktop experience
* Automatically update auto-playlist data instead of waiting for the user to become active
* Embeds are now sent across the bridge instead of showing up as an empty message
* Enabled Markdown for struck-through text, underlined text, and some links
* Added styling to Discord login button
* Errors are now displayed when logging in with OAuth

### New Features

* Added status page which also allows stream controllers to restart the Discord bot
* Added Discord bot configuration page
* Added framerate information to quality picker. Requires `FRAME-RATE` data in the master playlist
* Added message timestamps

### Miscellaneous

* Fixed another filename which slipped through when the project was renamed
* Added a separate license for documentation files
* Updated documentation and sample configuration files 
* Added changelog file
* Removed unnecessary text on login page

### Notes

* The new UI requires JavaScript for menu item highlighting. There is currently no fallback for users blocking content scripts

*Forked from development commit c6ad7aef11cd941d41402c0738a7bffbd53232f2 on 2019-08-09*

## 2019-07-23

First public release.