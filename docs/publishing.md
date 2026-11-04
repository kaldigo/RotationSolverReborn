# RSR testing candidates

The testing branch retains InternalName and assembly RotationSolver. It builds an opt-in candidate with an immutable `testing-vA.B.C.D` tag. It does not overwrite a stable feed, publish a release, or install the plugin.

The workflow obtains the current stable RotationSolver entry from the upstream CombatRebornRepo feed. The generated candidate overlay changes only TestingAssemblyVersion, TestingDalamudApiLevel and DownloadLinkTesting when the candidate is newer. A central public testing feed must recompute this overlay against fresh stable entries on every sync; never publish the captured stable snapshot as a separately maintained permanent feed.

The build must verify the actual DLL and manifest version match the requested version. Packaging requires RotationSolver.dll, ECommons.dll, RebornMaterial.dll and the included upstream/dependency notices. The checker verifies checksums, identity and the unchanged stable fields. Source and immutable release assets must be available before any feed points at the candidate.

Native installation, action hooks, manual/automatic contention and performance acceptance remain separate from compilation and packaging checks.
