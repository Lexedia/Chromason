# Chromason

Walls off Chroma for your pretty eyes, but keep it fancy for other people to see it :D

<img width="64" height="64" alt="son" src="https://github.com/user-attachments/assets/a7f5fae1-4387-4651-b7f2-e3c93a40fd21" />

(The name was just a portmanteau of Chroma and mason, but now the kirk worms have taken upon my brain. I'm very sorry)

## How it works

Chroma's own settings can't be split by camera. It decides when a level loads which features run and which
environment is built, and every camera sees the same scene. So Chroma is left fully on, and Chromason undoes its
effects only while a camera with that feature disabled is rendering, putting everything back right after.

I've been heavily inspired by [kinsi55](https://github.com/kinsi55)'s [ImBlindedByTheLights](https://github.com/kinsi55/BeatSaber_ImBlindedByTheLights) to make this mod.

## Requirements

- Chroma, Heck, SiraUtil and BeatSaberMarkupLanguage
- A desktop camera mod (Camera2, ReeCamera) or the built-in smooth camera. Without one, the desktop window is just a copy of the headset view.
- Not ImBlindedByTheLights at the same time: both change the lights per camera and undo each other soo.. pick your poison c:

## Known limitations

- A map that forces a different environment can't be undone, since there is only one environment in the scene. The headset gets the map's environment with Chroma's additions hidden and moves reverted.
- Not reverted: Chroma's ring and laser speed or rotation changes, particle colours, and material changes when Chroma **is disabled** in that view.
- Vanilla lights are computed once, so they use your own colours if *any* view has both "no Chroma events" and "no map colours" on. Lights keep the map's colours in a view that only has "no map colours" on.
- Static lights in a single view are applied per camera, and may miss lights on the newer environments that use GSL. With static lights in both views, the level loads with the game's own static lights instead, the same as its "Static lights -> No Effects" setting.
- Sabres colours are global, meaning you either get your own sabres colours from your colour scheme, or the map's colour scheme. A per view override would be too much code to maintain for little gains.
<sub>Even if I were to implement such thing (which I will not), mods like ReeSabers implement setting custom colours for your sabres, which I'm sure would break with Chromason; sorgy :c</sub>

## Building

Chromason uses [Legato](https://github.com/ScoreSaber/legato) to build one source tree (written against 1.40.8) for
several game versions.

1. Create `Chromason.user.props` next to the project, pointing at a modded install:
   ```xml
   <Project>
     <PropertyGroup>
       <BeatSaberDir>/path/to/Beat Saber</BeatSaberDir>
     </PropertyGroup>
   </Project>
   ```
2. `dotnet build -c Release` builds for that install (1.40.8 by default, change it with
   `-p:LegatoTargetVersion=1.44.1`).
3. Bravo, you now have a dll.

## Credits

- [kinsi55](https://github.com/kinsi55)'s [ImBlindedByTheLights](https://github.com/kinsi55/BeatSaber_ImBlindedByTheLights), which showed how to change what each camera sees, and gave the idea for static lights. Files with code adapted from it say which parts at the top, under its MIT licence (see [LICENCE](LICENCE)).
- [Aeroluna](https://github.com/Aeroluna)'s [Chroma and Heck](https://github.com/Aeroluna/Heck), which Chromason is built around.
- [ScoreSaber](https://github.com/ScoreSaber)'s [Legato](https://github.com/ScoreSaber/legato), for building against several game versions from one source tree.
- [TheBlackParrot](https://git.gay/TheBlackParrot)'s [NoteTweaks](https://git.gay/TheBlackParrot/BS_NoteTweaks), whose note colours Chromason follows.

