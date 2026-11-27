from pathlib import Path
import subprocess
import tempfile
import unittest

class LimitBreakTests(unittest.TestCase):
    def test_manual_priority(self):
        root = Path(__file__).resolve().parents[1]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)
            (path/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><AllowUnsafeBlocks>true</AllowUnsafeBlocks><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>')
            (path/'ManualLimitBreak.cs').write_bytes((root/'RotationSolver/IPC/ManualLimitBreak.cs').read_bytes())
            (path/'Program.cs').write_text(r'''using FFXIVClientStructs.FFXIV.Client.Game;
using RotationInterop;
using Clock = RotationInterop.Environment;
unsafe {
    var am = ActionManager.Instance();
    void Check(bool ok, string reason) { if(!ok) throw new Exception(reason); }
    void Press() => ManualLimitBreak.Input(am, ActionType.GeneralAction, 3, false);
    void Queue(uint id, ulong target) { am->ActionQueued=true; am->QueuedActionType=ActionType.Action; am->QueuedActionId=id; am->QueuedTargetId.Id=target; }
    var gcd = am->GetRecastGroupDetail(57);
    gcd->IsActive=true; gcd->Total=2.5f; gcd->Elapsed=0;
    Clock.TickCount64=1000; Press(); Clock.TickCount64+=2501;
    Check(ManualLimitBreak.Waiting(),"runner resumed before LB got its post-GCD retry");
    am->CastActionId=123; am->CastTimeTotal=5; am->CastTimeElapsed=1;
    Check(ManualLimitBreak.InputDeadline(am,1000)==6800,"longer current cast was ignored");
    am->CastActionId=0; gcd->IsActive=false;
    Clock.TickCount64=10000;
    using (ManualLimitBreak.TrackRunner()) Queue(34606,42);
    Press(); Check(!am->ActionQueued && ManualLimitBreak.Waiting(),"Viper queue beat manual LB");
    am->CastActionType=ActionType.Action; am->CastActionId=201;
    Clock.TickCount64+=2500; Check(ManualLimitBreak.Waiting(),"cast lost protection after input window");
    am->CastActionId=0;am->AnimationLock=1.5f;
    Clock.TickCount64+=800;Check(ManualLimitBreak.Waiting(),"animation lock lost protection");
    am->AnimationLock=0;Clock.TickCount64+=1500;Check(!ManualLimitBreak.Waiting(),"failed to resume");
    Queue(34606,42); Press();Check(am->ActionQueued,"cleared manual queue");
    am->ActionQueued=false;
    using (ManualLimitBreak.TrackRunner()) Queue(34606,42);
    Check(!ManualLimitBreak.HasManualQueue(),"owned runner queue mistaken for manual");
    Queue(34606,99);Check(ManualLimitBreak.HasManualQueue(),"different recipient queue was not protected");Press();Check(am->ActionQueued,"cleared a different recipient");
    am->ActionQueued=false;
    Clock.TickCount64+=2000;Check(!ManualLimitBreak.Waiting(),"rejected input paused indefinitely");
    Press();am->AreaTargetingActionType=ActionType.Action;am->AreaTargetingActionId=201;
    Clock.TickCount64+=6000;Check(ManualLimitBreak.Waiting(),"ground placement timed out");
    am->AreaTargetingActionId=0;Clock.TickCount64+=700;Check(!ManualLimitBreak.Waiting(),"canceled ground target never resumed");
    Queue(201,42);Clock.TickCount64+=2000;Check(ManualLimitBreak.Waiting(),"queued LB lost protection");
    am->ActionQueued=false;Check(!ManualLimitBreak.HasManualQueue(),"inactive retained queue ID blocked runner");Clock.TickCount64+=2000;Check(!ManualLimitBreak.Waiting(),"abandoned queue did not recover");
    ManualLimitBreak.Input(am,ActionType.Action,777,false);Check(ManualLimitBreak.Waiting(),"direct PvP LB bypassed input guard");
    Clock.TickCount64+=2000;Check(!ManualLimitBreak.Waiting(),"direct input stuck");
    ManualLimitBreak.Input(am,ActionType.Action,3,false);Check(!ManualLimitBreak.Waiting(),"ordinary action 3 mistaken for general LB");
    Clock.TickCount64+=3000;
    using (ManualLimitBreak.TrackRunner()) Queue(34606,42);
    Check(ManualLimitBreak.Input(am,ActionType.Item,4554,false),"manual item did not bypass interception");
    Check(!am->ActionQueued && ManualLimitBreak.Waiting(),"runner queue displaced Phoenix Down");
    am->CastActionType=ActionType.Action;am->CastActionId=133; // Native item casts may expose their action instead of the item ID.
    Clock.TickCount64+=12000;Check(ManualLimitBreak.Waiting(),"long item cast lost ownership");
    am->CastActionId=0;am->AnimationLock=0;
    Clock.TickCount64+=600;Check(!ManualLimitBreak.Waiting(),"completed/cancelled item did not resume");
    using (ManualLimitBreak.TrackRunner()) Check(!ManualLimitBreak.Input(am,ActionType.Item,4554,false),"automatic item mistaken for manual");
    using (ManualLimitBreak.TrackRunner()) { am->ActionQueued=true;am->QueuedActionType=ActionType.Item;am->QueuedActionId=999;am->QueuedTargetId.Id=9; }
    Check(!ManualLimitBreak.Waiting(),"owned automatic consumable queue created a manual hold");
    am->ActionQueued=false;
    using (ManualLimitBreak.TrackRunner()) {ManualLimitBreak.Input(am,ActionType.GeneralAction,3,false);Check(!ManualLimitBreak.Waiting(),"runner input mistaken for manual");}
}
namespace RotationInterop { static class Environment { public static long TickCount64; } }
namespace AccordDiagnostics { static class DiagnosticLog { public static void Event(string k,Func<object> f){} } }
namespace Lumina.Excel.Sheets { class Action {public Category ActionCategory;} struct Category {public uint RowId;} }
namespace ECommons.DalamudServices {
 static class Svc {public static Data Data=new();}
 class Data {public Sheet GetExcelSheet<T>()=>new();}
 class Sheet {public bool TryGetRow(uint id,out Lumina.Excel.Sheets.Action row){row=new(){ActionCategory=new(){RowId=id==201?9u:id==777?15u:2u}};return true;}}
}
namespace FFXIVClientStructs.FFXIV.Client.Game {
 enum ActionType { Item=2, Action=1, GeneralAction=5 }
 struct Recast { public bool IsActive; public float Total,Elapsed; }
 struct Target {public ulong Id;}
 unsafe struct ActionManager {
   static ActionManager* pointer=(ActionManager*)System.Runtime.InteropServices.NativeMemory.AllocZeroed((nuint)sizeof(ActionManager));
   static Recast* gcd=(Recast*)System.Runtime.InteropServices.NativeMemory.AllocZeroed((nuint)sizeof(Recast));
   public Recast* GetRecastGroupDetail(int group)=> group==57 ? gcd : null;
   public float CastTimeTotal,CastTimeElapsed;
   public static ActionManager* Instance()=>pointer;
   public ActionType QueuedActionType,CastActionType,AreaTargetingActionType;
   public uint QueuedActionId,CastActionId,AreaTargetingActionId;
   public Target QueuedTargetId;public bool ActionQueued;public float AnimationLock;
 }
}
''')
            result = subprocess.run(['dotnet','run','--project',str(path/'Test.csproj')],capture_output=True,text=True,timeout=120)
            self.assertEqual(result.returncode,0,result.stdout+result.stderr)
