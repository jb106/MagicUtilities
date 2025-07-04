# MagicUtilities

**MagicUtilities** is a lightweight Unity package with reusable tools to streamline common project needs. It’s modular, easy to drop in, and designed to grow over time.

---

## ✨ Features

### 🔁 Prefab Instancer

Ensures a list of prefabs is instantiated **once** across all scenes.

- Place this component in a scene.
- On load, it instantiates assigned prefabs **only once** (using a static flag).
- Auto-destroys itself if already initialized.

Great for shared systems like managers or pooled objects.

---

### 🎛️ CanvasGroup Updater

Simple component to **fade and toggle** a `CanvasGroup` from a boolean.

- Smoothly interpolates alpha using `lerpSpeed`.
- Updates `interactable` and `blocksRaycasts` accordingly.
- Call `UpdateValue(bool)` to show/hide the group.

---

### 🔊 Audio System (Basic)

Modular system for playing 2D/3D sounds using pooling and `ScriptableObject` configs.

- `AudioData` stores pitch/volume ranges, cooldowns, visibility checks, etc.
- `AudioManager` handles pooling, fading, spatialization, and track volumes.
- Use `AudioDataPlayer` to trigger sounds with a position reference.

Example:
```csharp
AudioManager.Instance.PlaySound(new AudioDataPlayer(myAudioData, transform.position));
```

---

## 📦 Installing

Use **Package Manager > Add package from Git URL**:

```
https://github.com/jb106/MagicUtilities.git
```

---

## 🛠️ Requirements

- [Odin Inspector](https://odininspector.com/) for `AudioData` editor

---

## 👤 Author

Made by [@jb106](https://github.com/jb106)
```
