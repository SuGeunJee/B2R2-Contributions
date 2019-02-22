(*
  B2R2 - the Next-Generation Reversing Platform

  Author: Sang Kil Cha <sangkilc@kaist.ac.kr>

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

namespace B2R2.Monads

/// Maybe represents computation expressions that might go wrong.
module Maybe = begin

  /// A builder for Maybe computation expression.
  type MaybeBuilder () =
    member __.Return (x) = Some x
    member __.ReturnFrom (x: 'a option) = x
    member __.Bind (m, f) = Option.bind f m
    member __.Zero () = None

  let maybe = MaybeBuilder ()

  let inline (>>=) m f = Option.bind f m

end

/// OrElse represents computation expressions that capture the result until
/// successful.
module OrElse = begin

  let bind f m =
    match m with
    | None -> f ()
    | Some _ -> m

  /// A builder for OrElse computation expression.
  type OrElseBuilder () =
    member __.ReturnFrom (x) = x
    member __.YieldFrom (x) = x
    member __.Bind (m, f) = bind f m
    member __.Delay (f) = f
    member __.Run (f) = f ()
    member __.Combine (m, f) = bind f m

  let orElse = OrElseBuilder ()

end

// vim: set tw=80 sts=2 sw=2:
