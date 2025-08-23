# Using JL with mpv to watch anime with Japanese subtitles

## Setup instructions

1. Install [mpv](https://mpv.io/).
2. Install [mpv_websocket](https://github.com/kuroahna/mpv_websocket) to automatically copy subtitles to a WebSocket. Unlike mpvacious, it [correctly sends an empty string when no subtitle is shown](https://github.com/kuroahna/mpv_websocket/issues/21), allowing JL to detect when subtitles disappear.
3. Add `input-ipc-server=/tmp/mpv-socket` to your `mpv.conf` file.
4. Enable the `Automatically pause the video playing in mpv on hover and resume on mouse leave` option via the `Preferences -> Main Window` menu in JL.
5. Hide the mpv subtitles by pressing `v`. To make them hidden by default, add `sub-visibility=no` to your `mpv.conf` file.

---

## Recommended JL settings for watching anime

- Consider creating a new profile for anime.
- Enable `Dynamic height`.
- Enable `Dynamic width` and `Reposition window to the fixed right-edge position on text change`.  
  Set `Fixed right-edge position` to `0`.
- Enable `Center text horizontally`.
- Enable `Hide all buttons when mouse is not over the title bar`.
- Disable `Text only visible on hover`.
- Try experimenting with the `Shadow depth`, `Shadow blur radius`, `Shadow opacity`, and `Shadow direction` settings to improve the readability of the text when the main text box background is transparent.  
  - A good starting point might be: `Shadow depth: 2.0`, `Blur radius: 7`, `Opacity: 100`, `Direction: 340`.
- Try changing the `Font weight` option and see if it improves the text readability of the main text box text for you.

---

## Recommended mpv configs

- To make sub-texts hidden by default, add `sub-visibility=no` to your `mpv.conf` file.
- To make OSC show up only when you hover over it, add `deadzonesize=1.0` to your `script-opts/osc.conf` file.
---

Here’s what the end result looks like:

https://github.com/user-attachments/assets/bf17d0d3-e89d-4d67-80d9-ea5bc5c84d58
