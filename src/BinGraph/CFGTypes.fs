(*
  B2R2 - the Next-Generation Reversing Platform

  Author: Soomin Kim <soomink@kaist.ac.kr>

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

namespace B2R2.BinGraph

open B2R2
open B2R2.FrontEnd
open B2R2.BinIR
open B2R2.BinIR.LowUIR
open System.Collections.Generic

type PPoint = Addr * int

type DisasmBBL (range: AddrRange, instrs, last) =
  inherit RangedVertexData (range)

  /// List of all the instructions in this block.
  member __.Instrs: Instruction list = instrs

  /// The last instruction of this block (to access it efficiently).
  member __.LastInstr: Instruction = last

  /// Do we need to resolve the successor(s) of this basic block?
  member val ToResolve = false with get, set

type IRVertexData () =
  inherit VertexData (VertexData.genID ())

type IRBBL (ppoint, lastPpoint, stmts, last) =
  inherit IRVertexData ()

  /// This block's starting program point
  member __.Ppoint: PPoint = ppoint

  /// List of all the statements in this block.
  member __.Stmts: Stmt list = stmts

  /// The last statement of this block (to access it efficiently).
  member __.LastStmt: Stmt = last

  /// Program point of the last statement.
  member __.LastPpoint: PPoint = lastPpoint

  /// Do we need to resolve the successor(s) of this basic block?
  member val ToResolve = false with get, set

type IRCall (target) =
  inherit IRVertexData ()

  member __.Target: Addr = target

type SSABBL (irbbl, stmts, last) =
  inherit VertexData (VertexData.genID ())

  member __.IRBBL: IRBBL = irbbl

  member __.Stmts: SSA.Stmt list = stmts

  member __.LastStmt: SSA.Stmt = last

  member val ToResolve = false with get, set

type CFGEdge =
  | JmpEdge
  | CJmpTrueEdge
  | CJmpFalseEdge
  | CallEdge
  | RetEdge
  | FallThroughEdge

type CFG<'a when 'a :> VertexData> = DiGraph<'a, CFGEdge>

type DisasmVertex = Vertex<DisasmBBL>

type DisasmCFG = RangedDiGraph<DisasmBBL, CFGEdge>

type IRVertex = Vertex<IRVertexData>

type IRCFG = SimpleDiGraph<IRVertexData, CFGEdge>

type SSAVertex = Vertex<SSABBL>

type SSACFG = SimpleDiGraph<SSABBL, CFGEdge>
