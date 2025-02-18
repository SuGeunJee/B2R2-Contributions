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

namespace B2R2.MiddleEnd.BinGraph

open System.Collections.Generic

/// Dominator interface.
type IDominator<'V, 'E when 'V: equality and 'E: equality> =
  /// Get the dominators of a vertex.
  abstract Dominators: IVertex<'V> -> IEnumerable<IVertex<'V>>

  /// Get the immediate dominator of a vertex, which is the vertex that strictly
  /// dominates the vertex and is closest to the vertex. If the vertex is a root
  /// vertex, then the immediate dominator is `null`.
  abstract ImmediateDominator: IVertex<'V> -> IVertex<'V>

  /// Get the dominance frontier of a vertex, which is the set of all vertices
  /// that are not strictly dominated by the vertex but are reachable from the
  /// vertex.
  abstract DominanceFrontier: IVertex<'V> -> IEnumerable<IVertex<'V>>

  /// Get the dominator tree of the given implementation type.
  abstract DominatorTree: unit -> DominatorTree<'V, 'E>

  /// Get the post-dominators of a vertex.
  abstract PostDominators: IVertex<'V> -> IEnumerable<IVertex<'V>>

  /// Get the immediate post-dominator of a vertex. If the vertex is a root
  /// vertex in the reversed graph, then the immediate post-dominator is `null`.
  abstract ImmediatePostDominator: IVertex<'V> -> IVertex<'V>

/// Dominator tree interface. A dominator tree is a tree where each node's
/// children are those nodes it immediately dominates. This function returns a
/// map from a node to its children in the dom tree.
and DominatorTree<'V, 'E when 'V: equality
                          and 'E: equality>
  public (g: IDiGraphAccessible<'V, 'E>, dom: IDominator<'V, 'E>) =

  let domTree = Dictionary<IVertex<'V>, list<IVertex<'V>>> ()

  do g.IterVertex (fun v ->
       let idom = dom.ImmediateDominator v
       if isNull idom then ()
       elif domTree.ContainsKey idom then domTree[idom] <- v :: domTree[idom]
       else domTree[idom] <- [ v ])

  /// Get the children of a vertex in the dominator tree.
  member _.GetChildren (v: IVertex<'V>) =
    match domTree.TryGetValue v with
    | true, children -> children
    | false, _ -> []
