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

namespace B2R2.MiddleEnd.BinGraph.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open B2R2.MiddleEnd.BinGraph
open B2R2.MiddleEnd.BinGraph.Tests.Examples

[<TestClass>]
type Basic () =
  let sum acc (v: IVertex<_>) = v.VData + acc

  let inc acc (edge: Edge<_, _>) = acc + edge.Label

  static member GraphTypes = [| [| box Persistent |]; [| box Imperative |] |]

  [<TestMethod>]
  [<DynamicData(nameof Basic.GraphTypes)>]
  member __.``DiGraph Traversal Test 1`` (t) =
    let g, vmap = digraph1 t
    let root = vmap[1]
    let s1 = Traversal.foldPostorder g [root] sum 0
    let s2 = Traversal.foldRevPostorder g [root] sum 0
    let s3 = Traversal.foldPreorder g [root] sum 0
    let s4 = g.FoldVertex sum 0
    let s5 = g.FoldEdge inc 0
    Assert.AreEqual (21, s1)
    Assert.AreEqual (21, s2)
    Assert.AreEqual (21, s3)
    Assert.AreEqual (21, s4)
    Assert.AreEqual (28, s5)

  [<TestMethod>]
  [<DynamicData(nameof Basic.GraphTypes)>]
  member __.``DiGraph Traversal Test 2`` (t) =
    let g, vmap = digraph1 t
    let root = vmap[1]
    let s1 =
      Traversal.foldPostorder g [root] (fun acc v -> v.VData :: acc) []
      |> List.rev |> List.toArray
    let s2 =
      Traversal.foldPreorder g [root] (fun acc v -> v.VData :: acc) []
      |> List.rev |> List.toArray
    CollectionAssert.AreEqual ([| 5; 3; 4; 6; 2; 1 |], s1)
    CollectionAssert.AreEqual ([| 1; 2; 3; 5; 4; 6 |], s2)

  [<TestMethod>]
  [<DynamicData(nameof Basic.GraphTypes)>]
  member __.``DiGraph Traversal Test 3`` (t) =
    let g, vmap = digraph3 t
    let root = vmap[1]
    let s1 =
      Traversal.foldPostorder g [root] (fun acc v -> v.VData :: acc) []
      |> List.rev |> List.toArray
    let s2 =
      Traversal.foldPreorder g [root] (fun acc v -> v.VData :: acc) []
      |> List.rev |> List.toArray
    CollectionAssert.AreEqual ([| 4; 2; 5; 3; 1 |], s1)
    CollectionAssert.AreEqual ([| 1; 2; 4; 3; 5 |], s2)

  [<TestMethod>]
  [<DynamicData(nameof Basic.GraphTypes)>]
  member __.``DiGraph Removal Test`` (t) =
    let g1, g1vmap = digraph1 t
    let g1root = g1vmap[1]
    let g2 = g1.Clone ()
    let g2root = g2.FindVertexByData g1root.VData
    let g2 = g2.FindVertexByData 3 |> g2.RemoveVertex
    let s1 = Traversal.foldPreorder g1 [g1root] sum 0
    let s2 = Traversal.foldPreorder g2 [g2root] sum 0
    Assert.AreEqual (6, g1.Size)
    Assert.AreEqual (5, g2.Size)
    Assert.AreEqual (21, s1)
    Assert.AreEqual (18, s2)

  [<TestMethod>]
  [<DynamicData(nameof Basic.GraphTypes)>]
  member __.``Graph Transposition Test`` (t) =
    let g1, g1vmap = digraph1 t
    let g1root = g1vmap[1]
    let g2 = g1.Reverse ()
    let g2root = g2.FindVertexByData 6
    let s1 = Traversal.foldPreorder g1 [g1root] sum 0
    let s2 = Traversal.foldPreorder g2 [g2root] sum 0
    let lst =
      g2.FoldEdge (fun acc e -> (e.First.VData, e.Second.VData) :: acc) []
    let edges = List.sort lst |> List.toArray
    let solution = [| (2, 1); (2, 5); (3, 2); (4, 2); (5, 3); (5, 4); (6, 2) |]
    Assert.AreEqual (6, g1.Size)
    Assert.AreEqual (6, g2.Size)
    Assert.AreEqual (21, s1)
    Assert.AreEqual (21, s2)
    CollectionAssert.AreEqual (edges, solution)
