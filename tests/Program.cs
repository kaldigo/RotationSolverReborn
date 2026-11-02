using RotationSolver.IPC;

static void Check(bool ok, string label) { if (!ok) throw new Exception(label); }
var lease = new ExperimentLease();
Check(!lease.Active(0, false), "No consent on startup");
try { lease.Begin(0, true); throw new Exception("Conflict accepted"); } catch (InvalidOperationException) { }
var token = lease.Begin(10, false);
Check(lease.Active(11, false), "Explicit consent");
Check(!lease.Active(11, true), "Late conflict blocks execution");
Check(!lease.Renew("wrong", 11, false), "Unknown clients cannot renew");
Check(lease.Renew(token, 100, false), "Heartbeat extends session");
Check(!lease.Active(4100, false), "Deadline is exclusive");
Check(!lease.Renew(token, 4100, false), "Expired consent cannot revive itself");
var replacement = lease.Begin(5000, false);
Check(!lease.End(token), "Old session cannot end replacement");
Check(lease.End(replacement) && !lease.Active(5001, false), "Explicit stop revokes consent");
Console.WriteLine("Experiment lease scenarios passed.");
