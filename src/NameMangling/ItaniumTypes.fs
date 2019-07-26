(*
  B2R2 - the Next-Generation Reversing Platform

  Author: Mehdi Aghakishiyev <agakisiyev.mehdi@gmail.com>
          Sang Kil Cha <sangkilc@kaist.ac.kr>

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

namespace B2R2.NameMangling

type BuiltinTypeIndicator =
  | Void
  | WChar
  | Boolean
  | Char
  | SignedChar
  | UnsignedChar
  | Short
  | UnsignedShort
  | Int
  | UnsignedInt
  | Long
  | UnsignedLong
  | LongLong
  | UnsignedLongLong
  | Int128
  | UnsignedInt128
  | Float
  | Double
  | LongDouble
  | Float128
  | Ellipsis
  | Decimal64
  | Decimal128
  | Decimal32
  | Char16
  | Auto
  | DecltypeNullptr
  | Char32
  | Half
  | Unknown

module BuiltinTypeIndicator =
  let ofString = function
    | "v" -> Void
    | "w" -> WChar
    | "b" -> Boolean
    | "c" -> Char
    | "a" -> SignedChar
    | "h" -> UnsignedChar
    | "s" -> Short
    | "t" -> UnsignedShort
    | "i" -> Int
    | "j" -> UnsignedInt
    | "l" -> Long
    | "m" -> UnsignedLong
    | "x" -> LongLong
    | "y" -> UnsignedLongLong
    | "n" -> Int128
    | "o" -> UnsignedInt128
    | "f" -> Float
    | "d" -> Double
    | "e" -> LongDouble
    | "g" -> Float128
    | "z" -> Ellipsis
    | "Dd" -> Decimal64
    | "De" -> Decimal128
    | "Df" -> Decimal32
    | "Ds" -> Char16
    | "Da" -> Auto
    | "Dn" -> DecltypeNullptr
    | "Di" -> Char32
    | "Dh" -> Half
    | _    -> Unknown

  let toString = function
    | Void -> "void"
    | WChar -> "wchar_t"
    | Boolean -> "bool"
    | Char -> "char"
    | SignedChar -> "signed char"
    | UnsignedChar -> "unsigned char"
    | Short -> "short"
    | UnsignedShort -> "unsigned short"
    | Int -> "int"
    | UnsignedInt -> "unsigned int"
    | Long -> "long"
    | UnsignedLong -> "unsigned long"
    | LongLong -> "long long"
    | UnsignedLongLong -> "unsigned long long"
    | Int128 -> "__int128"
    | UnsignedInt128 -> "unsigned __int128"
    | Float -> "float"
    | Double -> "double"
    | LongDouble -> "long double"
    | Float128 -> "float"
    | Ellipsis -> "ellipsis"
    | Decimal64 -> "decimal64"
    | Decimal128 -> "decimal128"
    | Decimal32 -> "decimal32"
    | Char16 -> "char16_t"
    | Auto -> "auto"
    | DecltypeNullptr -> "decltype(nullptr)"
    | Char32 -> "char32_t"
    | Half -> "half"
    | Unknown -> "???"

type Sxabbreviation =
  | Std
  | StdAllocator
  | StdBasicString
  | StdBasicStringT
  | StdBasicIstream
  | StdBasicOstream
  | StdBasicIOStream
  | Unknown

module Sxabbreviation =
  let ofString = function
    | "St" -> Std
    | "Sa" -> StdAllocator
    | "Sb" -> StdBasicString
    | "Ss" -> StdBasicStringT
    | "Si" -> StdBasicIstream
    | "So" -> StdBasicOstream
    | "Sd" -> StdBasicIOStream
    | _    -> Unknown

  let toString = function
    | Std -> "std"
    | StdAllocator -> "std::allocator"
    | StdBasicString -> "std::basic_string"
    | StdBasicStringT ->
      "std::basic_string<char, std::char_traits<char>, std::allocator<char>>"
    | StdBasicIstream -> "std::basic_istream<char, std::char_traits<char>>"
    | StdBasicOstream -> "std::basic_ostream<char, std::char_traits<char>>"
    | StdBasicIOStream -> "std::basic_iostream<char, std::char_traits<char>>"
    | Unknown -> "???"

  let get = function
    | StdAllocator -> "allocator"
    | StdBasicString -> "basic_string"
    | StdBasicStringT ->
      "basic_string"
    | StdBasicIstream -> "basic_istream"
    | StdBasicOstream -> "basic_ostream"
    | StdBasicIOStream -> "basic_iostream"
    | _ -> ""

type OperatorIndicator =
  | New
  | NewList
  | Delete
  | DeleteList
  | UnaryPlus
  | UnaryMinus
  | Address
  | Pointer
  | BitwiseNot
  | BinaryPlus
  | BinaryMinus
  | BinaryMultiply
  | BinaryDivide
  | BinaryRemainder
  | BitwiseAnd
  | BitwiseOr
  | BitwiseXor
  | Assignment
  | PlusAssign
  | MinusAssign
  | MultiplyAssign
  | DivideAssign
  | RemainderAssign
  | BitwiseAndAssign
  | BitwiseOrAssign
  | BitwiseXorAssign
  | ShiftLeft
  | ShiftRight
  | ShiftLeftAssign
  | ShiftRightAssign
  | Equality
  | InEquality
  | LessThan
  | GreaterThan
  | LessThanorEqual
  | GreaterThanorEqual
  | LogicalNot
  | LogicalAnd
  | LogicalOr
  | Increment
  | Decrement
  | Comma
  | PointertoMember
  | Member
  | Parantheses
  | Brackets
  | DoubleColon
  | Unknown

module OperatorIndicator =
  let ofString = function
    | "nw" -> New
    | "na" -> NewList
    | "dl" -> Delete
    | "da" -> DeleteList
    | "ps" -> UnaryPlus
    | "ng" -> UnaryMinus
    | "ad" -> Address
    | "de" -> Pointer
    | "co" -> BitwiseNot
    | "pl" -> BinaryPlus
    | "mi" -> BinaryMinus
    | "ml" -> BinaryMultiply
    | "dv" -> BinaryDivide
    | "rm" -> BinaryRemainder
    | "an" -> BitwiseAnd
    | "or" -> BitwiseOr
    | "eo" -> BitwiseXor
    | "aS" -> Assignment
    | "pL" -> PlusAssign
    | "mI" -> MinusAssign
    | "mL" -> MultiplyAssign
    | "dV" -> DivideAssign
    | "rM" -> RemainderAssign
    | "aN" -> BitwiseAndAssign
    | "oR" -> BitwiseOrAssign
    | "eO" -> BitwiseXorAssign
    | "ls" -> ShiftLeft
    | "rs" -> ShiftRight
    | "lS" -> ShiftLeftAssign
    | "rS" -> ShiftRightAssign
    | "eq" -> Equality
    | "ne" -> InEquality
    | "lt" -> LessThan
    | "gt" -> GreaterThan
    | "le" -> LessThanorEqual
    | "ge" -> GreaterThanorEqual
    | "nt" -> LogicalNot
    | "aa" -> LogicalAnd
    | "oo" -> LogicalOr
    | "pp" -> Increment
    | "mm" -> Decrement
    | "cm" -> Comma
    | "pm" -> PointertoMember
    | "pt" -> Member
    | "cl" -> Parantheses
    | "ix" -> Brackets
    | "sr" -> DoubleColon
    | _    -> Unknown

  let toString = function
    | New -> " new"
    | NewList -> " new[]"
    | Delete -> " delete"
    | DeleteList -> " delete[]"
    | UnaryPlus -> "+"
    | UnaryMinus -> "-"
    | Address -> "&"
    | Pointer -> "*"
    | BitwiseNot -> "~"
    | BinaryPlus -> "+"
    | BinaryMinus -> "-"
    | BinaryMultiply -> "*"
    | BinaryDivide -> "/"
    | BinaryRemainder -> "%"
    | BitwiseAnd -> "&"
    | BitwiseOr -> "|"
    | BitwiseXor -> ""
    | Assignment -> "="
    | PlusAssign -> "+="
    | MinusAssign -> "-="
    | MultiplyAssign -> "*="
    | DivideAssign -> "/="
    | RemainderAssign -> "%="
    | BitwiseAndAssign -> "&="
    | BitwiseOrAssign -> "|="
    | BitwiseXorAssign -> "="
    | ShiftLeft -> "<<"
    | ShiftRight -> ">>"
    | ShiftLeftAssign -> "<<="
    | ShiftRightAssign -> ">>="
    | Equality -> "=="
    | InEquality -> "!="
    | LessThan -> "<"
    | GreaterThan -> ">"
    | LessThanorEqual -> "<="
    | GreaterThanorEqual -> ">="
    | LogicalNot -> "!"
    | LogicalAnd -> "&&"
    | LogicalOr -> "||"
    | Increment -> "++"
    | Decrement -> "--"
    | Comma -> ","
    | PointertoMember -> "->*"
    | Member -> "->"
    | Parantheses -> "()"
    | Brackets -> "[]"
    | DoubleColon -> "::"
    | Unknown -> "???"

type ConstructorDestructor =
  | Constructor
  | Destructor
  | Unknown

module ConstructorDestructor =
  let ofChar = function
    | 'C' -> Constructor
    | 'D' -> Destructor
    | _   -> Unknown

  let toChar = function
    | Constructor -> ""
    | Destructor -> "~"
    | Unknown -> "???"

/// Qualifiers: const, volatile. ConstaVolatile defines const volatile together.
type ConsTandVolatile =
  | Const
  | Volatile
  | ConstaVolatile
  | Unknown

module ConsTandVolatile =
  let ofChar = function
    | ('K', None) -> Const
    | ('V', None) -> Volatile
    | ('V', Some 'K') -> ConstaVolatile
    | _ -> Unknown

  let toString = function
    | Const -> " const"
    | Volatile -> " volatile"
    | ConstaVolatile -> " const volatile"
    | Unknown -> "???"

/// Restrict qualifier including optional const and volatile qualifier.
/// It is consisted of quadruple, restrict, optional const and volatile and
/// pointer. Nothing implies no qualifier and pointer, JustPointer implies
/// only pointer.
type RestrictQualifier =
  | Nothing
  | Restrict
  | RestrictConst
  | RestrictVolatile
  | RestrictVolatileConst
  | Unknown

module RestrictQualifier =
  let ofTuple = function
    | ("", None, None) -> Nothing
    | ("r", None, None) -> Restrict
    | ("r", Some _, None) -> RestrictVolatile
    | ("r", None, Some _) -> RestrictConst
    | ("r", Some _, Some _) -> RestrictVolatileConst
    | _ -> Unknown

  let toString = function
    | Nothing -> ""
    | Restrict -> " __restrict__"
    | RestrictConst -> " const __restrict__"
    | RestrictVolatile -> " volatile __restrict__"
    | RestrictVolatileConst -> " const volatile __restrict__"
    | Unknown -> "???"

type ReferenceQualifier =
  | Empty
  | LValueReference
  | RvalueReference
  | Unknown

module ReferenceQualifier =
  let ofString = function
    | "" -> Empty
    | "R" -> LValueReference
    | "O" -> RvalueReference
    | _   -> Unknown

  let toString = function
    | LValueReference -> "&"
    | RvalueReference -> "&&"
    | Empty -> ""
    | Unknown -> "???"

type ItaniumExpr =
  /// Dummy type.
  | Dummy of string

  /// Number.
  | Num of int

  /// Name composed of string.
  | Name of string

  /// Sx abbreviation. For example, St = std, Sa = std :: allocator.
  | Sxsubstitution of Sxabbreviation

  /// Operators.
  | Operators of OperatorIndicator

  /// Sx abrreviation and name.
  | Sxname of ItaniumExpr * ItaniumExpr

  /// Sx abbreviation and operator.
  | Sxoperator of ItaniumExpr * ItaniumExpr

  /// Builtin extended types by vendors.
  | Vendor of string

  /// Builtin types.
  | BuiltinType of BuiltinTypeIndicator

  /// Literals encoded with their type and value.
  | Literal of ItaniumExpr * ItaniumExpr

  /// Single pointer encoded by character 'P'.
  | SingleP of char

  /// Many pointers encoded by consecutive 'P's.
  | Pointer of ItaniumExpr list

  /// Function, template, operator or expression argument.
  | SingleArg of ItaniumExpr

  /// Many arguments.
  | Arguments of ItaniumExpr list

  /// Nested name composed of optional qualifiers and
  /// list of names, Sx abbreviation, templates and operator names.
  | NestedName of ItaniumExpr * ItaniumExpr list

  /// Template composed of name and arguments.
  | Template of ItaniumExpr * ItaniumExpr

  /// Function with name (name, template, nestedname), return
  /// and arguments (if return is not included in mangling, second expression
  /// will be part of arguments).
  | Function of ItaniumExpr * ItaniumExpr * ItaniumExpr

  /// Constructors and Destructors.
  | ConsOrDes of ConstructorDestructor

  /// Function Pointer is composed of pointers and optional qualifiers, return
  /// and arguments.
  | FunctionPointer of ItaniumExpr * ItaniumExpr * ItaniumExpr

  /// Operator and arguments.
  | SimpleOP of ItaniumExpr * ItaniumExpr

  /// Unary expression is composed of operator and single argument.
  | UnaryExpr of ItaniumExpr * ItaniumExpr

  /// Binary expression is composed operator and two arguments.
  | BinaryExpr of ItaniumExpr * ItaniumExpr * ItaniumExpr

  /// Constant and volatile qualifiers. This includes, const, volatile and
  /// const volatile together.
  | CVqualifier of ConsTandVolatile

  /// CV qualifiers followed by pointers. For example, const*.
  | ConstVolatile of ItaniumExpr * ItaniumExpr

  /// Reference qualifiers, & and &&.
  | Reference of ReferenceQualifier

  /// CV qualifiers followed by reference in mangled form. This form is only
  /// used in nested names.
  | CVR of ItaniumExpr * ItaniumExpr

  /// Arguments with pointer (string part, single pointer) and CV qualifiers.
  /// The string part is a single character string "P".
  /// It is defined like this for making substitution easier.
  | PointerArg of string * ItaniumExpr option * ItaniumExpr

  /// Arguments with reference qualifiers.
  /// Second expression can be PointerArg too.
  | RefArg of ItaniumExpr * ItaniumExpr

  /// CV qualifiers followed by reference qualifier in code.
  /// For example, const&.
  | ReferenceArg of ItaniumExpr * ItaniumExpr option

  /// Beginning of function pointer in mangling which is
  /// consisted of qualifiers and pointers. First list expression is
  /// qualifiers with pointers, such as const*, second expression is successive
  /// pointers (For example, *****).
  | FunctionBegin of ItaniumExpr list option * ItaniumExpr

  /// Restrict qualifer which is applied to pointer arguments. There can be
  /// const or volatile between pointer sign (*) and restrict qualifier.
  | Restrict of RestrictQualifier

  /// Array arguments encoded with their size and type.
  | ArrayPointer of int list * ItaniumExpr

  /// Created for special case of CV qualifiers, first element is qualifier.
  | Functionarg of ItaniumExpr option * ItaniumExpr

type ItaniumUserState = {
  Namelist: ItaniumExpr List
  TemplateArgList : ItaniumExpr list
  Carry : ItaniumExpr
}
with
  static member Default =
    { Namelist = []
      TemplateArgList = []
      Carry = Dummy ""}

type ItaniumParser<'a> = FParsec.Primitives.Parser<'a, ItaniumUserState>

