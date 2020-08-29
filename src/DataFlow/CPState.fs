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

namespace B2R2.DataFlow

open B2R2
open B2R2.FrontEnd
open B2R2.BinIR.SSA
open B2R2.BinGraph
open B2R2.Lens
open System.Collections.Generic

/// An ID of an SSA memory instance.
type SSAMemID = int

type CPState = {
  /// BinHandler of the current binary.
  BinHandler: BinHandler
  /// SSA edges
  SSAEdges: SSAEdges.EdgeInfo
  /// SSA var values.
  RegState : Dictionary<Variable, CPValue>
  /// SSA mem values. Only store values of constant addresses.
  MemState : Dictionary<SSAMemID, Map<Addr, CPValue> * Set<Addr>>
  /// Executable edges from vid to vid. If there's no element for an edge, that
  /// means the edge is not executable.
  ExecutableEdges: HashSet<VertexID * VertexID>
  /// Executed edges from vid to vid.
  ExecutedEdges: HashSet<VertexID * VertexID>
  /// Default word size of the current analysis.
  DefaultWordSize : RegType
  /// Worklist for blocks.
  FlowWorkList: Queue<VertexID * VertexID>
  /// Worklist for SSA stmt, this stack stores a list of def variables, and we
  /// will use SSAEdges to find all related SSA statements.
  SSAWorkList: Stack<Variable>
  UndefinedMemories: HashSet<Addr>
}

module CPState =
  let private initStackRegister hdl (dict: Dictionary<_, _>) =
    match hdl.RegisterBay.StackPointer with
    | Some sp ->
      let rt = hdl.RegisterBay.RegIDToRegType sp
      let str = hdl.RegisterBay.RegIDToString sp
      let var = { Kind = RegVar (rt, sp, str); Identifier = 0 }
      dict.[var] <- Const (BitVector.ofUInt64 0x80000000UL rt)
      dict
    | None -> dict

  let private initMemory (dict: Dictionary<_, _>) =
    dict.[0] <- (Map.empty, Set.empty)
    dict

  let initState hdl ssaCfg =
    { BinHandler = hdl
      SSAEdges = SSAEdges.compute ssaCfg
      RegState = Dictionary () |> initStackRegister hdl
      MemState = Dictionary () |> initMemory
      ExecutableEdges = HashSet ()
      ExecutedEdges = HashSet ()
      DefaultWordSize = hdl.ISA.WordSize |> WordSize.toRegType
      FlowWorkList = Queue ()
      SSAWorkList = Stack ()
      UndefinedMemories = HashSet () }

  let markExecutable st src dst =
    if st.ExecutableEdges.Add (src, dst) then st.FlowWorkList.Enqueue (src, dst)
    else ()

  let isExecuted st src dst =
    st.ExecutedEdges.Contains (src, dst)

  let tryFindReg st r =
    match st.RegState.TryGetValue r with
    | true, v -> Some v
    | false, _ ->
      if r.Identifier = 0 then Some NotAConst
      else None

  let findReg st r =
    match st.RegState.TryGetValue r with
    | true, v -> v
    | false, _ -> NotAConst

  let findMem st m rt addr =
    let mid = m.Identifier
    let align = RegType.toByteWidth rt |> uint64
    if st.MemState.ContainsKey mid then ()
    else st.MemState.[mid] <- (Map.empty, Set.empty)
    if (rt = st.DefaultWordSize) && (addr % align = 0UL) then
      match Map.tryFind addr <| fst st.MemState.[mid] with
      | Some c -> c
      | None ->
        let mem, updated = st.MemState.[mid]
        let mem = Map.add addr NotAConst mem
        st.MemState.[mid] <- (mem, updated)
        st.UndefinedMemories.Add addr |> ignore
        NotAConst
    else NotAConst

  let copyMem st dstid srcid =
    st.MemState.[dstid] <- st.MemState.[srcid]

  let storeToFreshMem st mDst rt addr c =
    let align = RegType.toByteWidth rt |> uint64
    let dstid = mDst.Identifier
    let mem, updated = st.MemState.[dstid]
    if (rt = st.DefaultWordSize) && (addr % align = 0UL) then
      let mem = Map.add addr c mem
      st.MemState.[dstid] <- (mem, updated)
      st.SSAWorkList.Push mDst
    elif not <| Set.isEmpty updated then st.SSAWorkList.Push mDst

  let storeToDefinedMem oldMem st mDst rt addr c =
    let align = RegType.toByteWidth rt |> uint64
    let dstid = mDst.Identifier
    let mem, updated = st.MemState.[dstid]
    if (rt = st.DefaultWordSize) && (addr % align = 0UL) then
      match Map.tryFind addr oldMem with
      | Some c' when CPValue.goingUp c' c || c' = c ->
        let mem = Map.add addr c mem
        let updated = Set.remove addr updated
        st.MemState.[dstid] <- (mem, updated)
        if not <| Set.isEmpty updated then st.SSAWorkList.Push mDst
      | Some c' ->
        let mem = Map.add addr (CPValue.meet c c') mem
        let updated = Set.add addr updated
        st.MemState.[dstid] <- (mem, updated)
        st.SSAWorkList.Push mDst
      | _ ->
        let mem = Map.add addr c mem
        let updated = Set.add addr updated
        st.MemState.[dstid] <- (mem, updated)
        st.SSAWorkList.Push mDst
    elif not <| Set.isEmpty updated then st.SSAWorkList.Push mDst

  let merge st mem1 mem2 addr =
    match Map.tryFind addr mem1, Map.tryFind addr mem2 with
    | Some c, Some c' when c = c' -> Some c
    | Some c, Some c' -> Some <| CPValue.meet c c'
    | Some _, None when st.UndefinedMemories.Contains addr -> Some NotAConst
    | Some c, None -> Some c
    | None, Some _ when st.UndefinedMemories.Contains addr -> Some NotAConst
    | None, Some c -> Some c
    | None, None when st.UndefinedMemories.Contains addr -> Some NotAConst
    | _ -> None

  let private mergeMemAux st accMem (mem, _) =
    let addrs = Map.fold (fun acc addr _ -> Set.add addr acc) Set.empty accMem
    let addrs = Map.fold (fun acc addr _ -> Set.add addr acc) addrs mem
    addrs
    |> Set.fold (fun newMem addr ->
      match merge st newMem mem addr with
      | Some c -> Map.add addr c newMem
      | None -> newMem) accMem

  /// Merge memory mapping and return true if changed.
  let mergeMem st oldMem mDst srcids =
    let dstid = mDst.Identifier
    srcids
    |> Array.choose (fun mid ->
      if mid = dstid then oldMem
      else st.MemState.TryGetValue mid |> Utils.tupleToOpt)
    |> function
      | [||] -> ()
      | arr ->
        let mem =
          Array.fold (mergeMemAux st) Map.empty arr
        let updated, needEnqueue =
          mem
          |> Map.fold (fun (newUpdated, needEnqueue) addr c ->
            match oldMem with
            | Some (oldMem, _) ->
              match Map.tryFind addr oldMem with
              | Some c' when c = c' -> Set.remove addr newUpdated, needEnqueue
              | Some _ -> Set.add addr newUpdated, true
              | None -> Set.add addr newUpdated, true
            | None -> newUpdated, true) (Set.empty, false)
        st.MemState.[dstid] <- (mem, updated)
        if needEnqueue then st.SSAWorkList.Push mDst
