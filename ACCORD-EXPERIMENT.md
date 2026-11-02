# RSR fork for Accord testing

The opt-in fork uses `AccordRSREngine`, a separate identity and configuration store from stock `RotationSolver`. It is based on the commit in `UPSTREAM.ref`. The original source, job rotations and upstream history are retained. The experimental client is [FFXIVAccordTesting](https://github.com/kaldigo/FFXIVAccordTesting).

Install both experimental plugins from the separate testing feed. Disable stock RSR before loading this engine. Disable stable Accord and Wrath before enabling a test session through `/accordtest`. Use `/rsrtest` for the engine's settings. Each client load starts without consent; the client must renew its session every second. A four-second heartbeat expiry, a conflicting loaded plugin, or PvP blocks execution. Ending the session stops the rotation. Existing RSR auto-start settings still apply during a consented session.

The initial build runs RSR's own rotation with its own settings and displays existing cached healing, protection and movement suggestions. Open RSR's full control window to populate those suggestions. It does not implement redesigned hotbars, targeted intent execution, or Accord automation policies. It does not import stable Accord choices or alter stable configurations. Automatic rotation still executes actions when using manual enemy targeting.

The fork removes RSR's automatic disabling of Wrath. It gates the main update, state activation, action entry point, native actions/items and auto-attack intervention. This preserves a stable fallback: end the experiment, disable both test plugins and re-enable stable Accord/Wrath. No experimental settings need importing back.

Build and lease tests pass locally for the initial source. Native acceptance is pending: verify load/reload order, client unload or heartbeat loss, late conflict loading, session stop, PvP transitions, rotation execution and full-control selector observations. A successful build does not establish combat correctness.
