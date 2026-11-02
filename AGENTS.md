# Accord RSR experiment

This is a real fork of FFXIV-CombatReborn/RotationSolverReborn. Preserve upstream history, COPYING, COPYING.LESSER and attribution. The experimental work is on codex/accord-testing; main tracks the imported upstream baseline. Review upstream changes manually while the design is evolving.

The assembly, plugin identity and configuration store are AccordRSREngine. Commands are /rsrtest and /rotationtest. Do not restore stock identities or publish into the stable Accord/Wrath feed. Stock RSR and this engine must not operate together.

AccordRSRTesting is a separate, opt-in client in kaldigo/FFXIVAccordTesting. Execution requires its live session lease, no loaded stable Accord/Wrath/stock RSR, and PvE. Preserve the execution and state gates and the lost-client timeout. Never disable or reconfigure the stable engine through IPC.

This first build is a native-RSR baseline and cached-selector workbench, not a completed intent redesign. Do not represent cached preview actions as safe executable decisions. Selection may mutate action targets; future execution needs a captured decision, fresh eligibility validation, and confirmed-action continuation.

Run dotnet run --project tests/Experiment.Tests.csproj -c Release and build RotationSolver/RotationSolver.csproj before publishing. Run the experimental client's package checks too. Native acceptance remains separate. Do not import personal settings or silently transplant Wrath's four switches into RSR.
