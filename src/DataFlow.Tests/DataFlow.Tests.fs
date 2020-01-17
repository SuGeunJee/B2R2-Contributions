namespace B2R2.DataFlow.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting

open B2R2
open B2R2.FrontEnd
open B2R2.DataFlow
open B2R2.MiddleEnd

[<TestClass>]
type DataFlowTests () =

  (*
    Example 1: Fibonacci function

    unsigned int fib(unsigned int m)
    {
        unsigned int f0 = 0, f1 = 1, f2, i;
        if (m <= 1) return m;
        else {
            for (i = 2; i <= m; i++) {
                f2 = f0 + f1;
                f0 = f1;
                f1 = f2;
            }
            return f2;
        }
    }

    00000000: 8B 54 24 04        mov         edx,dword ptr [esp+4]
    00000004: 56                 push        esi
    00000005: 33 F6              xor         esi,esi
    00000007: 8D 4E 01           lea         ecx,[esi+1]
    0000000A: 3B D1              cmp         edx,ecx
    0000000C: 77 04              ja          00000012
    0000000E: 8B C2              mov         eax,edx
    00000010: 5E                 pop         esi
    00000011: C3                 ret
    00000012: 4A                 dec         edx
    00000013: 8D 04 31           lea         eax,[ecx+esi]
    00000016: 8D 31              lea         esi,[ecx]
    00000018: 8B C8              mov         ecx,eax
    0000001A: 83 EA 01           sub         edx,1
    0000001D: 75 F4              jne         00000013
    0000001F: 5E                 pop         esi
    00000020: C3                 ret

    8B5424045633F68D4E013BD177048BC25EC34A8D04318D318BC883EA0175F45EC3
  *)

  let binary =
    [| 0x8Buy; 0x54uy; 0x24uy; 0x04uy; 0x56uy; 0x33uy; 0xF6uy; 0x8Duy; 0x4Euy;
       0x01uy; 0x3Buy; 0xD1uy; 0x77uy; 0x04uy; 0x8Buy; 0xC2uy; 0x5Euy; 0xC3uy;
       0x4Auy; 0x8Duy; 0x04uy; 0x31uy; 0x8Duy; 0x31uy; 0x8Buy; 0xC8uy; 0x83uy;
       0xEAuy; 0x01uy; 0x75uy; 0xF4uy; 0x5Euy; 0xC3uy |]

  let isa = ISA.Init Architecture.IntelX86 Endian.Little
  let hdl = BinHandler.Init (isa, binary)
  let ess = BinEssence.Init hdl

  [<TestMethod>]
  member __.``Reaching Definitions Test 1``() =
    let cfg, root = ess.SCFG.GetFunctionCFG 0UL
    let rd = ReachingDefinitions (cfg)
    let ins, outs = rd.Compute (root)
    let solution =
      [ ProgramPoint (0UL, 1); ProgramPoint (4UL, 2); ProgramPoint (5UL, 2)
        ProgramPoint (7UL, 1); ProgramPoint (10UL, 4); ProgramPoint (10UL, 5)
        ProgramPoint (10UL, 6); ProgramPoint (10UL, 7); ProgramPoint (10UL, 8)
        ProgramPoint (10UL, 11) ]
    Assert.IsTrue (ins.[root.GetID ()] = Set.ofList [])
    Assert.IsTrue (outs.[root.GetID ()] = Set.ofList solution)
