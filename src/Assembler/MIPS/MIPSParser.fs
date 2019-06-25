﻿(*
  B2R2 - the Next-Generation Reversing Platform

  Author: Kangsu Kim <kskim0610@kaist.ac.kr>

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

module B2R2.Assembler.MIPS.Parser

open System

open B2R2.FrontEnd.MIPS
open FParsec

type UserState = unit
type Parser<'t> = Parser<'t, UserState>

type Operand =
  | Address of string option * string
  | Immediate of string
  | Label of string
  | Register of string

type Statement =
  | Instruction of (string * Operand list)
  | Label of string

module private Rules =
  let isWhitespace c = [' '; '\t'; '\f'] |> List.contains c
  let whitespace: Parser<_> = manySatisfy isWhitespace
  let whitespace1: Parser<_> = many1Satisfy isWhitespace
  let terminator: Parser<_> = pchar ';' <|> newline
  let skipWhitespaces s = whitespace >>. s .>> whitespace

  let operandSeps: Parser<_> = (pstring "," |> skipWhitespaces) <|> whitespace1
  let betweenParen s = s |> skipWhitespaces |> between (pchar '(') (pchar ')')

  let alphaNumericWithUnderscore s = Char.IsLetterOrDigit s || s = '_'

  let pid: Parser<_> = many1Satisfy alphaNumericWithUnderscore
  let labelDef: Parser<_> = pid .>> pchar ':' |>> Statement.Label

  let opcode: Parser<_> =
    (Enum.GetNames typeof<Opcode>)
    |> Array.map (fun x -> pstringCI x)
    |> choice

  let label: Parser<_> = pid |>> Operand.Label

  // TODO: Support +, - operators
  let numberFormat =
    NumberLiteralOptions.AllowBinary
    ||| NumberLiteralOptions.AllowOctal
    ||| NumberLiteralOptions.AllowHexadecimal
    ||| NumberLiteralOptions.AllowMinusSign
  let pimm: Parser<_> = numberLiteral numberFormat "number" |>> string
  let imm: Parser<_> = pimm |>> Immediate

  // TODO: Support other names (e.g., $at, $v0, ...)
  let preg: Parser<_> =
    (Enum.GetNames typeof<Register>)
    |> Array.map (fun x -> optional (pchar '$') >>. pstringCI x)
    |> choice
  let reg: Parser<_> = preg |>> Register
  let regAddr: Parser<_> = betweenParen preg

  let paddr: Parser<_> = opt (pimm .>> whitespace) .>>. regAddr
  let addr: Parser<_> = paddr |>> Address

  let operand: Parser<_> = addr <|> reg <|> imm <|> label
  // TODO: Limit to 3 operands
  let operands: Parser<_> = sepBy operand operandSeps
  let instruction: Parser<_> = opcode .>>. (whitespace >>. operands) |>> Instruction
  let statement: Parser<_> = (instruction <|> labelDef) |> skipWhitespaces
  let statements: Parser<_> = sepEndBy statement terminator .>> eof

let parse assembly =
  run Rules.statements assembly
