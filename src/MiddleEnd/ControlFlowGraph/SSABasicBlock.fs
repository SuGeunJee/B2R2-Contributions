(*
  B2R2 - the Next-Generation Reversing Platform

  Copyright (c) SoftSec Lab. @ KAIST, since 2016

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in all
  copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.
*)

namespace B2R2.MiddleEnd.ControlFlowGraph

open B2R2
open B2R2.BinIR
open B2R2.BinIR.SSA
open B2R2.FrontEnd
open B2R2.FrontEnd.BinLifter
open B2R2.MiddleEnd.BinGraph

[<AutoOpen>]
module private SSABasicBlockHelper =
  let private buildRegVar (hdl: BinHandle) reg =
    let wordSize = hdl.File.ISA.WordSize |> WordSize.toRegType
    RegVar (wordSize, reg, hdl.RegisterFactory.RegIDToString reg)

  let private addReturnValDef (hdl: BinHandle) defs =
    match hdl.File.ISA.Arch with
    | Architecture.EVM -> defs
    | _ ->
      let var = CallingConvention.returnRegister hdl |> buildRegVar hdl
      let rt = hdl.File.ISA.WordSize |> WordSize.toRegType
      let e = Undefined (rt, "ret")
      OutVariableInfo.add hdl var e defs

  let private addStackDef (hdl: BinHandle) fakeBlkInfo defs =
    match hdl.RegisterFactory.StackPointer with
    | Some sp ->
      let rt = hdl.RegisterFactory.RegIDToRegType sp
      let var = buildRegVar hdl sp
      let retAddrSize = RegType.toByteWidth rt |> int64
      let adj = fakeBlkInfo.UnwindingBytes
      let shiftAmount = BitVector.OfInt64 (retAddrSize + adj) rt
      let v1 = Var { Kind = var; Identifier = -1 }
      let v2 = Num shiftAmount
      let e = BinOp (BinOpType.ADD, rt, v1, v2)
      OutVariableInfo.add hdl var e defs
    | None -> defs

  let private addMemDef hdl defs =
    let e = Var { Kind = MemVar; Identifier = - 1 }
    OutVariableInfo.add hdl MemVar e defs

  let computeDefinedVars hdl fakeBlkInfo =
    if fakeBlkInfo.IsPLT then Map.empty |> addReturnValDef hdl
    else fakeBlkInfo.OutVariableInfo
    |> addMemDef hdl
    |> addStackDef hdl fakeBlkInfo

  let computeNextPPoint (ppoint: ProgramPoint) = function
    | Def (v, Num bv) ->
      match v.Kind with
      | PCVar _ -> ProgramPoint (BitVector.ToUInt64 bv, 0)
      | _ -> ProgramPoint.Next ppoint
    | _ -> ProgramPoint.Next ppoint

  let private addInOutMemVars inVars outVars =
    let inVar = { Kind = MemVar; Identifier = -1 }
    let outVar = { Kind = MemVar; Identifier = -1 }
    inVar :: inVars, outVar :: outVars

  let private postprocessStmtForEVM = function
    | ExternalCall ((BinOp (BinOpType.APP, _, FuncName "calldatacopy", _)) as e,
                    _, _) ->
      let inVars, outVars = addInOutMemVars [] []
      ExternalCall (e, inVars, outVars)
    | stmt -> stmt

  let private postprocessOthers stmt = stmt

  let postprocessStmt arch s =
    match arch with
    | Architecture.EVM -> postprocessStmtForEVM s
    | _ -> postprocessOthers s

/// SSA statement information.
type LiftedSSAStmt = ProgramPoint * Stmt

/// Basic block type for an SSA-based CFG (SSACFG). It holds an array of
/// LiftedSSAStmts (ProgramPoint * Stmt).
[<AbstractClass>]
type SSABasicBlock (pp, instrs: LiftedInstruction []) =
  inherit BasicBlock (pp)

  let mutable idom: IVertex<SSABasicBlock> option = None
  let mutable frontier: IVertex<SSABasicBlock> list = []

  override __.Range =
    if Array.isEmpty instrs then Utils.impossible () else ()
    let last = instrs[instrs.Length - 1].Instruction
    AddrRange (pp.Address, last.Address + uint64 last.Length - 1UL)

  override __.IsFakeBlock () = Array.isEmpty instrs

  override __.ToVisualBlock () =
    __.LiftedSSAStmts
    |> Array.map (fun (_, stmt) ->
      [| { AsmWordKind = AsmWordKind.String
           AsmWordValue = Pp.stmtToString stmt } |])

  /// Return the corresponding LiftedInstruction array.
  member __.LiftedInstructions with get () = instrs

  /// Get the last SSA statement of the bblock.
  member __.GetLastStmt () =
    snd __.LiftedSSAStmts[__.LiftedSSAStmts.Length - 1]

  /// Immediate dominator of this block.
  member __.ImmDominator with get() = idom and set(d) = idom <- d

  /// Dominance frontier of this block.
  member __.DomFrontier with get() = frontier and set(f) = frontier <- f

  /// Prepend a Phi node to this SSA basic block.
  member __.PrependPhi varKind count =
    let var = { Kind = varKind; Identifier = -1 }
    let ppoint = ProgramPoint.GetFake ()
    __.LiftedSSAStmts <-
      Array.append [| ppoint, Phi (var, Array.zeroCreate count) |]
                   __.LiftedSSAStmts

  /// Update program points. This must be called after updating SSA stmts.
  member __.UpdatePPoints () =
    __.LiftedSSAStmts
    |> Array.foldi (fun ppoint idx (_, stmt) ->
      let ppoint' = computeNextPPoint ppoint stmt
      __.LiftedSSAStmts[idx] <- (ppoint', stmt)
      ppoint') pp
    |> ignore

  /// Return the array of LiftedSSAStmts.
  abstract LiftedSSAStmts: LiftedSSAStmt[] with get, set

  /// Return the corresponding fake block information. This is only valid for a
  /// fake SSABasicBlock.
  abstract FakeBlockInfo: FakeBlockInfo with get, set

/// Regular SSABasicBlock with regular instructions.
type RegularSSABasicBlock (hdl: BinHandle, pp, instrs) =
  inherit SSABasicBlock (pp, instrs)

  let mutable stmts: LiftedSSAStmt[] =
    (instrs: LiftedInstruction[])
    |> Array.collect (fun i ->
      let wordSize = i.Instruction.WordSize |> WordSize.toRegType
      let stmts = i.Stmts
      let address = i.Instruction.Address
      let arch = hdl.File.ISA.Arch
      AST.translateStmts wordSize address (postprocessStmt arch) stmts)
    |> Array.map (fun s -> ProgramPoint.GetFake (), s)

  override __.LiftedSSAStmts with get() = stmts and set(s) = stmts <- s

  override __.FakeBlockInfo
    with get() = Utils.impossible () and set(_) = Utils.impossible ()

  override __.ToString () =
    $"SSABBLK({__.PPoint.Address:x})"

/// Fake SSABasicBlock, which may or may not hold a function summary with
/// ReturnVal expressions.
type FakeSSABasicBlock (hdl, pp, retPoint: ProgramPoint, fakeBlkInfo) =
  inherit SSABasicBlock (pp, [||])

  let mutable stmts: LiftedSSAStmt [] =
    if fakeBlkInfo.IsTailCall then [||]
    else
      let stmts = (* For a fake block, we check which var can be defined. *)
        computeDefinedVars hdl fakeBlkInfo
        |> Seq.map (fun (KeyValue (kind, e)) ->
          let dst = { Kind = kind; Identifier = -1 }
          let src = e
          Def (dst, ReturnVal (pp.Address, retPoint.Address, src)))
        |> Seq.toArray
      let wordSize = hdl.File.ISA.WordSize |> WordSize.toRegType
      let fallThrough = BitVector.OfUInt64 retPoint.Address wordSize
      let jmpToFallThrough = Jmp (InterJmp (Num fallThrough))
      Array.append stmts [| jmpToFallThrough |]
      |> Array.map (fun s -> ProgramPoint.GetFake (), s)

  let mutable fakeBlkInfo = fakeBlkInfo

  override __.LiftedSSAStmts with get() = stmts and set(s) = stmts <- s

  override __.FakeBlockInfo
    with get() = fakeBlkInfo and set(f) = fakeBlkInfo <- f

  override __.ToString () =
    "SSABBLK(Dummy;" + pp.ToString () + ";" + retPoint.ToString () + ")"

/// SSACFG's vertex.
type SSAVertex = IVertex<SSABasicBlock>

[<RequireQualifiedAccess>]
module SSABasicBlock =
  let initRegular hdl pp instrs =
    RegularSSABasicBlock (hdl, pp, instrs) :> SSABasicBlock

  let initFake hdl pp retPoint fakeBlkInfo =
    FakeSSABasicBlock (hdl, pp, retPoint, fakeBlkInfo) :> SSABasicBlock
