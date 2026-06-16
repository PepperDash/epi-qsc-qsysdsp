# PLAN — Add QSC QRC JSON-RPC Component-by-Name Control

> Branch plan for extending the existing `epi-qsc-qsysdsp` EPI to **also** support direct
> Q-Sys **component** control by name over the **QRC JSON-RPC protocol (TCP 1710)**, in
> addition to the existing **ECP Named Control protocol (TCP 1702)**.

- **Repo:** `PepperDash/epi-qsc-qsysdsp`
- **Proposed branch:** `feature/qrc-component-control`
- **Type:** Additive feature (new optional protocol path); **no regression** to existing ECP behavior.
- **Context (not committed):** A separate `Recommendation-QSC-Component-Control.md` note explains *why*
  component control is needed. It is intentionally **kept out of the repo/branch** and is referenced
  here only for background — this PLAN is self-contained.

---

## 1. Background & Problem

The EPI today speaks the **legacy External Control Protocol (ECP, TCP 1702)** via a single
`IBasicCommunication` on `QscDsp`. ECP can only address **Named Controls** and **Snapshots** —
it has **no ability to address a Q-Sys *component* by name** (e.g. a `Router` component's
`select.N` controls).

Direct component control requires the **QRC JSON-RPC protocol (QRC, TCP 1710)** — a different
transport (null-`\0`-delimited JSON-RPC 2.0 frames), different framing, different auth, and
different change-group/poll semantics.

The background note proposes a **forked/new EPI** to keep the production plugin low-risk. This plan
instead implements the requested approach: **extend the existing EPI** with QRC as an **optional,
config-gated second connection** so the entire ECP code path is untouched when QRC is not configured.
The reasoning behind "second connection" vs. a single `controlType` switch is in §3a below.

---

## 2. Scope

### In scope
- Add an **optional second `IBasicCommunication`** (QRC, TCP 1710) to `QscDsp`, created only when
  the new config block is present.
- Implement a minimal **JSON-RPC 2.0** client for QRC: `Logon`, `NoOp` keep-alive,
  `Component.Get`, `Component.Set`, `Component.GetControls`, and change-group auto-poll for feedback.
- Add a new child control type **`QscDspComponentControl`** representing one component+control pair
  (e.g. `Router` / `select.3`), with set + integer/string feedback.
- Add a new config section **`componentControlBlocks`** plus an optional **`qrcControl`** connection block.
- Add **new bridge joins** (analog set/feedback + string feedback) for component selects, in a
  non-conflicting join range.
- Add documentation/config examples to `README.md` and `configurationFile.json`.

### Out of scope (explicitly)
- **No v3 / .NET 8 / Serilog migration.** The project stays `net472` + `SERIES4` + `Debug.Console`
  to keep the regression surface small (consistent with the recommendation). Track separately.
- **No namespace/assembly rename.** See §8 Convention Notes.
- No removal or rewrite of ECP-based level/preset/dialer/camera features.
- No change to `TypeNames` / factory device type id.

---

## 3. Architecture / Approach

```
                    QscDsp (single Essentials device)
   ┌───────────────────────────────┬───────────────────────────────┐
   │  EXISTING — ECP (TCP 1702)     │  NEW — QRC JSON-RPC (TCP 1710) │
   │  Communication                 │  QrcCommunication (optional)   │
   │  PortGather  delimiter "\x0a"  │  QrcGather   delimiter "\0"    │
   │  Port_LineReceived (text)      │  Qrc_LineReceived (JSON-RPC)   │
   │  LevelControlPoints / Dialers  │  ComponentControlPoints        │
   │  / Cameras / Presets           │  (QscDspComponentControl)      │
   │  cgd/cgc/cgsna change groups   │  ChangeGroup.AddComponentCtrl  │
   └───────────────────────────────┴───────────────────────────────┘
```

- **Config-gated:** if `qrcControl` / `componentControlBlocks` are absent, **no second socket is
  opened** and behavior is byte-for-byte identical to today.
- **Isolation:** QRC gets its own `CommunicationGather` (null-delimited), its own parse method, and
  its own keep-alive timer. It does **not** share the ECP command queue or `Port_LineReceived`.
- **Feedback:** QRC change group auto-poll responses are parsed and routed to the matching
  `QscDspComponentControl` by `Component` + `Name`.

---

## 3a. Design Decision — Why a config-gated **second** connection (my opinion)

> You asked the right question: if we add QRC, why not just pick **one** protocol with a
> `controlType` switch, or migrate everything to QRC? Here is my honest reasoning.

### The key fact: QRC is a **functional superset** of ECP

QRC (JSON-RPC, 1710) can do **everything** ECP (1702) does **and more**. There is no capability ECP
has that QRC lacks. So "we must keep ECP because QRC can't do X" is **not** the reason. Mapping:

| Capability used today (ECP) | ECP verb | QRC equivalent |
|-----------------------------|----------|----------------|
| Named-control get/set (levels, mutes, faders) | `cg` / `csv` / `css` | `Control.Get` / `Control.Set` |
| Subscriptions / change feedback | `cgc` / `cgsna` / `cv` | `ChangeGroup.AddControl` + `AutoPoll` |
| Snapshots / presets | `ssl` / `sss` | `Snapshot.Load` / `Snapshot.Save` |
| Status / failover (IsPrimary/IsActive) | `sg` / `sr` | `StatusGet` + `EngineStatus` notification |
| **Component by name** (the new ask) | *(not possible)* | `Component.Get` / `Component.Set` |

So the real question is **not capability** — it's **migration risk vs. reward** on a production,
public plugin. Three realistic options:

| Option | What it means | Pros | Cons |
|--------|---------------|------|------|
| **A. Dual connection (this plan)** | Keep proven ECP code as-is; open a **second** QRC socket only for the new component blocks. | Near-zero regression to levels/dialers/cameras/presets; smallest new code surface (components only); ships fast. | Two TCP sockets to one Core; two parsers/keep-alives; protocol split is "ugly" architecturally. |
| **B. `controlType` switch (ECP *or* QRC)** | One socket. `ecp` = today's behavior, **no components**. `qrc` = **re-implement** levels, mutes, dialers, cameras, presets, failover **and** components on QRC. | Single transport; clean long-term shape; full feature parity *if* on QRC. | To actually get components you must run the `qrc` path, which means **rewriting every proven feature** on a new protocol \u2014 large regression surface. You'd maintain **two complete implementations** of every feature (ECP and QRC) indefinitely. |
| **C. QRC-only (full migration)** | Delete ECP; everything on QRC. | Cleanest end state; one protocol. | Biggest-bang rewrite + regression on a public plugin; abandons code proven in production; highest risk. |

### My recommendation

**Go with Option A now, treat Option C as the eventual end state.** Rationale:

1. **The new capability is small and isolated.** Component control is genuinely new (~1 child class +
   a JSON-RPC client + a handful of joins). Bolting it onto a second socket touches none of the large,
   battle-tested ECP code.
2. **Option B is deceptively the *most* code, not the least.** Because `controlType: ecp` can't do
   components, anyone who wants components is forced onto `qrc`, which only helps if **all** the other
   features also exist on QRC \u2014 i.e. you must reimplement and then *maintain two parallel
   implementations* of levels/mutes/dialers/cameras/presets/failover. That is a worse long-term burden
   than one extra socket.
3. **Risk is the dominant factor for a public production plugin.** Option A's blast radius is "did I
   break the new QRC block?" Options B/C's blast radius is "did I break every existing integration?"
4. **Q-SYS Cores allow multiple concurrent external-control connections,** so two sockets to one Core
   is supported and normal; the "two sockets" cost is mostly cosmetic.
5. **Clean migration path:** once QRC parity is proven in the field (component blocks first, then
   optionally levels/presets ported one type at a time behind config), ECP can be retired \u2014 arriving
   at Option C **incrementally and safely** rather than in one risky cutover.

**Bottom line:** the second connection is not because QRC is missing anything \u2014 it's a deliberate
**risk-isolation / incremental-migration** choice. If the team would rather pay the regression cost now
for a cleaner single-protocol device, Option C (QRC-only) is the architecturally "right" target and I'm
happy to re-plan for it; I'd just want hardware regression coverage for every existing feature first.

---

## 3b. QRC API Coverage — do we have everything we need?

**Short answer:** I have a solid working knowledge of the QRC method set and I'm confident it covers
this feature. QRC is **documented** by QSC (the Q-SYS *External Control / QRC Protocol* reference, e.g.
`q-syshelp.qsc.com` and the QSC developer docs) but it is **not strictly open source** \u2014 it's a
published spec. I have enough to design and implement Option A, but I'd like the **official QRC
reference PDF/page** to lock down two details before coding (see "to confirm" below). If it's handy,
please grab it; otherwise I'll proceed against my current understanding and flag any mismatch during
emulator testing.

### Methods required for this feature (Option A)

| Need | QRC method | Confidence |
|------|------------|------------|
| Authenticate (if Core requires) | `Logon` `{ User, Password }` | High |
| Keep-alive (idle drop ~60 s) | `NoOp` | High |
| Set a component control | `Component.Set` `{ Name, Controls:[{ Name, Value, (Ramp?) }] }` | High |
| Read a component control | `Component.Get` `{ Name, Controls:[{ Name }] }` | High |
| Validate control exists (optional, log) | `Component.GetControls` `{ Name }` | High |
| Discover components (optional) | `Component.GetComponents` | High |
| Feedback subscription | `ChangeGroup.AddComponentControl` `{ Id, Component:{ Name, Controls:[{ Name }] } }` | Medium |
| Auto feedback | `ChangeGroup.AutoPoll` `{ Id, Rate }` | Medium |
| Teardown | `ChangeGroup.Destroy` / `Remove` | High |
| Core/redundancy status (optional, for parity w/ failover) | `StatusGet` + unsolicited `EngineStatus` notification | Medium |

### Methods available if we later port existing features to QRC (Options B/C)

`Control.Get` / `Control.Set` (named controls), `Snapshot.Load` / `Snapshot.Save`,
`ChangeGroup.AddControl`, `Mixer.*`, `LoopPlayer.*`, `PA.*` \u2014 i.e. the full superset noted above.

### To confirm from the official QRC reference before coding

1. **Exact `Component.Set` param schema** \u2014 whether value goes in `Value` vs `Position`, and the
   `Ramp` field name/units for a router `select.N` (integer index).
2. **Change-group auto-poll wire format** \u2014 whether updates arrive as a JSON-RPC **notification**
   (`method: "ChangeGroup.Poll"` with a `Changes` array) vs. plain responses, and the `Changes` item
   shape (`Component`, `Name`, `Value`, `String`, `Position`). This drives `Qrc_LineReceived` parsing.

> Everything else (transport = TCP 1710, JSON-RPC 2.0, **null-`\0`** message framing, `Logon`/`NoOp`)
> I'm confident on. Provide the official doc if you have it and I'll reconcile points 1\u20132; otherwise the
> QRC emulator (\u00a79) will surface any schema differences quickly.

---

## 4. Change List

### 4.1 New files

| File | Purpose |
|------|---------|
| `src/QscDspComponentControl.cs` | Child control point for one component/control pair; exposes Set + `IntFeedback`/`StringFeedback`. |
| `src/QscQrcMessages.cs` | Minimal JSON-RPC 2.0 request/response/params POCOs (`Component`, `Control`, `id`, `method`, `params`). |
| `src/QscDspComponentControlBlockConfig.cs` *(or add to config file)* | Config object for `componentControlBlocks`. |

### 4.2 Modified files

| File | Change | Existing behavior preserved? |
|------|--------|------------------------------|
| `QscDspPropertiesConfig.cs` | Add `[JsonProperty("qrcControl")] ControlPropertiesConfig QrcControl` and `[JsonProperty("componentControlBlocks")] Dictionary<string, QscDspComponentControlBlockConfig> ComponentControlBlocks`. | Yes — additive optional properties only. |
| `QscDsp.cs` | Add `QrcCommunication`, `QrcGather`, `ComponentControlPoints`, optional QRC connect in the `AllDevicesActivated` handler, `Qrc_LineReceived` parser, `SendQrc(...)`, QRC `Logon`/`NoOp` keep-alive, QRC change-group subscribe. Build QRC objects in a new `CreateQrcObjects()` only when config present. | Yes — all new members are additive and guarded by `QrcControl != null`. ECP path unchanged. |
| `QscDspFactory.cs` | No required change (QRC comm is built inside `QscDsp` from config). Optional: log when QRC is enabled. | Yes. |
| `QscDspBridge.cs` | Add a `foreach (ComponentControlPoints)` block linking new joins (string name, analog set, analog/string feedback). | Yes — appended after existing links; existing joins untouched. |
| `QscDspDeviceJoinMapAdvanced` (in `QscDspBridge.cs`) | Add new `JoinDataComplete` joins in a free range (see §6). | Yes — new join numbers only. |
| `configurationFile.json` | Add example `qrcControl` + `componentControlBlocks`. | Yes — example only. |
| `README.md` | Document the new config block, joins, and QRC port 1710 requirement. | Yes. |

### 4.3 Functionally **unchanged** (must remain identical)

- ECP connection on TCP 1702, `CommunicationGather` `"\x0a"`, `Port_LineReceived`.
- `levelControlBlocks`, `presets`, `dialerControlBlocks`, `cameraControlBlocks` semantics & subscriptions
  (`cgd` / `cgc` / `cgsna` / `cg` / `cv` / `sr`).
- Heartbeat / `CheckSubscriptions` / `GenericCommunicationMonitor` behavior for ECP.
- Failover `IsPrimary` / `IsActive` parsing.
- Existing bridge join numbers and their meaning.
- `QscDspFactory.TypeNames = { "qscDsp" }`.
- Public API/signatures of all existing classes.
- Config back-compat: existing configs with **no** `qrcControl` keep working with zero behavior change.

---

## 5. Config Schema (additions)

```jsonc
"properties": {
  // --- existing ECP control (unchanged) ---
  "control": {
    "method": "tcpIp",
    "tcpSshProperties": { "address": "10.0.0.50", "port": 1702 }
  },
  "levelControlBlocks": { /* unchanged */ },

  // --- NEW: optional QRC connection (TCP 1710) ---
  "qrcControl": {
    "method": "tcpIp",
    "tcpSshProperties": {
      "address": "10.0.0.50",
      "port": 1710,
      "username": "",
      "password": "",
      "autoReconnect": true,
      "autoReconnectIntervalMs": 5000
    }
  },

  // --- NEW: component-by-name control blocks ---
  "componentControlBlocks": {
    "cafeOut1": {
      "label": "1st Floor Cafe Output 1 Select",
      "componentName": "Router",      // Q-Sys component "Code Name"
      "controlName": "select.1",      // control within the component
      "hasFeedback": true,
      "valueType": "integer"          // "integer" | "string"
    }
  }
}
```

> If `qrcControl` is omitted, `componentControlBlocks` is ignored (with a warning) and no QRC
> socket is opened.

---

## 6. Bridge Joins (proposed, non-conflicting)

Existing joins occupy roughly 1–1400 (level/preset blocks) and **3100–3137** (cameras). Component
joins start at **4001** with a per-control offset (one index per configured block):

| Join (base) | Type | Dir | Purpose |
|-------------|------|-----|---------|
| 4001 + idx | Serial | EPI→SIMPL | Component control label/name |
| 4001 + idx | Analog | SIMPL→EPI | Set component select value (integer) |
| 4001 + idx | Analog | EPI→SIMPL | Component select feedback (integer) |
| 4201 + idx | Serial | SIMPL→EPI | Set component value (string) |
| 4201 + idx | Serial | EPI→SIMPL | Component value feedback (string) |

> Final join numbers/spans to be confirmed during implementation against
> `QscDspDeviceJoinMapAdvanced`; the requirement is **no overlap** with existing joins.

---

## 7. QRC Protocol Notes (implementation reference)

- **Transport:** TCP 1710, JSON-RPC 2.0, each message terminated by a **null byte `\0`**
  (use a dedicated `CommunicationGather` with `"\0"` — *not* the ECP `"\x0a"`).
- **Auth:** send `Logon` with `User`/`Password` if the Core requires it (mirror existing
  `login_required` handling pattern).
- **Keep-alive:** send `NoOp` periodically (Core drops idle connections ~60 s).
- **Set:** `Component.Set` → `{ "Name": "<componentName>", "Controls": [ { "Name": "<controlName>", "Value": <v> } ] }`.
- **Get:** `Component.Get` → `{ "Name": "<componentName>", "Controls": [ { "Name": "<controlName>" } ] }`.
- **Feedback:** create a change group via `ChangeGroup.AddComponentControl` + `ChangeGroup.AutoPoll`,
  parse `Poll`/change responses and route to the matching `QscDspComponentControl`.
- **Discovery (optional/log only):** `Component.GetControls` to validate `controlName` exists.

---

## 8. Convention Compliance Notes (flagged — not changed by this branch)

The orchestrator EPI conventions expect namespace `PepperDash.Essentials.Plugins` and a v3
(`net8`) project. This repo currently uses:

- **Namespace `QscQsysDspPlugin`** (not `PepperDash.Essentials.Plugins`).
- **`net472` + `SERIES4` define + `Debug.Console`** (v2-style), with `PepperDashEssentials 2.13.1`.

These are **pre-existing deviations**. Changing them is **out of scope** for this feature branch
because a namespace/framework rename is a breaking, high-churn change unrelated to QRC control and
would enlarge the regression surface. **Flagging per convention rules** so the team can schedule a
separate v3-migration branch if desired. Confirm acceptance before merge.

---

## 9. Testing & Validation

1. **Build:** 0 errors / 0 warnings (`QscQsysDspPlugin.4Series.sln`).
2. **Regression (ECP):** with a config that has **no** `qrcControl`, confirm identical behavior —
   no second socket, levels/presets/dialers/cameras work as before.
3. **QRC unit test:** add a Python QRC emulator (TCP 1710, `\0`-framed JSON-RPC) via the
   `epi-emulator` agent; verify `Component.Set` / `Component.Get` / change-group feedback.
4. **Hardware test:** validate against a real Q-Sys Core with a `Router` component before commit.
5. **Bridge:** verify new component joins move the router output and report feedback; verify no
   existing join changed.

> Per EPI bug/feature workflow: **do not auto-commit.** Build → ask user to test on hardware →
> commit only after confirmation.

---

## 10. Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Second socket regresses ECP timing/queue | QRC fully isolated (own gather, parser, timer); gated by config. |
| JSON-RPC framing (`\0`) mis-parsed | Dedicated `CommunicationGather("\0")`; do not reuse ECP gather. |
| Idle disconnect on 1710 | `NoOp` keep-alive timer + `autoReconnect`. |
| Join overlap with cameras (3100–3137) | New range at 4000+; validated against join map. |
| Scope creep into v3 migration | Explicitly out of scope (§2, §8). |

---

## 11. Milestones

1. Config objects (`qrcControl`, `componentControlBlocks`) + deserialization.
2. QRC comms + JSON-RPC message model + connect/logon/keep-alive (isolated).
3. `QscDspComponentControl` (Set + feedback) + change-group subscribe/parse.
4. Bridge joins + `LinkToApiExt` wiring.
5. Emulator + tests; README + `configurationFile.json` examples.
6. Hardware validation → commit/PR.
