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

namespace B2R2.FrontEnd.SH4

open B2R2
open B2R2.FrontEnd.BinLifter

/// The internal representation for a SH4 instruction used by our disassembler
/// and lifter.
type SH4Instruction (addr, numBytes, insInfo) =
  inherit Instruction (addr, numBytes, WordSize())

  member val Info: InsInfo = insInfo

  override __.IsBranch () = Terminator.futureFeature ()
  override __.IsModeChanging () = false
  override __.IsDirectBranch () = Terminator.futureFeature ()
  override __.IsIndirectBranch () = Terminator.futureFeature ()
  override __.IsCondBranch () = Terminator.futureFeature ()
  override __.IsCJmpOnTrue () = Terminator.futureFeature ()
  override __.IsCall () = Terminator.futureFeature ()
  override __.IsRET () = Terminator.futureFeature ()
  override __.IsInterrupt () = Terminator.futureFeature ()
  override __.IsExit () = Terminator.futureFeature ()
  override __.IsTerminator () = Terminator.futureFeature ()
  override __.DirectBranchTarget (_) = Terminator.futureFeature ()
  override __.IndirectTrampolineAddr (_) = Terminator.futureFeature ()
  override __.Immediate (_) = Terminator.futureFeature ()
  override __.GetNextInstrAddrs () = Terminator.futureFeature ()
  override __.InterruptNum (_) = Terminator.futureFeature ()
  override __.IsNop () = Terminator.futureFeature ()

  override __.Translate ctxt =
    (Lifter.translate __.Info numBytes ctxt).ToStmts ()

  override __.TranslateToList ctxt =
    Lifter.translate __.Info numBytes ctxt

  override __.Disasm (showAddr, _) =
    let builder =
      DisasmStringBuilder (showAddr, false, WordSize.Bit32, addr, numBytes)
    Disasm.disas __.Info builder
    builder.ToString ()

  override __.Disasm () =
    let builder =
      DisasmStringBuilder (false, false, WordSize.Bit32, addr, numBytes)
    Disasm.disas __.Info builder
    builder.ToString ()

  override __.Decompose (showAddr) =
    let builder =
      DisasmWordBuilder (showAddr, false, WordSize.Bit32, addr, numBytes, 8)
    Disasm.disas __.Info builder
    builder.ToArray ()

  override __.IsInlinedAssembly () = false

  override __.Equals (_) = Terminator.futureFeature ()
  override __.GetHashCode () = Terminator.futureFeature ()
