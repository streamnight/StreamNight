# StreamNight Changelog

This document describes the notable changes made to StreamNight. 

Releases are currently referred to by date as the project is under heavy development and user-facing components are subject to change at any time.

**This document follows the ISO 8601 date format.**

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