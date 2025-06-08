# Using JL with mpv to watch anime with Japanese subtitles

## Setup instructions

1. Install [mpv](https://mpv.io/).
2. Install a plugin that automatically copies subtitles:
   - To a WebSocket (e.g., [mpv_websocket](https://github.com/kuroahna/mpv_websocket)), or  
   - To the clipboard (e.g., [mpvacious](https://github.com/Ajatt-Tools/mpvacious)).
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

---

Here’s what the end result looks like:

https://github.com/user-attachments/assets/bf17d0d3-e89d-4d67-80d9-ea5bc5c84d58
