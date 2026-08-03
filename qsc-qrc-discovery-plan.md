# QSC Q-SYS QRC — Component/Control Discovery Feature Plan

## Background

**Question:** Can Crestron poll QSC via the QRC protocol to get a list of available controls?

**Answer:** Yes, via a two-step discovery flow (no need to know control names ahead of time):

- `Component.GetComponents` — returns every named component in the Q-SYS design (Name, Type, Properties). No params needed.
- `Component.GetControls` — pass a component `Name`, returns all its controls (Name, Type, Value, String, min/max, Direction, etc.)

**Flow:**
1. Send `Component.GetComponents` to enumerate all components in the design.
2. Loop through results, calling `Component.GetControls` for each `Name`.

**Other relevant methods:**
- `Control.Get` / `Control.GetValues` — for querying specific **Named Controls** directly, if names are already known.

**Protocol notes:**
- QRC runs over raw TCP on **port 1710**, JSON-RPC 2.0.
- Messages are **null-terminated** (`\0`), not newline-terminated (differs from many Crestron string-protocol conventions).
- No first-party Crestron driver exposes full discovery — requires raw `TCPClient` socket handling + manual JSON parsing (e.g. Newtonsoft.Json in SIMPL#).

---

## Target Repo

[`PepperDash/epi-qsc-qsysdsp`](https://github.com/PepperDash/epi-qsc-qsysdsp) — PepperDash Essentials plugin for QSC Q-Sys DSP (4-Series).

- **`main` branch:** uses the older text-based **ECP** (External Control Protocol) on port 1702, not JSON QRC.
- **`qrc-protocol` branch:** contains a real QRC (JSON-RPC) implementation already:
  - `src/RemoteControlProtocol/QsysQrcController.cs`
  - `src/RemoteControlProtocol/QsysQrcFactory.cs`
  - `src/RemoteControlProtocol/QsysQrcPropertiesConfig.cs`
  - `src/Interfaces/IQsys.cs` (protocol-neutral interface shared with ECP)
  - `src/Shared/*` — level controls, dialers, cameras, presets, bridge/join map

This branch sends JSON-RPC requests via `SendRequest(method, params)` and parses inbound frames in `Port_LineReceived` → `ProcessResult`, but is currently **fire-and-forget**: no request-ID correlation exists yet.

---

## Feature: Get All Components/Controls → JSON File on Processor

### Key design gap
- `SendRequest` increments `_requestId` but discards it.
- `ProcessResult` routes purely by JSON *shape* (array vs. object with `Controls`), assuming every response is a control update.
- Adding discovery safely requires a **pending-request map** so discovery responses don't get misrouted into control-update dispatch.

### Plan

**1. Add request/response correlation**
- New field: `Dictionary<int, Action<JToken>> _pendingRequests` in `QsysQrcController`.
- Overload `SendRequest(method, params, Action<JToken> onResult = null)` that stores the callback by id before sending.
- In `Port_LineReceived`, check `json["id"]` against `_pendingRequests` first; if found, invoke callback and remove; otherwise fall through to existing behavior.
- **Also correlate the `error` branch**, not just `result`: if an id in `_pendingRequests` comes back with a JSON-RPC `error` instead of `result`, the callback must still fire (e.g. with `null`) and be removed, otherwise a single component erroring on `Component.GetControls` leaves the pending map dangling until the safety timeout fires.
- `_pendingRequests` is written from the console-command/bridge thread (on send) and read from the socket receive thread (`Port_LineReceived`, on response) — wrap add/remove/lookup in a `lock` to avoid races.

**2. New POCOs** (new file, e.g. `src/Shared/QsysComponentInfo.cs`)
- `QsysComponentProperty { Name, Value }`
- `QsysComponent { Name, Type, Properties }`
- `QsysControlInfo { Name, Type, Value, String, Position, Direction, ValueMin, ValueMax, StringMin, StringMax }`
- `QsysComponentWithControls { Name, Type, Controls: List<QsysControlInfo> }` — final export shape.
- **Recommendation:** add a computed `SuggestedTag` field to `QsysControlInfo` (`"{ComponentName}#{ControlName}"`) so an integrator can copy a value straight out of the exported JSON into `levelInstanceTag`/`muteInstanceTag`/etc. in `configurationFile.json`, matching this plugin's existing Component Control tag convention documented in the README.

**3. Discovery logic in `QsysQrcController`**
- `public void GetAllComponentsAndControls()` (also exposed via `IQsys`):
  - Send `Component.GetComponents`; callback deserializes result → `List<QsysComponent>`.
  - Track `_pendingComponentCount`; for each component, send `Component.GetControls { Name }` with its own callback.
  - Each callback appends to a `List<QsysComponentWithControls>`, decrements pending count.
  - When count reaches 0, call `WriteComponentsToFile(...)`.
  - Add a `CTimer` safety timeout (~15s) so the file still writes with whatever completed if a component never responds.
  - **Throttle instead of firing all requests at once.** Real designs can return well over 100 components from `Component.GetComponents`; blasting 100+ `Component.GetControls` requests in a tight loop risks overrunning the Core's receive queue or the reply-side `CommunicationGather` buffer. Recommend chaining sequentially (send the next `Component.GetControls` only from inside the previous callback) or capping in-flight requests (e.g. 5 at a time) instead of one-shot fan-out.

**4. File output**
- Use `Crestron.SimplSharp.CrestronIO` (`Directory`, `File`, `StreamWriter`) — compatible with `net472`/4-series target already in use.
- Path: **confirmed** — use `PepperDash.Essentials.Core.Global.FilePathPrefix` (backed by `Global.ApplicationDirectoryPathPrefix`), not a raw `Directory.GetApplicationRootDirectory()` + manual `"user"` join. This is the helper the rest of Essentials uses to resolve the correct writable directory per-processor-family, so it stays consistent with how config/log files are already located on this platform.
  - e.g. `Path.Combine(Global.FilePathPrefix, "qsys-components.json")`.
- **Decided:** fixed filename, overwritten on every run (no timestamp) — simplest, and avoids accumulating files on space-constrained 4-series storage.
- **Decided:** export every returned component/control unfiltered for v1 — no Type allow-list. Revisit filtering once real output from a live Core/design is seen, since design size is currently unknown (see testing note below).
- Serialize with `Newtonsoft.Json` (already a dependency), `Formatting.Indented`.
- Wrap in try/catch + log via existing `this.LogInformation` / `this.LogError` pattern.

**5. Trigger mechanism**
- **Decided:** no bridge join for v1 — keeps the shared `QsysDeviceJoinMapAdvanced` (used by both ECP and QRC) untouched.
- `GetAllComponentsAndControls()` is a plain `public void` method (parameterless, no return value) on `QsysQrcController`, so it's usable two ways:
  1. **`devjson`** — Essentials' `DeviceJsonApi.DoDeviceAction` console command invokes public device methods by name via reflection, e.g. `devjson:1 {"deviceKey":"dsp-1","methodName":"GetAllComponentsAndControls"}`. Works automatically since the method is public; no extra code needed for this path.
  2. **Global `getcomponents <deviceKey>` console command** — a single command (not one per device key) registered once via a static guard in `CustomActivate()`, so it works regardless of how many QRC devices are active. The handler resolves the device with `DeviceManager.GetDeviceForKey<IQsys>(key)` and calls `GetAllComponentsAndControls()` on it. Registering by key argument instead of baking the key into the command name (`"getcomponents" + Key`) matters once Essentials is running in a program slot > 1 alongside other programs/plugins that may also register console commands — it keeps a single, predictable command name instead of one per device instance.
- Since ECP doesn't implement discovery, `DeviceManager.GetDeviceForKey<IQsys>` will still resolve an ECP-typed device key, but its `GetAllComponentsAndControls()` stub just logs a warning; `devjson` against an ECP key behaves the same way.

**6. Interface update**
- Add `void GetAllComponentsAndControls();` to `IQsys.cs`.
- Stub as no-op/log-warning in the ECP controller, since ECP doesn't support this discovery model.

**7. Testing**
- Bench test against a live Core or Q-SYS Emulator on port 1710.
- Verify null-terminator framing holds up with large multi-KB responses (big designs → large payloads); confirm `CommunicationGather` buffer handles it.
- Confirm file appears via `Global.FilePathPrefix` and is valid JSON (Text Console `type`, or SFTP pull).
- **Expected design size is currently unknown** — keep the ~15s safety timeout and in-flight request cap conservative/configurable (e.g. constants near the top of `QsysQrcController`) until tested against a real Core, rather than hardcoding values tuned for a small test design.

---

## Files to touch/add (qrc-protocol branch)

| File | Change |
|---|---|
| `src/RemoteControlProtocol/QsysQrcController.cs` | Add pending-request map (with error correlation + locking), `GetAllComponentsAndControls()`, `WriteComponentsToFile()`, global `"getcomponents"` console command (registered once, takes device key as its argument) in `CustomActivate()` |
| `src/Shared/QsysComponentInfo.cs` (new) | POCOs for components/controls (incl. `SuggestedTag`) |
| `src/Interfaces/IQsys.cs` | Add `GetAllComponentsAndControls()` to interface |
| `src/ExternalControlProtocol/QsysEcpController.cs` | Stub interface method (no-op/log) |

`src/Shared/QsysBridge.cs` is **not** touched — triggering is via `devjson`, not a bridge join.

**Next step:** implement steps 1–4 (controller/POCO/file-write code) against the actual branch files.
