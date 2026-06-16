# QSC Component-Direct Control — Upgrade Options for Review

**Audience:** Engineering leads
**Repo:** `PepperDash/epi-qsc-qsysdsp`
**Purpose:** Decide *how* to add direct Q-Sys **component-by-name** control to the QSC EPI, given two
viable designs. This doc lays out both options with good / bad / effort so we can pick a direction
before any branch work begins.

---

## 0. TL;DR

- The EPI today speaks **ECP (External Control Protocol, TCP 1702)**, which can address **Named
  Controls** and **Snapshots** only — it **cannot** address a Q-Sys *component* by name.
- Direct component control requires **QRC (JSON-RPC 2.0, TCP 1710)**.
- **Key technical fact:** **QRC is fully self-contained and is a functional *superset* of ECP.** It
  needs *nothing* from ECP — not the heartbeat, not the change groups, not the connection. Anything
  ECP does today, QRC can do, **plus** component-direct control.
- Therefore the decision is **not** "can QRC work alone?" (it can) — it's **risk vs. cleanliness vs.
  effort**. Two designs are on the table:
  - **Option 1 — Dual transport (additive):** keep ECP for existing features, add a QRC connection
    only for component-direct blocks. Lowest risk, but two protocols live in one device.
  - **Option 2 — QRC-only (single transport):** replace ECP entirely; every existing feature is
    re-implemented over QRC. Cleanest end state, larger rewrite/regression, but fully vetted on a
    branch before merge.

> Stakeholder lean noted: preference is to **move off ECP and standardize on QRC** (Option 2),
> provided existing **config names and parent/child relationships are preserved** and the only
> visible change is a transport switch. Option 2 below is designed to honor exactly that.

---

## 1. Does QRC depend on ECP? (the question that drives everything)

**No.** QRC operates completely independently of ECP:

| Function | ECP (1702) today | QRC (1710) equivalent | Needs ECP? |
|----------|------------------|------------------------|-----------|
| Connection / auth | TCP 1702, `login` | TCP 1710, `Logon` | No |
| Keep-alive / heartbeat | `cgp 2` poll, `CheckSubscriptions` | `NoOp` on the QRC socket | **No** |
| Named control get/set | `cg` / `csv` / `css` | `Control.Get` / `Control.Set` | No |
| Feedback subscription | `cgc` / `cgsna` / `cv` | `ChangeGroup.AddControl` + `AutoPoll` | No |
| Snapshots / presets | `ssl` / `sss` | `Snapshot.Load` / `Snapshot.Save` | No |
| Failover status | `sg` / `sr` | `StatusGet` + `EngineStatus` | No |
| **Component by name** | *(impossible)* | `Component.Get` / `Component.Set` | No |

**Conclusion:** A QRC-only device is viable. The *only* reason to keep ECP is to avoid re-implementing
and re-testing the existing, production-proven feature code — i.e. risk management, not capability.

---

## 2. Option 1 — Dual transport (additive QRC for component-direct only)

Keep the entire ECP code path exactly as-is. Add a **second, optional** QRC connection that is opened
only when component-direct blocks are configured. A device-level config flag (or per-child flag)
tells the keyed device which control method a given block uses.

### Architecture

```
                         QscDsp (one Essentials device)
   ┌───────────────────────────────┬───────────────────────────────┐
   │  ECP (TCP 1702) — UNCHANGED    │  QRC JSON-RPC (TCP 1710) — NEW │
   │  Named Controls, Snapshots,    │  Component.Get / Component.Set │
   │  dialers, cameras, failover    │  ChangeGroup auto-poll         │
   │  own heartbeat (cgp)           │  own keep-alive (NoOp)         │
   └───────────────────────────────┴───────────────────────────────┘
        existing child types                new child type:
        (level/mute/dialer/camera/preset)   QscDspComponentControl
```

### Config impact

- **Existing config unchanged.** All current blocks keep working verbatim.
- **Add** an optional `qrcControl` connection block (address + port 1710) and a new
  `componentControlBlocks` section. A device-level `controlMethod` is **not required**; presence of
  `qrcControl` simply enables the second socket.

```jsonc
"properties": {
  "control":       { "method": "tcpIp", "tcpSshProperties": { "port": 1702 } },   // ECP, unchanged
  "levelControlBlocks": { /* unchanged */ },
  "qrcControl":    { "method": "tcpIp", "tcpSshProperties": { "port": 1710 } },   // NEW, optional
  "componentControlBlocks": {                                                     // NEW
    "cafeOut1": { "label": "Cafe Out 1", "componentName": "Router", "controlName": "select.1" }
  }
}
```

### Good
- **Lowest risk.** Zero change to proven ECP features; blast radius is only the new component code.
- **Ships fastest.** ~1 new child class + a small JSON-RPC client + a few joins.
- **Fully back-compatible.** Existing configs run identically with no edits.
- **Incremental.** Can later port more features to QRC type-by-type and retire ECP gradually.

### Bad
- **Two protocols in one device** — two sockets, two parsers, two keep-alives to one Core (supported
  by Q-Sys, but architecturally "ugly").
- **Doesn't advance the strategic goal** of standardizing on QRC; ECP debt remains.
- **Split mental model** for integrators (some blocks ECP, some QRC).

### Effort: **Low** (≈ small feature branch)
- New: `QscDspComponentControl`, JSON-RPC message model, `componentControlBlocks` config, QRC
  connect/logon/NoOp, change-group parse, new bridge joins.
- Existing files: additive only (config object, device wiring guarded by `qrcControl != null`,
  bridge link loop).

---

## 3. Option 2 — QRC-only (single transport, ECP removed)

Replace ECP with QRC as the device's **only** transport. Every existing feature (levels, mutes,
dialers, cameras, presets, failover) is re-implemented over QRC, **and** component-direct control is
added. **Config keys, labels, and parent/child relationships are preserved** — the only structural
change is a transport switch and how each block addresses its target (Named Control vs. component).

### Architecture

```
                         QscDsp (one Essentials device)
   ┌───────────────────────────────────────────────────────────────┐
   │  QRC JSON-RPC (TCP 1710) — single transport                    │
   │  Control.Get/Set ...... Named-Control blocks (existing keys)   │
   │  Component.Get/Set ..... component-direct blocks (new)         │
   │  ChangeGroup.AddControl / AddComponentControl + AutoPoll       │
   │  Logon + NoOp keep-alive                                       │
   └───────────────────────────────────────────────────────────────┘
        same child types, same keys/labels, same bridge joins
        (level/mute/dialer/camera/preset) now driven over QRC
```

### Config impact (designed to preserve names & relationships)

- **Device-level switch** selects transport. Default stays ECP for safety until cutover; `qrc`
  opts a device into the new single-transport path:

```jsonc
"properties": {
  "controlMethod": "qrc",                 // NEW: "ecp" (legacy/default) | "qrc"
  "control": { "method": "tcpIp", "tcpSshProperties": { "port": 1710 } },
  "levelControlBlocks": {                 // SAME keys/labels/relationships
    "fader1": { "label": "Fader 1", "levelInstanceTag": "MainVol", "hasMute": true }
  },
  "componentControlBlocks": {             // NEW capability, same child pattern
    "cafeOut1": { "label": "Cafe Out 1", "componentName": "Router", "controlName": "select.1" }
  }
}
```

- A block with `levelInstanceTag` is driven via `Control.Set` (Named Control) over QRC.
- A block with `componentName` + `controlName` is driven via `Component.Set`.
- **Same config schema names, same device keys, same join map** — integrators see continuity.

### Good
- **Single clean protocol.** One socket, one parser, one keep-alive; QRC is the modern QSC API.
- **Achieves the strategic goal** — ECP debt eliminated; future QSC features (Mixer, LoopPlayer, PA,
  richer status) become reachable.
- **Uniform mental model** — every block is "QRC", whether Named Control or component.
- **Config continuity** — existing keys/labels/parent-child relationships preserved.

### Bad
- **Largest regression surface.** Every existing feature is re-implemented and must be re-verified
  on hardware (levels, mute ramping, dialer call states, camera PTZ/presets, failover Primary/Active).
- **Two implementations during transition** if we keep `controlMethod: ecp` for back-compat (ECP +
  QRC code coexist until ECP is formally retired).
- **Higher up-front cost & longer branch** before it can merge.
- **Subscription/feedback semantics differ** (ECP `cv` text vs QRC change-group JSON) — all feedback
  parsing rewritten.

### Effort: **High** (≈ near-rewrite of the comms + every child type's I/O)
- Rewrite: connection/auth/keep-alive; feedback subscription/parsing; level/mute set+ramp; dialer
  command set & status; camera control & online; preset load/save; failover status.
- Add: component-direct blocks.
- Validate: full hardware regression of **every** existing feature, not just the new one.

---

## 4. Side-by-side

| Dimension | Option 1 — Dual transport | Option 2 — QRC-only |
|-----------|---------------------------|---------------------|
| New capability (component-direct) | ✅ | ✅ |
| ECP code touched | None | Removed / replaced |
| Regression surface | New code only | **Every existing feature** |
| Sockets per Core | 2 | 1 |
| Strategic goal (off ECP) | ✗ (debt remains) | ✅ |
| Config back-compat | ✅ no edits needed | ✅ keys preserved; transport switch added |
| Integrator mental model | Mixed (ECP+QRC) | Uniform (QRC) |
| Effort | **Low** | **High** |
| Risk to production | **Low** | Medium–High (mitigated by full branch vetting) |
| Future QSC features (Mixer/PA/etc.) | Partially | ✅ unlocked |
| Time to ship | Short | Longer |

---

## 5. Recommendation

Both are legitimate; the choice is **speed/safety now** vs. **clean strategic platform**.

- If the priority is **ship component control quickly with minimal risk** → **Option 1**.
- If the priority is **standardize on QRC and retire ECP**, and the branch will be **fully vetted on
  hardware before merge** (as stated) → **Option 2** is the right long-term call. The stated
  preference (off ECP, preserve config/relationships, transport switch only) maps directly to
  Option 2's design.

**Suggested path (best of both):** Build **Option 2** behind a `controlMethod` switch that **defaults
to `ecp`**. This lets us:
1. Develop and hardware-vet the full QRC implementation on a branch without disrupting any existing
   deployment.
2. Flip individual sites to `controlMethod: qrc` once validated.
3. Make `qrc` the default and retire ECP in a later release once field-proven.

This reaches the QRC-only end state **safely and incrementally**, while keeping config keys and
parent/child relationships intact throughout.

---

## 6. Open items to confirm before coding (Option 2 in particular)

1. **Official QRC reference** — QRC is documented by QSC (Q-SYS External Control / QRC protocol) but
   not strictly open source. Two schema details to lock down:
   - `Component.Set` value field (`Value` vs `Position`) and the `Ramp` field name/units for an
     integer `select.N`.
   - Change-group **auto-poll** wire format (JSON-RPC notification `ChangeGroup.Poll` with a
     `Changes[]` array vs. plain responses) and the `Changes` item shape
     (`Component`, `Name`, `Value`, `String`, `Position`).
2. **Failover parity** — confirm `StatusGet` + `EngineStatus` over QRC reproduces today's
   `IsPrimary`/`IsActive` behavior on redundant Cores.
3. **Auth** — confirm whether target Cores require `Logon`, and credential handling parity with the
   existing ECP `login` flow.
4. **Decision owner & timeline** — who signs off Option 1 vs 2, and the hardware available for
   regression testing.

---

## 7. Decision requested

> Please indicate: **Option 1 (dual, fast/low-risk)** or **Option 2 (QRC-only, strategic)** — and if
> Option 2, confirm acceptance of the `controlMethod` default-`ecp` transition approach in §5. Once
> chosen, the implementation PLAN will be finalized for that option.
