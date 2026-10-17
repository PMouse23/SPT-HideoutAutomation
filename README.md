# HideoutAutomation for SPT 4.1

This port updates construction automation, Hideout In Progress integration and the existing production queue for SPT 4.1.4 and later 4.1 releases. Development checks use SPT 4.1.6.

## Features

- Construct, upgrade and install hideout areas automatically, with optional confirmation.
- Contribute items through Hideout In Progress when that optional mod is installed.
- Queue non-continuous recipes, display their queued count and cancel a queued craft with the configured modifier key.
- Keep queued ingredient payments and completed crafts across server restarts.

The production queue remains experimental. An in-game playtest is still required before treating this port as a release. Construction automation can be used independently of the queue.

## Compatibility

The client targets .NET Framework 4.7.2 and requires SPT 4.1.4 or newer in the 4.1 series. The server targets .NET 10 and references the SPTushonka 4.1.6 packages.

If `com.maschine.CraftQueue` is installed, this mod disables its own queue while keeping construction and HIP automation available. No CraftQueue source or binaries are included. Queue interception on the server requires an explicit opt-in from the matching client session, so construction-only clients do not activate it.

Changing `ProductionStacking.EnableProductionStacking` requires restarting the game. Queued ingredients remain reserved while the queue is disabled; enable it again to finish or cancel those crafts. Back up profiles and `SPT_Runtime/user/profileData` before switching between 4.0 and 4.1 builds.

## Build

Prerequisites:

- .NET 10 SDK.
- .NET Framework 4.7.2 reference assemblies.
- A local SPT 4.1.4+ installation containing the game, Unity and BepInEx assemblies. SPT 4.1.6 was used for validation.

From the repository root:

```powershell
./Build.ps1 -SPTPath "C:\SPT"
```

Or build both projects without packaging:

```powershell
dotnet build HideoutAutomation.Server/HideoutAutomation.Server.csproj -c Release -p:SPTPath="C:\SPT"
```

The packaging script creates a ZIP under `releases`. It contains only the client plugin, server mod, metadata, README and license. Game dependencies, profiles, local configurations and build caches are excluded.

## Install

1. Stop the game and server.
2. Extract the ZIP into the SPT root folder.
3. Start the server, then the game. Configure automation in the BepInEx configuration manager.

Expected paths:

```text
BepInEx/plugins/HideoutAutomation/HideoutAutomation.dll
SPT_Runtime/user/mods/HideoutAutomation.Server/HideoutAutomation.Server.dll
```

## Port fixes

- Use the deobfuscated client classes and current SPT namespaces, metadata interfaces, lifecycle methods and DI-managed server patches.
- Resolve the public stash scroll field and report the selected HIP contribution amount.
- Defer a canceled requirements reload instead of reopening the same dialog every polling cycle.
- Preserve queue collections during JSON deserialization and replace queue files atomically.
- Validate ingredient reservations, preserve FIFO order and retain entries when starting a craft fails.
- Keep queued rewards pending when collection fails. Scope reward collection to its request and clear that context on exceptions.
- Rebind completion handlers when the hideout instance changes and advance the client queue only after successful collection.

## Validation

Both projects build against SPT 4.1.6. Temporary regression checks cover game member references, Harmony targets, queue JSON roundtrips, payment validation, session opt-in and construction guards. An isolated server startup verifies DI registration and patch attachment without using existing profiles. Temporary test projects and server copies are not part of this submission.

Before release, manually verify construction/HIP prompts, multiple queued recipes, cancellation refunds, full-stash collection failure, fuel-off behavior, offline progression and restarting with queued crafts. These Unity and gameplay paths have not been validated in a live game session.

## License

MIT. Original author and license attribution are preserved.
