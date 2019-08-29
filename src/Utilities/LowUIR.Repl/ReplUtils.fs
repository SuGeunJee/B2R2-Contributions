﻿module B2R2.Utilities.LowUIR.Repl.ReplUtils
open System
open B2R2
open B2R2.BinIR.LowUIR
open B2R2.ConcEval
open B2R2.FrontEnd
open IRParseHelper

/// Supported repl commands. Other commands may be added here.
type ReplCommand =
  | IRStatement of string
  | Quit
  | Show
  | NoInput

module ReplCommand =
  let fromInput (str: string) =
    match str.Trim () with
    | "" -> NoInput
    | "quit" | "exit" | "stop" -> Quit
    | "show" -> Show
    | str -> IRStatement str

/// Status to remember the last context for pretty printing.
type Status (regSeq, tempRegSeq) =

  let mutable prevContext = (regSeq, tempRegSeq)

  member __.RegSeq = fst prevContext

  member __.TempRegSeq = snd prevContext

  member __.UpdateStatus (state: EvalState) =
    let context = EvalState.GetCurrentContext state
    prevContext <-
        ( Array.copy (Seq.toArray (context.Registers.ToSeq ())),
          Array.copy (Seq.toArray (context.Temporaries.ToSeq ())) )

  member __.GetUpdatedRegIndices state =
    Seq.fold2
      (fun acc t1 t2 ->
        if t1 <> t2 then fst t1 :: acc else acc)
      []
      ((EvalState.GetCurrentContext state).Registers.ToSeq ()) __.RegSeq

  member __.GetUpdatedTempRegs state =
    Seq.fold2
      (fun acc t1 t2 ->
        if t1 <> t2 then fst t1 :: acc else acc)
      []
      ((EvalState.GetCurrentContext state).Temporaries.ToSeq ()) __.TempRegSeq

/// Gets a string representation of an EvalValue.
let private getEvalValueString = function
  | Def bv -> bv.ToString ()
  | Undef -> "undef"

/// Gets the value of a register from the evalstate.
let private getRegValue st e =
  match e with
  | Var (_, n, _, _) -> EvalState.GetReg st n
  | PCVar (t, _) -> BitVector.ofUInt64 st.PC t |> Def
  | _ -> raise InvalidExprException

/// Gets the registers name (string), and value (EvalValue) tuple.
let getRegNameValTuple regExpr evalState =
  let regName =
    match regExpr with
    | Var (_typ, _, n, _) -> n
    | PCVar (_typ, n) -> n
    | x -> sprintf "%A" x
  regName, getRegValue evalState regExpr

/// Map of the isa string and corresponding number on the repl.
let isaMap =
  Map.empty. (* Creating an empty Map *)
    Add("", "x64").
    Add("0", "x64").
    Add("1", "x86").
    Add("2", "x64").
    Add("3", "armv7").
    Add("4", "armv7be").
    Add("5", "armv8a32").
    Add("6", "armv8a32be").
    Add("7", "mips32r2").
    Add("8", "mips32r2be").
    Add("9", "armv8a64").
    Add("10", "mips32r6").
    Add("11", "mips32r6be").
    Add("12", "armv8a64be").
    Add("13", "mips64r2").
    Add("14", "mips64r2be").
    Add("15", "mips64r6").
    Add("16", "mips64r6be");;

/// Gets the architecture from the number chosen by the user.
let numToArchitecture n =
  if Map.containsKey n isaMap then ISA.OfString isaMap.[n] |> Some else None

/// Initiates the registers in the architecture with value of zero.
let initStateForReplStart handle (pHelper: IRVarParseHelper) =
  let st = EvalState (B2R2.BinGraph.DisasHeuristic.imageLoader handle, true)
  EvalState.PrepareContext st 0 0UL
    (pHelper.InitStateRegs |> List.map (fun (x, y) -> (x, Def y)))

/// Gets a register name and EvalValue string representation.
let getRegValueString (rid: RegisterID, value: EvalValue) =
  let reg = Intel.Register.ofRegID rid
  let valueStr =
    match value with
    | Def bv -> BitVector.valToString bv
    | Undef -> "undef"
  sprintf "%s: %s" (reg.ToString ()) valueStr

/// Gets a temporary register name and EvalValue string representation.
let getTempRegrString (n: int, value: EvalValue) =
  let tRegName = "T_" + string (n)
  let valueStr =
    match value with
    | Def bv -> BitVector.valToString bv
    | Undef -> "undef"
  sprintf "%s: %s" tRegName valueStr



module ReplDisplay =

  /// Displayed table on the repl console.
  let ISATableStr =

    "========================================================================\n\
     ******* Choose an ISA number or press Enter for the default ISA ********\n\
     ========================================================================\n\
     0. Default(Intelx64)            | 1. x86, i386   | 2. x64, x86-64, amd64\n\
     3. armv7, armv7le, armel, armhf | 4. armv7be     | 5. armv8a32, aarch32\n\
     6. armv8a32be, aarch32be        | 7. mips32r2    | 8. mips32r2be\n\
     9. armv8a64, aarch64            | 10. mips32r6   | 11. mips32r6be\n\
     12. armv8a64be, aarch64be       | 13. mips64r2   | 14. mips64r2be\n\
     15. mips64r6                    | 16. mips64r6be \n\
     ========================================================================\n"

  let cprintf c fmt =
    Printf.kprintf
        (fun s ->
            let old = System.Console.ForegroundColor
            try
              System.Console.ForegroundColor <- c;
              System.Console.Write s
            finally
              System.Console.ForegroundColor <- old)
        fmt

  let printRed str = cprintf ConsoleColor.Red str
  let printBlue str = cprintf ConsoleColor.Blue str
  let printCyan str = cprintf ConsoleColor.Cyan str

  /// Gets register status string.
  let singleRegStatusString regExp evalState =
    getRegNameValTuple regExp evalState
    |> (fun (name, value) -> sprintf "%3s: %s" name (getEvalValueString value))

  /// Prints all the registers and their statuses to the console.
  let printRegStatusString state (pHelper: IRVarParseHelper) (status:Status) =
    let changedIds = status.GetUpdatedRegIndices state
    let idList = List.map pHelper.IdOf pHelper.MainRegs
    List.map
      (fun s ->
        singleRegStatusString s state |> sprintf "%-34s")
      pHelper.MainRegs |>
    List.iteri (fun i str ->
      if i%3 = 2 && List.contains (idList.[i]) changedIds then
        printRed "%s\n" str
      elif i%3 = 2 then printfn "%s" str
      elif List.contains (idList.[i]) changedIds then printRed "%s" str
      else printf "%s" str)

  /// Prints all the temporary registers and their statuses to the console.
  let printTRegStatusString state (status: Status) =
    let tRegSeq = (EvalState.GetCurrentContext state).Temporaries.ToSeq ()
    let changedTempRegs = status.GetUpdatedTempRegs state
    Seq.iteri
      (fun i rTuple ->
        if i = status.TempRegSeq.Length then
          printRed " %s " (getTempRegrString rTuple)
        elif List.contains (fst rTuple) changedTempRegs then
          printRed " %s " (getTempRegrString rTuple)
        else printf " %s " (getTempRegrString rTuple)
       ) tRegSeq
    printfn ""

  /// Used to print all available registers to the console.
  let printRegisters (state: EvalState) pHelper status =
    printCyan "Main registers: \n" ;
    printRegStatusString state pHelper status
    printCyan "\nTemporary Registers:" ;
    printTRegStatusString state status



