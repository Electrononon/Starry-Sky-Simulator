# Starry Sky Simulator

A real-time 3D night sky built in Unity. Fly anywhere within 1,000 light-years of the Sun and watch the stars and constellations shift and warp as your viewpoint changes.

**Live demo:** [electrononon.github.io/Starry-Sky-Simulator](https://electrononon.github.io/Starry-Sky-Simulator/)

![Starry Sky Simulator](Images/Starry%20Sky%20Simulator%20Image.png)

## Features

- 110,000+ stars from the Hipparcos catalogue, placed at their real 3D positions; stars too faint to see from your current position are culled each frame
- Star brightness and size computed from each star's distance to you, so the sky changes as you move
- Toggleable star labels, constellation lines and constellation labels
- Milky Way backdrop from NASA's Deep Star Maps 2020 (Gaia DR2)
- Free 6-DOF movement with adjustable speed
- Runs in the browser (WebGL) and on desktop

## Controls

- `W` – Move Forward  
- `S` – Move Backward  
- `A` – Move Left  
- `D` – Move Right  
- `SPACE` – Move Up  
- `SHIFT` – Move Down  
- `Q` – Decrease Movement Speed  
- `E` – Increase Movement Speed  
- `ESC` – Toggle Sidebar Menu  

## How it works

- **Star positions:** Each star's Hipparcos position is stored in 3D space. Every frame, the direction from the player to each star is projected onto a sky sphere around the camera, so constellations distort naturally as you fly.
- **Brightness:** Apparent magnitude is recalculated from each star's absolute magnitude and current distance (the distance modulus), and drives the star's rendered size. Stars too faint to see are skipped.
- **Rendering:** All 110,000+ stars are drawn with GPU instancing (`Graphics.DrawMeshInstanced`, in batches of 1,023) and frustum culling, which keeps the frame rate smooth, including in WebGL.
- **Labels:** Star and constellation labels are pooled, fade in and out, and appear only for stars brighter than a magnitude threshold. When labels overlap on screen, the brighter star's label is kept and the other is hidden, and constellation labels take priority over star labels.
- **Constellations:** Lines are built from Hipparcos IDs in a JSON file and redrawn between the stars' current sky positions.
- **Milky Way:** NASA's galactic-coordinate EXR was converted and rotated into equatorial (J2000) coordinates with a Python script, so it lines up with the star data.

## Running locally

1. Install **Unity 6**.
2. Clone this repo and open the folder in Unity Hub.
3. Open the main scene and press Play.

The web version lives in [`docs/`](docs) and is hosted on GitHub Pages.

## Gallery

| Menu | Explore |
| --- | --- |
| ![Menu](Images/Starry%20Sky%20Simulator%20Menu.png) | ![Explore](Images/Starry%20Sky%20Simulator%20Move%20View.png) |

## Credits

- **Star data:** Hipparcos catalogue (ESA)
- **Milky Way:** NASA/Goddard Space Flight Center Scientific Visualization Studio, Deep Star Maps 2020; Gaia DR2 (ESA/Gaia/DPAC)
- Built with Unity

## License

[MIT](LICENSE)
