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

namespace B2R2.MiddleEnd.ControlFlowAnalysis

open System
open System.Threading
open System.Threading.Tasks.Dataflow
open System.Collections.Generic
open B2R2
open B2R2.FrontEnd
open B2R2.MiddleEnd.ControlFlowGraph

/// Task manager for control flow recovery.
type CFGTaskManager<'V,
                    'E,
                    'Abs,
                    'Act,
                    'State,
                    'Req,
                    'Res when 'V :> IRBasicBlock<'Abs>
                          and 'V: equality
                          and 'E: equality
                          and 'Abs: null
                          and 'Act :> ICFGAction
                          and 'State :> IResettable
                          and 'State: (new: unit -> 'State)>
    (hdl,
     instrs: InstructionCollection,
     cfgConstructor: IRCFG.IConstructable<'V, 'E, 'Abs>,
     strategy: IFunctionBuildingStrategy<_, _, _, _, 'State, 'Req, 'Res>,
     noRetAnalyzer,
     ?numThreads) =

  let numThreads = defaultArg numThreads Environment.ProcessorCount
  let builders = Dictionary<Addr, FunctionBuilder<_, _, _, _, _, _, _>> ()
  let channel = BufferBlock<FunctionBuilder<_, _, _, _, _, _, _>> ()
  let cts = new CancellationTokenSource ()
  let dependenceMap = FunctionDependenceMap ()

  let rec processor (inbox: IAgentMessageReceivable<_>) =
    while not inbox.IsCancelled do
      match inbox.Receive () with
      | AddTask entryPoint ->
        let builder: FunctionBuilder<_, _, _, _, _, _, _> =
          getBuilder entryPoint
        if builder.InProgress || (builder :> IValidityCheck).IsValid then ()
        else channel.Post builder |> ignore
      | AddDependency (caller, callee) ->
        dependenceMap.AddDependency (caller, callee)
      | ReportResult (entryPoint, Success) ->
        builders[entryPoint].InProgress <- false
        dependenceMap.RemoveAndGetCallers entryPoint
        |> Seq.iter (getBuilder >> channel.Post >> ignore)
      | ReportResult (entryPoint, Postponement) ->
        builders[entryPoint].InProgress <- false
      | ReportResult (entryPoint, Failure _) ->
        (builders[entryPoint] :> IValidityCheck).Invalidate ()
        dependenceMap.RemoveAndGetCallers entryPoint
        |> addTasks
      | Query (entryPoint, _, _) as msg ->
        let builder = builders[entryPoint]
        strategy.OnQuery (msg, builder :> IValidityCheck)

  and getBuilder addr =
    match builders.TryGetValue addr with
    | true, builder -> builder
    | false, _ ->
      let builder =
        FunctionBuilder (hdl, instrs, addr,
                         cfgConstructor, manager, strategy, noRetAnalyzer)
      builders[addr] <- builder
      builder

  and manager = Agent<_>.Start (processor, cts.Token)

  and addTasks (entryPoints: IEnumerable<Addr>) =
    entryPoints |> Seq.iter (AddTask >> manager.Post)

  let _workers =
    Array.init numThreads (fun idx ->
      CFGTaskWorker (idx, manager, channel, cts.Token))

#if CFGDEBUG
  do initLogger numThreads
#endif

  /// Start the CFG recovery process using the given sequence of entry points.
  member __.StartRecovery (entryPoints: Addr[]) =
    entryPoints |> addTasks

/// Task worker for control flow recovery.
and private CFGTaskWorker<'V,
                          'E,
                          'Abs,
                          'Act,
                          'State,
                          'Req,
                          'Res when 'V :> IRBasicBlock<'Abs>
                                and 'V: equality
                                and 'E: equality
                                and 'Abs: null
                                and 'Act :> ICFGAction
                                and 'State :> IResettable
                                and 'State: (new: unit -> 'State)>
    (tid: int,
     manager: Agent<CFGTaskMessage<'Req, 'Res>>,
     chan: BufferBlock<FunctionBuilder<'V, 'E, 'Abs, 'Act, 'State, 'Req, 'Res>>,
     token: CancellationToken) =

  let _worker = task {
    while not token.IsCancellationRequested do
      let builder = chan.Receive (cancellationToken=token)
      builder.ThreadId <- tid
      let res = builder.Recover ()
      manager.Post <| ReportResult (builder.EntryPoint, res)
  }
