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

namespace B2R2.BinCorpus

open B2R2

/// A mapping from an instruction address to computed jump targets. This table
/// stores only "computed" jump targets.
type JmpTargetMap = Map<Addr, Addr list>

/// Jump table (for switch-case) information: (table range * entry size).
type JumpTableInfo = AddrRange * RegType

/// Basic-block addresses of callers of no-return function, and program points
/// of no-return call.
type NoReturnInfo = Set<Addr> * Set<ProgramPoint>

/// Recovered information about the binary under analysis.
type RecoveredInfo = {
  /// Recovered function entries.
  Entries: Set<LeaderInfo>
  /// Indirect branches' target addresses.
  IndirectBranchMap: Map<Addr, Addr * Set<Addr> * JumpTableInfo option>
  /// No return function addresses and program points.
  NoReturnInfo: NoReturnInfo
}

module RecoveredInfo =

  let init entries indmap noretInfo =
    { Entries = entries
      IndirectBranchMap = indmap
      NoReturnInfo = noretInfo }
