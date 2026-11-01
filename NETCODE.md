# Magic Utilities – Netcode

Drop-in components for **Netcode for GameObjects 2.x**: host, join, spawn players and sync simple objects without writing network code.

They are only compiled when NGO is installed. Nothing here depends on Magic Links.

- [Setup](#setup)
- [Components](#components)
- [Playing online (Multiplayer Services)](#playing-online-multiplayer-services)
- [Testing with several players](#testing-with-several-players)
- [Customizing](#customizing)

---

## Setup

### 1. Create a provider

The provider decides **how** a session is created. Right click in the Project window:

**Create > MagicUtilities > Netcode > Direct Session Provider**

The default values (`127.0.0.1`, port `7777`) work for local tests. See [Playing online](#playing-online-multiplayer-services) to play over the internet.

### 2. Set up the scene

On any GameObject of your boot scene (next to the `NetworkManager` is fine), add:

| Component | Fill in |
|---|---|
| `NetSession` | The provider |
| `NetSessionUI` | Your `UIDocument` or `PanelRenderer` (optional, see below) |
| `NetPlayerSpawner` | Your player prefab and spawn points |

> Leave the **Player Prefab** of the `NetworkManager` empty: `NetPlayerSpawner` spawns the players.

**Auto Get Network Manager** is checked by default: components use `NetworkManager.Singleton`. Uncheck it only if you have several `NetworkManager`s.

### 3. Set up the player prefab

On the player prefab (which needs a `NetworkObject` and to be registered in the Network Prefabs):

- Add `NetOwnerOnly`.
- Put what only **your own** player needs in **Owner Components / Owner Objects**: `PlayerInput`, camera, `AudioListener`, HUD.

### 4. Plug the UI

With UI Toolkit, `NetSessionUI` finds the elements by name:

| Field | Default name | Element |
|---|---|---|
| Offline Panel | `ConnectionButtons` | Shown while not connected |
| Online Panel | `InGameButtons` | Shown while connected |
| Host Button | `HostButton` | `Button` |
| Join Button | `JoinButton` | `Button` |
| Leave Button | `LeaveButton` | `Button` |
| Code Field | `CodeField` | `TextField` to type the code to join |
| Code Label | `CodeLabel` | `Label` showing the code to share |
| Status Label | `StatusLabel` | `Label` showing "Connecting...", errors... |

Missing elements are ignored: only add what your UI needs.

With uGUI or your own UI, skip `NetSessionUI` and call `NetSession` from your buttons:

```csharp
NetSession.Instance.Host();
NetSession.Instance.Join(codeInput.text);
NetSession.Instance.Leave();
```

---

## Components

### `NetSession`

Hosts, joins and leaves. Knows nothing about the UI.

| | |
|---|---|
| `Host()`, `Join(code)`, `Leave()` | Also `HostAsync`, `JoinAsync`, `LeaveAsync` to await them |
| `State` | `Offline`, `Connecting`, `Connected`, `Disconnecting` |
| `JoinCode` | The code to share once hosting |
| `LastReason` | Last error or disconnect reason |
| `IsHost` | |
| Events | `StateChanged`, `Connected`, `Disconnected(reason)`, `Failed(reason)` (C# and UnityEvents) |

`Failed` means hosting or joining did not work. `Disconnected` means the session ended after being connected (empty reason when you left yourself).

### `NetSessionUI`

Plugs a `UIDocument` or `PanelRenderer` to `NetSession`. See [Plug the UI](#4-plug-the-ui).

### `NetPlayerSpawner`

Spawns one player prefab per client, on the server.

| Mode | |
|---|---|
| `FirstFree` | First spawn point nobody uses (freed when the player leaves) |
| `RoundRobin` | Next spawn point each time |
| `Random` | |

Event: `PlayerSpawned(clientId, player)`, on the server.

### `NetOwnerOnly`

Enables components or objects only for the owner of the object, or only for the others. Everything listed is disabled until the object is spawned.

Events: `onBecameOwner`, `onBecameRemote`.

### `NetToggle`

An on/off state synced for every player. Doors, levers, lights.

```csharp
GetComponent<NetToggle>().Toggle();
```

| | |
|---|---|
| `Toggle()`, `SetOn()`, `SetOff()`, `IsOn` | |
| Who Can Change | `Everyone`, `Owner` or `Server` |
| Events | `onOn`, `onOff`, `onChanged(bool)` |

### `NetSingleUser`

Something only one player can use at a time. Chests, terminals, seats.

```csharp
public void Interact()
{
    if (_user.IsUsedByMe) _user.Release();
    else _user.TryUse();
}
```

| | |
|---|---|
| `TryUse()`, `Release()` | Only the current user can release it |
| `IsInUse`, `IsUsedByMe`, `CurrentUser` | |
| For the player who asked | `onUseGranted`, `onUseDenied`, `onUseEnded` |
| For every player | `onInUse`, `onFree` |

The server grants it to one player only, even if two ask at the same time. It is released when the user disconnects.

---

## Playing online (Multiplayer Services)

The **Direct** provider only works when players can reach the host's IP: same machine or same network.
To play with friends over the internet, use **Multiplayer Services**: players join with a short code, no port to open (it goes through Unity Relay).

### 1. Install the package

**Window > Package Manager > Unity Registry > Multiplayer Services** (`com.unity.services.multiplayer`).

### 2. Link the project to Unity Cloud

**Edit > Project Settings > Services**: select your organization and create or link a project.

### 3. Create the provider

**Create > MagicUtilities > Netcode > Multiplayer Services Session Provider**, then assign it on `NetSession` instead of the Direct one.

| Option | |
|---|---|
| Is Private | Players can only join with the code |
| Unique Player In Editor | Signs in as a new player each run in the editor. Keep it on to test with several editors. |

### 4. Add the code to your UI

- A `TextField` named `CodeField`: the code players type to join.
- A `Label` named `CodeLabel`: the code shown to the host, to share.

Players are signed in anonymously, nothing else to set up.

---

## Testing with several players

Use **Multiplayer Play Mode** (`com.unity.multiplayer.playmode`, **Window > Multiplayer > Multiplayer Play Mode**) to run up to 4 players from the editor.

- **Direct provider**: one player hosts, the others join with an empty code.
- **Multiplayer Services provider**: one player hosts, copy the code from `CodeLabel` into the others' `CodeField`. Keep **Unique Player In Editor** on.

---

## Customizing

Every component exposes C# events and UnityEvents, and its decision points are `virtual`:

| Component | Override |
|---|---|
| `NetSessionProvider` | Inherit from it for your own backend (Steam...) |
| `NetSessionUI` | `Refresh()`, `GetStatusText()`, `OnHostClicked()`, `OnJoinClicked()`, `OnLeaveClicked()` |
| `NetPlayerSpawner` | `ChooseSpawnIndex(clientId)`, `SpawnPlayer(clientId)` |
| `NetOwnerOnly` | `Apply(isOwner)` |
| `NetToggle` | `CanChange(clientId, value)`, `Notify(value)` |
| `NetSingleUser` | `CanUse(clientId)`, `NotifyState()`, `NotifyAnswer()` |
| `MultiplayerServicesSessionProvider` | `SignInAsync()` (sign in with Steam, Unity Player Accounts...) |

Example: a chest only usable from close by.

```csharp
public class NearbyOnly : NetSingleUser
{
    [SerializeField] private float _maxDistance = 3f;

    protected override bool CanUse(ulong clientId)
    {
        if (IsSpawned == false) return true;   // offline

        var player = NetworkManager.ConnectedClients[clientId].PlayerObject;
        return player != null && Vector3.Distance(player.transform.position, transform.position) <= _maxDistance;
    }
}
```
