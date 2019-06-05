(*
  B2R2 - the Next-Generation Reversing Platform

  Author: Sang Kil Cha <sangkilc@kaist.ac.kr>
          DongYeop Oh <oh51dy@kaist.ac.kr>
          Jaeseung Choi <jschoi17@kaist.ac.kr>

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

module B2R2.BinGraph.DisasHeuristic

open B2R2
open B2R2.BinIR.LowUIR.Eval
open B2R2.FrontEnd

/// An arbitrary stack value for applying heuristics.
let stackAddr t = Def (BitVector.ofInt32 0x1000000 t)

let getStackPtrRegID = function
  | Arch.IntelX86 -> Intel.Register.ESP |> Intel.Register.toRegID
  | Arch.IntelX64 -> Intel.Register.RSP |> Intel.Register.toRegID
  | Arch.ARMv7 -> ARM32.Register.SP |> ARM32.Register.toRegID
  | _ -> failwith "Not supported arch."

let initStateForLibcStart handle startAddr =
  let isa = handle.ISA
  // FIXME
  let sp = getStackPtrRegID isa.Arch
  let vars =
    match isa.Arch with
    | Arch.IntelX86 ->
      /// XXX: This is another heuristic
      let ebx = Intel.Register.EBX |> Intel.Register.toRegID
      Map.add sp (stackAddr 32<rt>) Map.empty
      |> Map.add ebx (Def (BitVector.ofInt32 (int startAddr) 32<rt>))
    | Arch.IntelX64 -> Map.add sp (stackAddr 64<rt>) Map.empty
    | Arch.ARMv7 -> Map.add sp (stackAddr 32<rt>) Map.empty
    | _ -> failwith "Not supported arch."
  { PC = startAddr
    BlockEnd = false
    Vars = vars
    TmpVars = Map.empty
    Mems = Map.empty
    NextStmtIdx = 0
    LblMap = Map.empty }

let imageLoader hdl addr =
  match hdl.ISA.Arch with
  | Arch.IntelX86 ->
    let fileInfo = hdl.FileInfo
    if fileInfo.IsValidAddr addr then
      let v = BinHandler.ReadBytes (hdl, addr, 1)
      Some <| v.[0]
    else None
  | _ -> None

let intel32LibcParams hdl state =
  let f ptr =
    try
      loadMem (imageLoader hdl) state.Mems Endian.Little ptr 32<rt>
      |> BitVector.toUInt64 |> Some
    with InvalidMemException -> None
  /// 1st, 4th, and 5th parameter of _libc_start_main
  let stackPtrReg = Intel.Register.ESP |> Intel.Register.toRegID
  match Map.tryFind stackPtrReg state.Vars with
  | Some (Def esp) ->
    let esp = BitVector.toUInt64 esp
    List.choose f [ esp; esp + 12UL; esp + 16UL ]
  | _ -> []

let intel64LibcParams state =
  let f var =
    match Map.tryFind (Intel.Register.toRegID var) state.Vars with
    | Some (Def addr) -> Some (BitVector.toUInt64 addr)
    | _ -> None
  /// 1st, 4th, and 5th parameter of _libc_start_main
  List.choose f [ Intel.Register.RDI; Intel.Register.RCX; Intel.Register.R8 ]

let arm32LibcParams hdl state =
  let f var =
    match Map.tryFind (ARM32.Register.toRegID var) state.Vars with
    | Some (Def addr) -> Some (BitVector.toUInt64 addr)
    | _ -> None
  let g ptr =
    try
      loadMem (imageLoader hdl) state.Mems Endian.Little ptr 32<rt>
      |> BitVector.toUInt64 |> Some
    with InvalidMemException -> None
  /// XXX: This only chooses init and main
  List.choose f [ ARM32.Register.R0; ARM32.Register.R3 ]

let getLibcStartMainParams hdl state =
  match hdl.ISA.Arch with
  | Arch.IntelX86 -> intel32LibcParams hdl state
  | Arch.IntelX64 -> intel64LibcParams state
  | Arch.ARMv7 -> arm32LibcParams hdl state
  | _ -> failwith "Not supported arch."

let isLibcStartMain hdl addr =
  let found, name = hdl.FileInfo.TryFindFunctionSymbolName addr
  found && name = "__libc_start_main" &&
  FileFormat.isELF hdl.FileInfo.FileFormat

let rec collectLibcStartInstrs (builder: CFGBuilder) curAddr endAddr acc =
  let ins = builder.GetInstr curAddr
  let nextAddr = curAddr + uint64 ins.Length
  if nextAddr = endAddr then List.rev (ins :: acc)
  else collectLibcStartInstrs builder nextAddr endAddr (ins :: acc)

/// Evaluate instructions that prepare call to libc_start_main. Ignore any
/// expressions that involve unknown values.
let evalLibcStartInstrs hdl state ins =
  let stmts = BinHandler.LiftInstr hdl ins
  Array.fold (fun state stmt ->
    try evalStmt (imageLoader hdl) state emptyCallBack stmt with
    | UnknownVarException (* Simply ignore exceptions *)
    | InvalidMemException -> state
  ) state stmts

/// Retrieve function pointer arguments of libc_start_main() function call.
let recoverLibcPointers hdl sAddr (callInstr: Instruction) builder =
  let callAddr = callInstr.Address
  let instrs = collectLibcStartInstrs builder sAddr callAddr []
  let initState = initStateForLibcStart hdl sAddr
  let callState = List.fold (evalLibcStartInstrs hdl) initState instrs
  getLibcStartMainParams hdl callState
  |> List.filter (builder.IsInteresting hdl)
