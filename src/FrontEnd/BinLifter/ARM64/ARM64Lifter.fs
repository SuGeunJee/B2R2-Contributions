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

module internal B2R2.FrontEnd.BinLifter.ARM64.Lifter

open B2R2
open B2R2.BinIR
open B2R2.BinIR.LowUIR
open B2R2.BinIR.LowUIR.AST.InfixOp
open B2R2.FrontEnd.BinLifter
open B2R2.FrontEnd.BinLifter.LiftingOperators
open B2R2.FrontEnd.BinLifter.LiftingUtils
open B2R2.FrontEnd.BinLifter.ARM64
open B2R2.FrontEnd.BinLifter.ARM64.LiftingUtils

/// A module for all AArch64-IR translation functions
let sideEffects insLen ctxt name =
  let ir = !*ctxt
  !<ir insLen
  !!ir (AST.sideEffect name)
  !>ir insLen

let adc ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2 = transThreeOprs ins ctxt addr
  let c = AST.zext ins.OprSize (getRegVar ctxt R.C)
  !<ir insLen
  let result, _ = addWithCarry src1 src2 c ins.OprSize
  !!ir (dst := result)
  !>ir insLen

let add ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  match ins.Operands with
  | ThreeOperands (o1, o2, o3) when isSIMDVector o1 (* SIMD Vector *) ->
    let dstB, dstA = transOprToExpr128 ins ctxt addr o1
    let src1B, src1A = transOprToExpr128 ins ctxt addr o2
    let src2B, src2A = transOprToExpr128 ins ctxt addr o3
    let elements, eSize = getElemNumAndSize ins.OprSize (getSIMDReg o1)
    let elements = elements / 2
    let s1ATmps = Array.init elements (fun _ -> !+ir eSize)
    let s1BTmps = Array.init elements (fun _ -> !+ir eSize)
    let s2ATmps = Array.init elements (fun _ -> !+ir eSize)
    let s2BTmps = Array.init elements (fun _ -> !+ir eSize)
    let resATmps = Array.init elements (fun _ -> !+ir eSize)
    let resBTmps = Array.init elements (fun _ -> !+ir eSize)
    let amt = RegType.toBitWidth eSize
    for i in 0 .. elements - 1 do
      !!ir (s1ATmps[i] := AST.extract src1A eSize (i * amt))
      !!ir (s1BTmps[i] := AST.extract src1B eSize (i * amt))
      !!ir (s2ATmps[i] := AST.extract src2A eSize (i * amt))
      !!ir (s2BTmps[i] := AST.extract src2B eSize (i * amt))
      !!ir (resATmps[i] := s1ATmps[i] .+ s2ATmps[i])
      !!ir (resBTmps[i] := s1BTmps[i] .+ s2BTmps[i])
    done
    !!ir (dstA := AST.concatArr resATmps)
    !!ir (dstB := AST.concatArr resBTmps)
  | ThreeOperands _ (* SIMD Scalar *) ->
    let dst, src1, src2 = transThreeOprs ins ctxt addr
    !!ir (dst := src1 .+ src2)
  | FourOperands _ (* Arithmetic *) ->
    let dst, s1, s2 = transFourOprsWithBarrelShift ins ctxt addr
    let result, _ = addWithCarry s1 s2 (AST.num0 ins.OprSize) ins.OprSize
    !!ir (dst := result)
  | _ -> raise InvalidOperandException
  !>ir insLen

let adds ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2 = transOprToExprOfADDS ins ctxt addr
  let oSz = ins.OprSize
  !<ir insLen
  let result, (n, z, c, v) = addWithCarry src1 src2 (AST.num0 oSz) oSz
  !!ir (getRegVar ctxt R.N := n)
  !!ir (getRegVar ctxt R.Z := z)
  !!ir (getRegVar ctxt R.C := c)
  !!ir (getRegVar ctxt R.V := v)
  !!ir (dst := result)
  !>ir insLen

let adr ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, label = transTwoOprs ins ctxt addr
  !<ir insLen
  !!ir (dst := getPC ctxt .+ label)
  !>ir insLen

let adrp ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, lbl = transTwoOprs ins ctxt addr
  !<ir insLen
  !!ir (dst := (getPC ctxt .& numI64 0xfffffffffffff000L 64<rt>) .+ lbl)
  !>ir insLen

let logAnd ins insLen ctxt addr = (* AND *)
  let ir = !*ctxt
  !<ir insLen
  match ins.OprSize with
  | 128<rt> ->
    let struct (dst, src1, src2) = getThreeOprs ins
    let dstB, dstA = transOprToExpr128 ins ctxt addr dst
    let src1B, src1A = transOprToExpr128 ins ctxt addr src1
    let src2B, src2A = transOprToExpr128 ins ctxt addr src2
    !!ir (dstA := src1A .& src2A)
    !!ir (dstB := src1B .& src2B)
  | _ ->
    let dst, src1, src2 = transOprToExprOfAND ins ctxt addr
    !!ir (dst := src1 .& src2)
  !>ir insLen

let asrv ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2 = transThreeOprs ins ctxt addr
  let amount = src2 .% oprSzToExpr ins.OprSize
  !<ir insLen
  !!ir (dst := shiftReg src1 amount ins.OprSize SRTypeASR)
  !>ir insLen

let ands ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2 = transOprToExprOfANDS ins ctxt addr
  !<ir insLen
  !!ir (dst := src1 .& src2)
  !>ir insLen

let b ins insLen ctxt addr =
  let ir = !*ctxt
  let label = transOneOpr ins ctxt addr
  let pc = getPC ctxt
  !<ir insLen
  !!ir (AST.interjmp (pc .+ label) InterJmpKind.Base)
  !>ir insLen

let bCond ins insLen ctxt addr cond =
  let ir = !*ctxt
  let label = transOneOpr ins ctxt addr
  let pc = getPC ctxt
  let fall = pc .+ numU32 insLen 64<rt>
  !<ir insLen
  !!ir (AST.intercjmp (conditionHolds ctxt cond) (pc .+ label) fall)
  !>ir insLen

let bfm ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  let dst, src, imms, immr = transOprToExprOfBFM ins ctxt addr
  let oSz = ins.OprSize
  let wmask, tmask = !+ir oSz, !+ir oSz
  let immN = if ins.OprSize = 64<rt> then AST.num1 8<rt> else AST.num0 8<rt>
  decodeBitMasksForIR wmask tmask immN imms immr oSz ir
  let width = oprSzToExpr ins.OprSize
  let bot = !+ir ins.OprSize
  !!ir (bot := (dst .& AST.not wmask) .| (ror src immr width .& wmask))
  !!ir (dst := (dst .& AST.not tmask) .| (bot .& tmask))
  !>ir insLen

let bic ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  match ins.Operands with
  | TwoOperands _ -> (* FIXME: SIMD Register *)
    !!ir (AST.sideEffect UnsupportedFP)
  | ThreeOperands _ -> (* FIXME: SIMD Register *)
    !!ir (AST.sideEffect UnsupportedFP)
  | _ ->
    let dst, src1, src2 = transFourOprsWithBarrelShift ins ctxt addr
    !!ir (dst := src1 .& AST.not src2)
  !>ir insLen

let bics ins insLen ctxt addr =
  let dst, src1, src2 = transFourOprsWithBarrelShift ins ctxt addr
  let z = if ins.OprSize = 64<rt> then AST.num0 64<rt> else AST.num0 32<rt>
  let ir = !*ctxt
  !<ir insLen
  !!ir (dst := src1 .& AST.not src2)
  !!ir (getRegVar ctxt R.N := AST.xthi 1<rt> dst)
  !!ir (getRegVar ctxt R.Z := if dst = z then AST.b1 else AST.b0)
  !!ir (getRegVar ctxt R.C := AST.b0)
  !!ir (getRegVar ctxt R.V := AST.b0)
  !>ir insLen

let bl ins insLen ctxt addr =
  let ir = !*ctxt
  let label = transOneOpr ins ctxt addr
  let pc = getPC ctxt
  !<ir insLen
  !!ir (getRegVar ctxt R.X30 := pc .+ numI64 4L ins.OprSize)
  // FIXME: BranchTo (BType_CALL)
  !!ir (AST.interjmp (pc .+ label) InterJmpKind.IsCall)
  !>ir insLen

let blr ins insLen ctxt addr =
  let ir = !*ctxt
  let src = transOneOpr ins ctxt addr
  let pc = getPC ctxt
  !<ir insLen
  !!ir (getRegVar ctxt R.X30 := pc .+ numI64 4L ins.OprSize)
  // FIXME: BranchTo (BranchType_CALL)
  !!ir (AST.interjmp src InterJmpKind.IsCall)
  !>ir insLen

let br ins insLen ctxt addr =
  let ir = !*ctxt
  let dst = transOneOpr ins ctxt addr
  !<ir insLen
  // FIXME: BranchTo (BType_JMP)
  !!ir (AST.interjmp dst InterJmpKind.Base)
  !>ir insLen

let cbnz ins insLen ctxt addr =
  let ir = !*ctxt
  let test, label = transTwoOprs ins ctxt addr
  let pc = getPC ctxt
  let fall = pc .+ numU32 insLen 64<rt>
  !<ir insLen
  !!ir (AST.intercjmp (test != AST.num0 ins.OprSize) (pc .+ label) fall)
  !>ir insLen

let cbz ins insLen ctxt addr =
  let ir = !*ctxt
  let test, label = transTwoOprs ins ctxt addr
  let pc = getPC ctxt
  let fall = pc .+ numU32 insLen 64<rt>
  !<ir insLen
  !!ir (AST.intercjmp (test == AST.num0 ins.OprSize) (pc .+ label) fall)
  !>ir insLen

let ccmn ins insLen ctxt addr =
  let ir = !*ctxt
  let src, imm, nzcv, cond = transOprToExprOfCCMN ins ctxt addr
  !<ir insLen
  let oSz = ins.OprSize
  let tCond = !+ir 1<rt>
  !!ir (tCond := conditionHolds ctxt cond)
  let _, (n, z, c, v) = addWithCarry src imm (AST.num0 oSz) oSz
  !!ir (getRegVar ctxt R.N := (AST.ite tCond n (AST.extract nzcv 1<rt> 3)))
  !!ir (getRegVar ctxt R.Z := (AST.ite tCond z (AST.extract nzcv 1<rt> 2)))
  !!ir (getRegVar ctxt R.C := (AST.ite tCond c (AST.extract nzcv 1<rt> 1)))
  !!ir (getRegVar ctxt R.V := (AST.ite tCond v (AST.xtlo 1<rt> nzcv)))
  !>ir insLen

let ccmp ins insLen ctxt addr =
  let ir = !*ctxt
  let src, imm, nzcv, cond = transOprToExprOfCCMP ins ctxt addr
  !<ir insLen
  let tCond = conditionHolds ctxt cond
  let oSz = ins.OprSize
  let _, (n, z, c, v) = addWithCarry src (AST.not imm) (AST.num1 oSz) oSz
  !!ir (getRegVar ctxt R.N := (AST.ite tCond n (AST.extract nzcv 1<rt> 3)))
  !!ir (getRegVar ctxt R.Z := (AST.ite tCond z (AST.extract nzcv 1<rt> 2)))
  !!ir (getRegVar ctxt R.C := (AST.ite tCond c (AST.extract nzcv 1<rt> 1)))
  !!ir (getRegVar ctxt R.V := (AST.ite tCond v (AST.xtlo 1<rt> nzcv)))
  !>ir insLen

let clz ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src = transTwoOprs ins ctxt addr
  !<ir insLen
  countLeadingZeroBitsForIR dst src ins.OprSize ir
  !>ir insLen

let cmp ins insLen ctxt addr =
  let ir = !*ctxt
  let src, imm = transOprToExprOfCMP ins ctxt addr
  let oSz = ins.OprSize
  let dst = getRegVar ctxt (if oSz = 64<rt> then R.XZR else R.WZR)
  !<ir insLen
  let result, (n, z, c, v) = addWithCarry src (AST.not imm) (AST.num1 oSz) oSz
  !!ir (getRegVar ctxt R.N := n)
  !!ir (getRegVar ctxt R.Z := z)
  !!ir (getRegVar ctxt R.C := c)
  !!ir (getRegVar ctxt R.V := v)
  !>ir insLen

let csel ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2, cond = transOprToExprOfCSEL ins ctxt addr
  !<ir insLen
  !!ir (dst := AST.ite (conditionHolds ctxt cond) src1 src2)
  !>ir insLen

let csinc ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, s1, s2, cond = transOprToExprOfCSINC ins ctxt addr
  !<ir insLen
  let cond = conditionHolds ctxt cond
  !!ir (dst := AST.ite cond s1 (s2 .+ AST.num1 ins.OprSize))
  !>ir insLen

let csinv ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2, cond = transOprToExprOfCSINV ins ctxt addr
  !<ir insLen
  !!ir (dst := AST.ite (conditionHolds ctxt cond) src1 (AST.not src2))
  !>ir insLen

let csneg ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2, cond = transOprToExprOfCSNEG ins ctxt addr
  !<ir insLen
  let cond = conditionHolds ctxt cond
  !!ir (dst := AST.ite cond src1 (AST.not src2 .+ AST.num1 ins.OprSize))
  !>ir insLen

let eor ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  match ins.Operands with
  | ThreeOperands (o1, o2, o3) when isSIMDVector o1 ->
    let dstB, dstA = transOprToExpr128 ins ctxt addr o1
    let src1B, src1A = transOprToExpr128 ins ctxt addr o2
    let src2B, src2A = transOprToExpr128 ins ctxt addr o3
    let struct (opr2, opr3) = tmpVars2 ir 64<rt>
    !!ir (opr2 := AST.num0 64<rt>)
    !!ir (opr3 := numI64 -1 64<rt>)
    !!ir (dstA := src2A <+> ((opr2 <+> src1A) .& opr3))
    if ins.OprSize = 64<rt> then ()
    else !!ir (dstB := src2B <+> ((opr2 <+> src1B) .& opr3))
  | _ ->
    let dst, src1, src2 = transOprToExprOfEOR ins ctxt addr
    !!ir (dst := src1 <+> src2)
  !>ir insLen

let extr ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2, lsb = transOprToExprOfEXTR ins ctxt addr
  let oSz = ins.OprSize
  !<ir insLen
  if oSz = 32<rt> then
    let con = !+ir 64<rt>
    !!ir (con := AST.concat src1 src2)
    let mask = numI32 0xFFFFFFFF 64<rt>
    !!ir (dst := AST.xtlo 32<rt> ((con >> (AST.zext 64<rt> lsb)) .& mask))
  elif oSz = 64<rt> then
    let lsb =
      match ins.Operands with
      | ThreeOperands (_, _, LSB shift) -> int32 shift
      | FourOperands (_, _, _, LSB lsb) -> int32 lsb
      | _ -> raise InvalidOperandException
    if lsb = 0 then !!ir (dst := src2)
    else
      let leftAmt = numI32 (64 - lsb) 64<rt>
      !!ir (dst := (src1 << leftAmt) .| (src2 >> (numI32 lsb 64<rt>)))
  else raise InvalidOperandSizeException
  !>ir insLen

let ldp ins insLen ctxt addr =
  let ir = !*ctxt
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  let address = !+ir 64<rt>
  let dByte = numI32 (RegType.toByteWidth ins.OprSize) 64<rt>
  !<ir insLen
  match ins.OprSize with
  | 128<rt> ->
    let struct (src1, src2, src3) = getThreeOprs ins
    let src1B, src1A = transOprToExpr128 ins ctxt addr src1
    let src2B, src2A = transOprToExpr128 ins ctxt addr src2
    let bReg, offset = transOprToExpr ins ctxt addr src3 |> separateMemExpr
    let n8 = numI32 8 64<rt>
    !!ir (address := bReg)
    !!ir (address := if isPostIndex then address else address .+ offset)
    !!ir (src1A := AST.loadLE 64<rt> address)
    !!ir (src1B := AST.loadLE 64<rt> address .+ n8)
    !!ir (src2A := AST.loadLE 64<rt> (address .+ dByte))
    !!ir (src2B := AST.loadLE 64<rt> (address .+ dByte .+ n8))
    if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
    else if isWBack then !!ir (bReg := address) else ()
  | _ ->
    let src1, src2, (bReg, offset) = transThreeOprsSepMem ins ctxt addr
    !!ir (address := bReg)
    !!ir (address := if isPostIndex then address else address .+ offset)
    !!ir (src1 := AST.loadLE ins.OprSize address)
    !!ir (src2 := AST.loadLE ins.OprSize (address .+ dByte))
    if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
    else if isWBack then !!ir (bReg := address) else ()
  !>ir insLen

let ldpsw ins insLen ctxt addr =
  let ir = !*ctxt
  let src1, src2, (bReg, offset) = transThreeOprsSepMem ins ctxt addr
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  let address = !+ir 64<rt>
  let data1 = !+ir 32<rt>
  let data2 = !+ir 32<rt>
  !<ir insLen
  !!ir (address := bReg)
  !!ir (address := if isPostIndex then address else address .+ offset)
  !!ir (data1 := AST.loadLE 32<rt> address)
  !!ir (data2 := AST.loadLE 32<rt> (address .+ numI32 4 64<rt>))
  !!ir (src1 := AST.sext 64<rt> data1)
  !!ir (src2 := AST.sext 64<rt> data2)
  if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
  else if isWBack then !!ir (bReg := address) else ()
  !>ir insLen

let ldr ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  match ins.Operands with
  | TwoOperands (o1, Memory (LiteralMode o2)) -> (* LDR (literal) *)
    let offset = transOprToExpr ins ctxt addr (Memory (LiteralMode o2))
    let address = !+ir 64<rt>
    match ins.OprSize with
    | 128<rt> ->
      let dstB, dstA = transOprToExpr128 ins ctxt addr o1
      !!ir (address := getPC ctxt .+ offset)
      !!ir (dstA := AST.loadLE 64<rt> address)
      !!ir (dstB := AST.loadLE 64<rt> (address .+ (numI32 8 64<rt>)))
    | _ ->
      let dst = transOprToExpr ins ctxt addr o1
      let data = !+ir ins.OprSize
      !!ir (address := getPC ctxt .+ offset)
      !!ir (data := AST.loadLE ins.OprSize address)
      !!ir (dst := data)
  | TwoOperands (o1, o2) ->
    let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
    let address = !+ir 64<rt>
    match ins.OprSize with
    | 128<rt> ->
      let dstB, dstA = transOprToExpr128 ins ctxt addr o1
      let bReg, offset = transOprToExpr ins ctxt addr o2 |> separateMemExpr
      !!ir (address := bReg)
      !!ir (address := if isPostIndex then address else address .+ offset)
      !!ir (dstA := AST.loadLE 64<rt> address)
      !!ir (dstB := AST.loadLE 64<rt> (address .+ (numI32 8 64<rt>)))
      if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
      else if isWBack then !!ir (bReg := address) else ()
    | _ ->
      let dst = transOprToExpr ins ctxt addr o1
      let bReg, offset = transOprToExpr ins ctxt addr o2 |> separateMemExpr
      let data = !+ir ins.OprSize
      !!ir (address := bReg)
      !!ir (address := if isPostIndex then address else address .+ offset)
      !!ir (data := AST.loadLE ins.OprSize address)
      !!ir (dst := AST.zext ins.OprSize data)
      if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
      else if isWBack then !!ir (bReg := address) else ()
  | _ -> raise InvalidOperandException
  !>ir insLen

let ldrb ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  let address = !+ir 64<rt>
  let data = !+ir 8<rt>
  !<ir insLen
  !!ir (address := bReg)
  !!ir (address := if isPostIndex then address else address .+ offset)
  !!ir (data := AST.loadLE 8<rt> address)
  !!ir (dst := AST.zext 32<rt> data)
  if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
  else if isWBack then !!ir (bReg := address) else ()
  !>ir insLen

let ldrh ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  let address = !+ir 64<rt>
  let data = !+ir 16<rt>
  !<ir insLen
  !!ir (address := bReg)
  !!ir (address := if isPostIndex then address else address .+ offset)
  !!ir (data := AST.loadLE 16<rt> address)
  !!ir (dst := AST.zext 32<rt> data)
  if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
  else if isWBack then !!ir (bReg := address) else ()
  !>ir insLen

let ldrsb ins insLen ctxt addr =
  let dst, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  let ir = !*ctxt
  let address = !+ir 64<rt>
  let data = !+ir 8<rt>
  !<ir insLen
  !!ir (address := bReg)
  !!ir (address := if isPostIndex then address else address .+ offset)
  !!ir (data := AST.loadLE 8<rt> address)
  !!ir (dst := AST.sext ins.OprSize data)
  if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
  else if isWBack then !!ir (bReg := address) else ()
  !>ir insLen

let ldrsh ins insLen ctxt addr =
  let dst, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  let ir = !*ctxt
  let address = !+ir 64<rt>
  let data = !+ir 16<rt>
  !<ir insLen
  !!ir (address := bReg)
  !!ir (address := if isPostIndex then address else address .+ offset)
  !!ir (data := AST.loadLE 16<rt> address)
  !!ir (dst := AST.sext ins.OprSize data)
  if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
  else if isWBack then !!ir (bReg := address) else ()
  !>ir insLen

let ldrsw ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  transOprToExprOfLDRSW ins ctxt addr ir
  !>ir insLen

let ldtr ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let address = !+ir 64<rt>
  let data = !+ir ins.OprSize
  !<ir insLen
  !!ir (address := bReg .+ offset)
  !!ir (data := AST.loadLE ins.OprSize address)
  !!ir (dst := AST.zext ins.OprSize data)
  !>ir insLen

let ldur ins insLen ctxt addr =
  let ir = !*ctxt
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  let address = !+ir 64<rt>
  let data = !+ir ins.OprSize
  !<ir insLen
  match ins.OprSize with
  | 128<rt> ->
    let struct (dst, src) = getTwoOprs ins
    let dstB, dstA = transOprToExpr128 ins ctxt addr dst
    let bReg, offset = transOprToExpr ins ctxt addr src |> separateMemExpr
    !!ir (address := bReg)
    !!ir (address := if isPostIndex then address else address .+ offset)
    !!ir (dstA := AST.loadLE 64<rt> address)
    !!ir (dstB := AST.loadLE 64<rt> (address .+ (numI32 8 64<rt>)))
    if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
    else if isWBack then !!ir (bReg := address) else ()
  | _ ->
    let dst, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
    !!ir (address := bReg)
    !!ir (address := if isPostIndex then address else address .+ offset)
    !!ir (data := AST.loadLE ins.OprSize address)
    !!ir (dst := data)
    if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
    else if isWBack then !!ir (bReg := address) else ()
  !>ir insLen

let ldurb ins insLen ctxt addr =
  let ir = !*ctxt
  let src, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let address = !+ir 64<rt>
  let data = !+ir 8<rt>
  !<ir insLen
  !!ir (address := bReg)
  !!ir (address := address .+ offset)
  !!ir (data := AST.loadLE 8<rt> address)
  !!ir (src := AST.zext 32<rt> data)
  !>ir insLen

let ldurh ins insLen ctxt addr =
  let ir = !*ctxt
  let src, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let address = !+ir 64<rt>
  let data = !+ir 16<rt>
  !<ir insLen
  !!ir (address := bReg)
  !!ir (address := address .+ offset)
  !!ir (data := AST.loadLE 16<rt> address)
  !!ir (src := AST.zext 32<rt> data)
  !>ir insLen

let ldursb ins insLen ctxt addr =
  let dst, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  let ir = !*ctxt
  let address = !+ir 64<rt>
  let data = !+ir 8<rt>
  !<ir insLen
  !!ir (address := bReg.+ offset)
  !!ir (data := AST.loadLE 8<rt> address)
  !!ir (dst := AST.sext ins.OprSize data)
  !>ir insLen

let ldursh ins insLen ctxt addr =
  let dst, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  let ir = !*ctxt
  let address = !+ir 64<rt>
  let data = !+ir 16<rt>
  !<ir insLen
  !!ir (address := bReg.+ offset)
  !!ir (data := AST.loadLE 16<rt> address)
  !!ir (dst := AST.sext ins.OprSize data)
  !>ir insLen

let ldursw ins insLen ctxt addr =
  let dst, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let ir = !*ctxt
  let address = !+ir 64<rt>
  let data = !+ir 32<rt>
  !<ir insLen
  !!ir (address := bReg)
  !!ir (address := address .+ offset)
  !!ir (data := AST.loadLE 32<rt> address)
  !!ir (dst := AST.sext 64<rt> data)
  !>ir insLen

let lslv ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2 = transThreeOprs ins ctxt addr
  let oprSz = ins.OprSize
  let dataSize = numI32 (RegType.toBitWidth ins.OprSize) oprSz
  !<ir insLen
  !!ir (dst := shiftReg src1 (src2 .% dataSize) oprSz SRTypeLSL)
  !>ir insLen

let lsrv ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2 = transThreeOprs ins ctxt addr
  let oprSz = ins.OprSize
  let dataSize = numI32 (RegType.toBitWidth oprSz) oprSz
  !<ir insLen
  !!ir (dst := shiftReg src1 (src2 .% dataSize) oprSz SRTypeLSR)
  !>ir insLen

let madd ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  match ins.Operands with
  | ThreeOperands (SIMDOpr _, SIMDOpr _, SIMDOpr _) ->
    !!ir (AST.sideEffect UnsupportedFP)
  | _ ->
    let dst, src1, src2, src3 = transOprToExprOfMADD ins ctxt addr
    !!ir (dst := src3 .+ (src1 .* src2))
  !>ir insLen

let mov ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  match ins.Operands with
  | TwoOperands (_, SIMDOpr _) -> !!ir (AST.sideEffect UnsupportedFP)
  | _ ->
    let dst, src = transOprToExprOfMOV ins ctxt addr
    if ins.Opcode = Opcode.MOVN then !!ir (dst := AST.not src)
    else !!ir (dst := src)
  !>ir insLen

let mrs ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src = transTwoOprs ins ctxt addr
  !<ir insLen
  !!ir (dst := src) (* FIXME: AArch64.SysRegRead *)
  !>ir insLen

let msub ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2, src3 = transOprToExprOfMSUB ins ctxt addr
  !<ir insLen
  !!ir (dst := src3 .- (src1 .* src2))
  !>ir insLen

let nop insLen ctxt =
  let ir = !*ctxt
  !<ir insLen
  !>ir insLen

let orn ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  match ins.Operands with
  | TwoOperands _ -> !!ir (AST.sideEffect UnsupportedFP)
  | ThreeOperands (SIMDOpr _, SIMDOpr _, SIMDOpr _) ->
    !!ir (AST.sideEffect UnsupportedFP)
  | _ ->
    let dst, src1, src2 = transOprToExprOfORN ins ctxt addr
    !!ir (dst := src1 .| AST.not src2)
  !>ir insLen

let orr ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  match ins.Operands with
  | TwoOperands (SIMDOpr _, _) | ThreeOperands (SIMDOpr _, _, _) ->
    !!ir (AST.sideEffect UnsupportedFP)
  | _ ->
    let dst, src1, src2 = transOprToExprOfORR ins ctxt addr
    !!ir (dst := src1 .| src2)
  !>ir insLen

let rbit ins insLen ctxt addr =
  let ir = !*ctxt
  match ins.Operands with
  | TwoOperands (OprRegister _, OprRegister _) ->
    let dst, src = transTwoOprs ins ctxt addr
    let datasize = if ins.OprSize = 64<rt> then 64 else 32
    let tmp = !+ir ins.OprSize
    !<ir insLen
    for i in 0 .. (datasize - 1) do
      !!ir (AST.extract tmp 1<rt> (datasize - 1 - i) := AST.extract src 1<rt> i)
    !!ir (dst := tmp)
  | _ ->
    let struct (dst, src) = getTwoOprs ins
    let dstB, dstA = transOprToExpr128 ins ctxt addr dst
    let srcB, srcA = transOprToExpr128 ins ctxt addr src
    let eSize = 8
    let dataSize = int ins.OprSize
    let elements = dataSize / eSize
    let struct (resultA, resultB) = tmpVars2 ir 64<rt>
    let struct (element, rev) = tmpVars2 ir 8<rt>
    !<ir insLen
    for e in 0 .. elements - 1 do
      let src, result = if e < eSize then srcA, resultA else srcB, resultB
      !!ir (element := AST.extract src 8<rt> ((e % 8) * 8))
      for i in 0 .. 7 do
        !!ir (AST.extract rev 1<rt> (7 - i) := AST.extract element 1<rt> i)
      !!ir (AST.extract result 8<rt> ((e % 8) * 8) := rev)
    if ins.OprSize = 128<rt> then
      !!ir (dstA := resultA)
      !!ir (dstB := resultB)
    else
      !!ir (dstA := resultA)
  !>ir insLen

let ret ins insLen ctxt addr =
  let ir = !*ctxt
  let src = transOneOpr ins ctxt addr
  let target = !+ir 64<rt>
  !<ir insLen
  !!ir (target := src)
  branchTo ins ctxt target BrTypeRET InterJmpKind.IsRet ir
  !>ir insLen

let rev ins insLen ctxt addr =
  let ir = !*ctxt
  let e = if ins.OprSize = 64<rt> then 7 else 3
  let t = !+ir ins.OprSize
  !<ir insLen
  match ins.Operands with
  | TwoOperands(SIMDOpr _, SIMDOpr _) -> (* FIXME: SIMD Register *)
    !!ir (AST.sideEffect UnsupportedFP)
  | _ ->
    let dst, src = transTwoOprs ins ctxt addr
    for i in 0 .. e do
      !!ir (AST.extract t 8<rt> ((e - i) * 8) := AST.extract src 8<rt> (i * 8))
    !!ir (dst := t)
  !>ir insLen

let rev16 ins insLen ctxt addr =
  let ir = !*ctxt
  let tmp = !+ir ins.OprSize
  !<ir insLen
  match ins.Operands with
  | TwoOperands(SIMDOpr _, SIMDOpr _) -> (* FIXME: SIMD Register *)
    !!ir (AST.sideEffect UnsupportedFP)
  | _ ->
    let dst, src = transTwoOprs ins ctxt addr
    !!ir (AST.extract tmp 8<rt> 8  := AST.extract src 8<rt> 0)
    !!ir (AST.extract tmp 8<rt> 0 := AST.extract src 8<rt> 8)
    !!ir (AST.extract tmp 8<rt> 24 := AST.extract src 8<rt> 16)
    !!ir (AST.extract tmp 8<rt> 16 := AST.extract src 8<rt> 24)
    if ins.OprSize = 64<rt> then
      !!ir (AST.extract tmp 8<rt> 40:= AST.extract src 8<rt> 32)
      !!ir (AST.extract tmp 8<rt> 32:= AST.extract src 8<rt> 40)
      !!ir (AST.extract tmp 8<rt> 52:= AST.extract src 8<rt> 48)
      !!ir (AST.extract tmp 8<rt> 48:= AST.extract src 8<rt> 56)
      !!ir (dst := tmp)
    !!ir (dst := tmp)
  !>ir insLen

let rev32 ins insLen ctxt addr =
  let ir = !*ctxt
  let tmp = !+ir ins.OprSize
  !<ir insLen
  match ins.Operands with
  | TwoOperands(SIMDOpr _, SIMDOpr _) -> (* FIXME: SIMD Register *)
    !!ir (AST.sideEffect UnsupportedFP)
  | _ ->
    let dst, src = transTwoOprs ins ctxt addr
    !!ir (AST.extract tmp 8<rt> 24:= AST.extract src 8<rt> 0)
    !!ir (AST.extract tmp 8<rt> 16:= AST.extract src 8<rt> 8)
    !!ir (AST.extract tmp 8<rt> 8:= AST.extract src 8<rt> 16)
    !!ir (AST.extract tmp 8<rt> 0:= AST.extract src 8<rt> 24)
    if ins.OprSize = 64<rt> then
      !!ir (AST.extract tmp 8<rt> 56:= AST.extract src 8<rt> 32)
      !!ir (AST.extract tmp 8<rt> 48:= AST.extract src 8<rt> 40)
      !!ir (AST.extract tmp 8<rt> 40:= AST.extract src 8<rt> 48)
      !!ir (AST.extract tmp 8<rt> 32:= AST.extract src 8<rt> 56)
      !!ir (dst := tmp)
    else
    !!ir (dst := tmp)
  !>ir insLen

let rorv ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2 = transThreeOprs ins ctxt addr
  let amount = src2 .% oprSzToExpr ins.OprSize
  !<ir insLen
  !!ir (dst := shiftReg src1 amount ins.OprSize SRTypeROR)
  !>ir insLen

let sbc ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2 = transThreeOprs ins ctxt addr
  let c = AST.zext ins.OprSize (getRegVar ctxt R.C)
  !<ir insLen
  let result, _ = addWithCarry src1 (AST.not src2) c ins.OprSize
  !!ir (dst := result)
  !>ir insLen

let sbfm ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src, immr, imms = transOprToExprOfSBFM ins ctxt addr
  let oprSz = ins.OprSize
  let bot, top = !+ir oprSz, !+ir oprSz
  let wmask, tmask = !+ir oprSz, !+ir oprSz
  let immN = if oprSz = 64<rt> then AST.num1 8<rt> else AST.num0 8<rt>
  let width = oprSzToExpr oprSz
  !<ir insLen
  decodeBitMasksForIR wmask tmask immN imms immr oprSz ir
  !!ir (bot := ror src immr width .& wmask)
  let srcS = (src >> imms) .& (numI32 1 oprSz)
  replicateForIR top srcS (oprSzToExpr oprSz) oprSz ir
  !!ir (dst := (top .& AST.not tmask) .| (bot .& tmask))
  !>ir insLen

let sdiv ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2 = transThreeOprs ins ctxt addr
  let cond = src2 == AST.num0 ins.OprSize
  !<ir insLen
  (* FIXME: RoundTowardsZero *)
  !!ir (dst := AST.ite cond (AST.num0 ins.OprSize) (src1 ./ src2))
  !>ir insLen

let smaddl ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2, src3 = transFourOprs ins ctxt addr
  !<ir insLen
  !!ir (dst := src3 .+ (AST.sext 64<rt> src1 .* AST.sext 64<rt> src2))
  !>ir insLen

let smsubl ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2, src3 = transOprToExprOfSMSUBL ins ctxt addr
  !<ir insLen
  !!ir (dst := src3 .- (AST.sext 64<rt> src1 .* AST.sext 64<rt> src2))
  !>ir insLen

let smulh ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2 = transThreeOprs ins ctxt addr
  let tSrc1B = !+ir 64<rt>
  let tSrc1A = !+ir 64<rt>
  let tSrc2B = !+ir 64<rt>
  let tSrc2A = !+ir 64<rt>
  let n32 = numI32 32 64<rt>
  let mask = numI64 0xFFFFFFFFL 64<rt>
  !<ir insLen
  !!ir (tSrc1B := (src1 >> n32) .& mask)
  !!ir (tSrc1A := src1 .& mask)
  !!ir (tSrc2B := (src2 >> n32) .& mask)
  !!ir (tSrc2A := src2 .& mask)
  let high = tSrc1B .* tSrc2B
  let mid = (tSrc1A .* tSrc2B) .+ (tSrc1B .* tSrc2A)
  let low = (tSrc1A .* tSrc2A) >> n32
  !!ir (dst := high .+ ((mid .+ low) >> n32)) (* [127:64] *)
  !>ir insLen

let smull ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  match ins.Operands with
  | ThreeOperands (SIMDOpr _, _, _) -> !!ir (AST.sideEffect UnsupportedFP)
  | _ ->
    let dst, src1, src2 = transThreeOprs ins ctxt addr
    let src3 = getRegVar ctxt R.XZR
    !!ir (dst := src3 .+ (AST.sext 64<rt> src1 .* AST.sext 64<rt> src2))
  !>ir insLen

let stp ins insLen ctxt addr =
  let ir = !*ctxt
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  !<ir insLen
  let address = !+ir 64<rt>
  let dByte = numI32 (RegType.toByteWidth ins.OprSize) 64<rt>
  match ins.OprSize with
  | 128<rt> ->
    let struct (src1, src2, src3) = getThreeOprs ins
    let src1B, src1A = transOprToExpr128 ins ctxt addr src1
    let src2B, src2A = transOprToExpr128 ins ctxt addr src2
    let bReg, offset = transOprToExpr ins ctxt addr src3 |> separateMemExpr
    let n8 = numI32 8 64<rt>
    !!ir (address := bReg)
    !!ir (address := if isPostIndex then address else address .+ offset)
    !!ir (AST.loadLE 64<rt> address := src1A)
    !!ir (AST.loadLE 64<rt> (address .+ n8) := src1B)
    !!ir (AST.loadLE 64<rt> (address .+ dByte) := src2A)
    !!ir (AST.loadLE 64<rt> (address .+ dByte .+ n8) := src2B)
    if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
    else if isWBack then !!ir (bReg := address) else ()
  | _ ->
    let src1, src2, (bReg, offset) = transThreeOprsSepMem ins ctxt addr
    !!ir (address := bReg)
    !!ir (address := if isPostIndex then address else address .+ offset)
    !!ir (AST.loadLE ins.OprSize address := src1)
    !!ir (AST.loadLE ins.OprSize (address .+ dByte) := src2)
    if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
    else if isWBack then !!ir (bReg := address) else ()
  !>ir insLen

let str ins insLen ctxt addr =
  let ir = !*ctxt
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  !<ir insLen
  match ins.OprSize with
  | 128<rt> ->
    let struct (src1, src2) = getTwoOprs ins
    let srcB, srcA = transOprToExpr128 ins ctxt addr src1
    let bReg, offset = transOprToExpr ins ctxt addr src2 |> separateMemExpr
    let address = !+ir 64<rt>
    !!ir (address := bReg)
    !!ir (address := if isPostIndex then address else address .+ offset)
    !!ir (AST.loadLE 64<rt> address := srcA)
    !!ir (AST.loadLE 64<rt> (address .+ (numI32 8 64<rt>)) := srcB)
    if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
    else if isWBack then !!ir (bReg := address) else ()
  | _ ->
    let src, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
    let address = !+ir 64<rt>
    let data = !+ir ins.OprSize
    !!ir (address := bReg)
    !!ir (address := if isPostIndex then address else address .+ offset)
    !!ir (data := src)
    !!ir (AST.loadLE ins.OprSize address := data)
    if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
    else if isWBack then !!ir (bReg := address) else ()
  !>ir insLen

let strb ins insLen ctxt addr =
  let ir = !*ctxt
  let src, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  let address = !+ir 64<rt>
  let data = !+ir 8<rt>
  !<ir insLen
  !!ir (address := bReg)
  !!ir (address := if isPostIndex then address else address .+ offset)
  !!ir (data := AST.xtlo 8<rt> src)
  !!ir (AST.loadLE 8<rt> address := data)
  if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
  else if isWBack then !!ir (bReg := address) else ()
  !>ir insLen

let strh ins insLen ctxt addr =
  let ir = !*ctxt
  let src, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  let address = !+ir 64<rt>
  let data = !+ir 16<rt>
  !<ir insLen
  !!ir (address := bReg)
  !!ir (address := if isPostIndex then address else address .+ offset)
  !!ir (data := AST.xtlo 16<rt> src)
  !!ir (AST.loadLE 16<rt> address := data)
  if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
  else if isWBack then !!ir (bReg := address) else ()
  !>ir insLen

let stur ins insLen ctxt addr =
  let ir = !*ctxt
  let isWBack, isPostIndex = getIsWBackAndIsPostIndex ins.Operands
  let address = !+ir 64<rt>
  let data = !+ir ins.OprSize
  !<ir insLen
  match ins.OprSize with
  | 128<rt> ->
    let struct (src1, src2) = getTwoOprs ins
    let src1B, src1A = transOprToExpr128 ins ctxt addr src1
    let bReg, offset = transOprToExpr ins ctxt addr src2 |> separateMemExpr
    !!ir (address := bReg)
    !!ir (address := if isPostIndex then address else address .+ offset)
    !!ir (AST.loadLE 64<rt> address := src1A)
    !!ir (AST.loadLE 64<rt> (address .+ (numI32 8 64<rt>)) := src1B)
    if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
    else if isWBack then !!ir (bReg := address) else ()
  | _ ->
    let src, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
    !!ir (address := bReg)
    !!ir (address := if isPostIndex then address else address .+ offset)
    !!ir (data := src)
    !!ir (AST.loadLE ins.OprSize address := data)
    if isWBack && isPostIndex then !!ir (bReg := address .+ offset)
    else if isWBack then !!ir (bReg := address) else ()
  !>ir insLen

let sturb ins insLen ctxt addr =
  let ir = !*ctxt
  let src, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let address = !+ir 64<rt>
  let data = !+ir 8<rt>
  !<ir insLen
  !!ir (address := bReg)
  !!ir (address := address .+ offset)
  !!ir (data := AST.xtlo 8<rt> src)
  !!ir (AST.loadLE 8<rt> address := data)
  !>ir insLen

let sturh ins insLen ctxt addr =
  let ir = !*ctxt
  let src, (bReg, offset) = transTwoOprsSepMem ins ctxt addr
  let address = !+ir 64<rt>
  let data = !+ir 16<rt>
  !<ir insLen
  !!ir (address := bReg)
  !!ir (address := address .+ offset)
  !!ir (data := AST.xtlo 16<rt> src)
  !!ir (AST.loadLE 16<rt> address := data)
  !>ir insLen

let sub ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  match ins.Operands with
  | TwoOperands (SIMDOpr _, SIMDOpr _) ->
    !!ir (AST.sideEffect UnsupportedFP) (* FIXME: NEG SIMD Register *)
  | ThreeOperands _ when ins.Opcode = Opcode.SUB ->
    !!ir (AST.sideEffect UnsupportedFP) (* FIXME: SUB SIMD Register *)
  | _ ->
    let dst, src1, src2 = transOprToExprOfSUB ins ctxt addr
    let result, _ = addWithCarry src1 src2 (AST.num1 ins.OprSize) ins.OprSize
    !!ir (dst := result)
  !>ir insLen

let subs ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  transOprToExprOfSUBS ins ctxt addr ir
  !>ir insLen

let tbnz ins insLen ctxt addr =
  let ir = !*ctxt
  let test, imm, label = transThreeOprs ins ctxt addr
  let pc = getPC ctxt
  let fall = pc .+ numU32 insLen 64<rt>
  let cond = (test >> imm .& AST.num1 ins.OprSize) == AST.num1 ins.OprSize
  !<ir insLen
  !!ir (AST.intercjmp cond (pc .+ label) fall)
  !>ir insLen

let tbz ins insLen ctxt addr =
  let ir = !*ctxt
  let test, imm, label = transThreeOprs ins ctxt addr
  let pc = getPC ctxt
  let fall = pc .+ numU32 insLen 64<rt>
  let cond = (test >> imm .& AST.num1 ins.OprSize) == AST.num0 ins.OprSize
  !<ir insLen
  !!ir (AST.intercjmp cond (pc .+ label) fall)
  !>ir insLen

let udiv ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2 = transThreeOprs ins ctxt addr
  let cond = src2 == AST.num0 ins.OprSize
  !<ir insLen
  !!ir // FIXME: RoundTwoardsZero
    (dst := AST.ite cond (AST.num0 ins.OprSize) (src1 ./ src2))
  !>ir insLen

let umaddl ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2, src3 = transFourOprs ins ctxt addr
  !<ir insLen
  !!ir (dst := src3 .+ (AST.zext 64<rt> src1 .* AST.zext 64<rt> src2))
  !>ir insLen

let umsubl ins insLen ctxt addr =
  let dst, src1, src2, src3 = transOprToExprOfUMADDL ins ctxt addr
  let ir = !*ctxt
  !<ir insLen
  !!ir (dst := src3 .- (AST.zext 64<rt> src1 .* AST.zext 64<rt> src2))
  !>ir insLen

let umulh ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src1, src2 = transThreeOprs ins ctxt addr
  let tSrc1B = !+ir 64<rt>
  let tSrc1A = !+ir 64<rt>
  let tSrc2B = !+ir 64<rt>
  let tSrc2A = !+ir 64<rt>
  let n32 = numI32 32 64<rt>
  let mask = numI64 0xFFFFFFFFL 64<rt>
  !<ir insLen
  !!ir (tSrc1B := (src1 >> n32) .& mask)
  !!ir (tSrc1A := src1 .& mask)
  !!ir (tSrc2B := (src2 >> n32) .& mask)
  !!ir (tSrc2A := src2 .& mask)
  let high = tSrc1B .* tSrc2B
  let mid = (tSrc1A .* tSrc2B) .+ (tSrc1B .* tSrc2A)
  let low = (tSrc1A .* tSrc2A) >> n32
  !!ir (dst := high .+ ((mid .+ low) >> n32)) (* [127:64] *)
  !>ir insLen

let umull ins insLen ctxt addr =
  let ir = !*ctxt
  !<ir insLen
  match ins.Operands with
  | ThreeOperands (SIMDOpr _, _, _) -> !!ir (AST.sideEffect UnsupportedFP)
  | _ ->
    let dst, src1, src2 = transThreeOprs ins ctxt addr
    let src3 = getRegVar ctxt R.XZR
    !!ir (dst := src3 .- (AST.zext 64<rt> src1 .* AST.zext 64<rt> src2))
  !>ir insLen

let ubfm ins insLen ctxt addr =
  let ir = !*ctxt
  let dst, src, immr, imms = transOprToExprOfUBFM ins ctxt addr
  let oSz = ins.OprSize
  let bot = !+ir oSz
  let wmask, tmask = !+ir oSz, !+ir oSz
  let immN = if ins.OprSize = 64<rt> then AST.num1 8<rt> else AST.num0 8<rt>
  decodeBitMasksForIR wmask tmask immN imms immr oSz ir
  let width = oprSzToExpr ins.OprSize
  !<ir insLen
  !!ir (bot := ror src immr width .& wmask)
  !!ir (dst := bot .& tmask)
  !>ir insLen

/// The logical shift left(or right) is the alias of LS{L|R}V and UBFM.
/// Therefore, it is necessary to distribute to the original instruction.
let distLogcalShift ins ctxt addr =
  match ins.Operands with
  | ThreeOperands (_, _, Immediate _) -> ubfm ins ctxt addr
  | ThreeOperands (_, _, OprRegister _) when ins.Opcode = Opcode.LSL ->
    lslv ins ctxt addr
  | ThreeOperands (_, _, OprRegister _) when ins.Opcode = Opcode.LSR ->
    lsrv ins ctxt addr
  | _ -> raise InvalidOperandException

/// Translate IR.
let translate ins insLen ctxt =
  let addr = ins.Address
  match ins.Opcode with
  | Opcode.ADC -> adc ins insLen ctxt addr
  | Opcode.ADD -> add ins insLen ctxt addr
  | Opcode.ADDS | Opcode.CMN -> adds ins insLen ctxt addr
  | Opcode.ADDP | Opcode.ADDV -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.ADR -> adr ins insLen ctxt addr
  | Opcode.ADRP -> adrp ins insLen ctxt addr
  | Opcode.AND -> logAnd ins insLen ctxt addr
  | Opcode.ANDS -> ands ins insLen ctxt addr
  | Opcode.ASR -> asrv ins insLen ctxt addr
  | Opcode.B -> b ins insLen ctxt addr
  | Opcode.BEQ -> bCond ins insLen ctxt addr EQ
  | Opcode.BNE -> bCond ins insLen ctxt addr NE
  | Opcode.BCS -> bCond ins insLen ctxt addr CS
  | Opcode.BCC -> bCond ins insLen ctxt addr CC
  | Opcode.BMI -> bCond ins insLen ctxt addr MI
  | Opcode.BPL -> bCond ins insLen ctxt addr PL
  | Opcode.BVS -> bCond ins insLen ctxt addr VS
  | Opcode.BVC -> bCond ins insLen ctxt addr VC
  | Opcode.BHI -> bCond ins insLen ctxt addr HI
  | Opcode.BLS -> bCond ins insLen ctxt addr LS
  | Opcode.BGE -> bCond ins insLen ctxt addr GE
  | Opcode.BLT -> bCond ins insLen ctxt addr LT
  | Opcode.BGT -> bCond ins insLen ctxt addr GT
  | Opcode.BLE -> bCond ins insLen ctxt addr LE
  | Opcode.BAL -> bCond ins insLen ctxt addr AL
  | Opcode.BNV -> bCond ins insLen ctxt addr NV
  | Opcode.BFI | Opcode.BFXIL -> bfm ins insLen ctxt addr
  | Opcode.BIC -> bic ins insLen ctxt addr
  | Opcode.BICS -> bics ins insLen ctxt addr
  | Opcode.BIF | Opcode.BIT -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.BL -> bl ins insLen ctxt addr
  | Opcode.BLR -> blr ins insLen ctxt addr
  | Opcode.BR -> br ins insLen ctxt addr
  | Opcode.BRK -> sideEffects insLen ctxt Breakpoint
  | Opcode.BSL -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.CBNZ -> cbnz ins insLen ctxt addr
  | Opcode.CBZ -> cbz ins insLen ctxt addr
  | Opcode.CCMN -> ccmn ins insLen ctxt addr
  | Opcode.CCMP -> ccmp ins insLen ctxt addr
  | Opcode.CLZ -> clz ins insLen ctxt addr
  | Opcode.CMP -> cmp ins insLen ctxt addr
  | Opcode.CMEQ -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.CMGE | Opcode.CMLT | Opcode.CMTST ->
    sideEffects insLen ctxt UnsupportedFP
  | Opcode.CMHI | Opcode.CMHS -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.CNEG | Opcode.CSNEG -> csneg ins insLen ctxt addr
  | Opcode.CNT -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.CSEL -> csel ins insLen ctxt addr
  | Opcode.CSETM | Opcode.CINV | Opcode.CSINV -> csinv ins insLen ctxt addr
  | Opcode.CSINC | Opcode.CINC | Opcode.CSET -> csinc ins insLen ctxt addr
  | Opcode.DUP -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.EOR | Opcode.EON -> eor ins insLen ctxt addr
  | Opcode.EXT -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.EXTR | Opcode.ROR -> extr ins insLen ctxt addr
  | Opcode.FABS -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FABD | Opcode.FADD -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FADDP -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FCCMP -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FCMP -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FCMPE -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FCSEL -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FCVT | Opcode.FCVTMU ->
    sideEffects insLen ctxt UnsupportedFP
  | Opcode.FCVTZS -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FCVTZU -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FDIV -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FMAX -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FMADD -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FMOV -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FMUL -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FNEG -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FNMUL -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FRINTM | Opcode.FRINTA | Opcode.FRINTP ->
    sideEffects insLen ctxt UnsupportedFP
  | Opcode.FSUB -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FSQRT -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.FMSUB -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.INS -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.LDP -> ldp ins insLen ctxt addr
  | Opcode.LDPSW -> ldpsw ins insLen ctxt addr
  | Opcode.LDR -> ldr ins insLen ctxt addr
  | Opcode.LDRB -> ldrb ins insLen ctxt addr
  | Opcode.LDRSB -> ldrsb ins insLen ctxt addr
  | Opcode.LDRH -> ldrh ins insLen ctxt addr
  | Opcode.LDRSW -> ldrsw ins insLen ctxt addr
  | Opcode.LDRSH -> ldrsh ins insLen ctxt addr
  | Opcode.LDUR -> ldur ins insLen ctxt addr
  | Opcode.LDURB -> ldurb ins insLen ctxt addr
  | Opcode.LDURH -> ldurh ins insLen ctxt addr
  | Opcode.LDURSB -> ldursb ins insLen ctxt addr
  | Opcode.LDURSH -> ldursh ins insLen ctxt addr
  | Opcode.LDURSW -> ldursw ins insLen ctxt addr
  | Opcode.LD1 | Opcode.LD1R | Opcode.LD2 | Opcode.LD2R | Opcode.LD3
  | Opcode.LD3R | Opcode.LD4 | Opcode.LD4R ->
    sideEffects insLen ctxt UnsupportedFP
  | Opcode.LSL | Opcode.LSR -> distLogcalShift ins insLen ctxt addr
  | Opcode.MADD -> madd ins insLen ctxt addr
  | Opcode.MLA -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.MNEG -> msub ins insLen ctxt addr
  | Opcode.MOV | Opcode.MOVN | Opcode.MOVK | Opcode.MOVZ ->
    mov ins insLen ctxt addr
  | Opcode.MOVI | Opcode.MVNI -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.MRS -> mrs ins insLen ctxt addr
  | Opcode.MSUB -> msub ins insLen ctxt addr
  | Opcode.MUL -> madd ins insLen ctxt addr
  | Opcode.MVN -> orn ins insLen ctxt addr
  | Opcode.NEG -> sub ins insLen ctxt addr
  | Opcode.NEGS -> subs ins insLen ctxt addr
  | Opcode.NOP -> nop insLen ctxt
  | Opcode.ORN -> orn ins insLen ctxt addr
  | Opcode.ORR -> orr ins insLen ctxt addr
  | Opcode.RBIT -> rbit ins insLen ctxt addr
  | Opcode.RET -> ret ins insLen ctxt addr
  | Opcode.REV -> rev ins insLen ctxt addr
  | Opcode.REV16 -> rev16 ins insLen ctxt addr
  | Opcode.REV32 -> rev32 ins insLen ctxt addr
  | Opcode.REV64 -> rev ins insLen ctxt addr
  | Opcode.RORV -> rorv ins insLen ctxt addr
  | Opcode.SBC -> sbc ins insLen ctxt addr
  | Opcode.SBFIZ | Opcode.SBFX | Opcode.SXTB | Opcode.SXTH | Opcode.SXTW ->
    sbfm ins insLen ctxt addr
  | Opcode.SCVTF -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.SDIV -> sdiv ins insLen ctxt addr
  | Opcode.SMADDL -> smaddl ins insLen ctxt addr
  | Opcode.SMSUBL | Opcode.SMNEGL -> smsubl ins insLen ctxt addr
  | Opcode.SMULH -> smulh ins insLen ctxt addr
  | Opcode.SMULL -> smull ins insLen ctxt addr
  | Opcode.SSHR | Opcode.SSHL |Opcode.SSHLL | Opcode.SSHLL2 | Opcode.SHL ->
    sideEffects insLen ctxt UnsupportedFP
  | Opcode.ST1 | Opcode.ST2 | Opcode.ST3 | Opcode.ST4 ->
    sideEffects insLen ctxt UnsupportedFP
  | Opcode.STP -> stp ins insLen ctxt addr
  | Opcode.STR -> str ins insLen ctxt addr
  | Opcode.STRB -> strb ins insLen ctxt addr
  | Opcode.STRH -> strh ins insLen ctxt addr
  | Opcode.STUR -> stur ins insLen ctxt addr
  | Opcode.STURB -> sturb ins insLen ctxt addr
  | Opcode.STURH -> sturh ins insLen ctxt addr
  | Opcode.SUB -> sub ins insLen ctxt addr
  | Opcode.SUBS -> subs ins insLen ctxt addr
  | Opcode.TBL -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.TBNZ -> tbnz ins insLen ctxt addr
  | Opcode.TBZ -> tbz ins insLen ctxt addr
  | Opcode.TST -> ands ins insLen ctxt addr
  | Opcode.UADDLV | Opcode.UADDW | Opcode.UMAXV | Opcode.UMINV ->
    sideEffects insLen ctxt UnsupportedFP
  | Opcode.UBFIZ | Opcode.UBFX | Opcode.UXTB | Opcode.UXTH ->
    ubfm ins insLen ctxt addr
  | Opcode.UCVTF -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.UDIV -> udiv ins insLen ctxt addr
  | Opcode.UMAX -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.UMADDL -> umaddl ins insLen ctxt addr
  | Opcode.UMLAL | Opcode.UMLAL2 ->
    sideEffects insLen ctxt UnsupportedFP
  | Opcode.UMSUBL | Opcode.UMNEGL -> umsubl ins insLen ctxt addr
  | Opcode.UMULH -> umulh ins insLen ctxt addr
  | Opcode.UMULL -> umull ins insLen ctxt addr
  | Opcode.UMOV -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.URSHL ->  sideEffects insLen ctxt UnsupportedFP
  | Opcode.USHL -> sideEffects insLen ctxt UnsupportedFP
  | Opcode.USHLL | Opcode.USHLL2 | Opcode.USHR ->
    sideEffects insLen ctxt UnsupportedFP
  | Opcode.UZP1 | Opcode.UZP2 | Opcode.ZIP1 | Opcode.ZIP2 ->
    sideEffects insLen ctxt UnsupportedFP
  | Opcode.XTN | Opcode.XTN2 -> sideEffects insLen ctxt UnsupportedFP
  | o ->
#if DEBUG
         eprintfn "%A" o
#endif
         raise <| NotImplementedIRException (Disasm.opCodeToString o)

// vim: set tw=80 sts=2 sw=2:
