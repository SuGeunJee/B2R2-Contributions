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

namespace B2R2.FrontEnd.S390

open System.Runtime.CompilerServices
open B2R2

[<assembly: InternalsVisibleTo("B2R2.FrontEnd.S390.Tests")>]
do ()

/// <summary>
///   S390 opcodes.
/// </summary>
type Opcode =
  | MyOp = 0 // FIXME

type internal Op = Opcode

type Operand =
  | MyOpr // FIXME

type Operands =
  | NoOperand
  | OneOperand of Operand
  | TwoOperands of Operand * Operand

/// Basic information obtained by parsing a S390 instruction.
[<NoComparison; CustomEquality>]
type InsInfo = {
  /// Address.
  Address: Addr
  /// Instruction length.
  NumBytes: uint32
  /// Opcode.
  Opcode: Opcode
  /// Operands.
  Operands: Operands
  /// Operation Size.
  OperationSize: RegType
}
with
  override this.GetHashCode () =
    hash (this.Address,
          this.NumBytes,
          this.Opcode,
          this.Operands,
          this.OperationSize)

  override this.Equals (i) =
    match i with
    | :? InsInfo as i ->
      i.Address = this.Address
      && i.NumBytes = this.NumBytes
      && i.Opcode = this.Opcode
      && i.Operands = this.Operands
      && i.OperationSize = this.OperationSize
    | _ -> false

// vim: set tw=80 sts=2 sw=2:
