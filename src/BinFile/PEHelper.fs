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

module internal B2R2.BinFile.PE.Helper

open System
open B2R2
open B2R2.BinFile
open B2R2.Monads.Maybe
open System.Reflection.PortableExecutable

/// The start offset for parsing ELF files.
let [<Literal>] startOffset = 0

let parseFormat bytes offset =
  let bs = Array.sub bytes offset (Array.length bytes - offset)
  use stream = new IO.MemoryStream (bs)
  use reader = new PEReader (stream, PEStreamOptions.Default)
  match reader.PEHeaders.CoffHeader.Machine with
  | Machine.I386 -> ISA.Init Arch.IntelX86 Endian.Little
  | Machine.Amd64 | Machine.IA64 -> ISA.Init Arch.IntelX64 Endian.Little
  | Machine.Arm -> ISA.Init Arch.ARMv7 Endian.Little
  | Machine.Arm64 -> ISA.Init Arch.AARCH64 Endian.Little
  | _ -> raise InvalidISAException

let getRawOffset (headers: PEHeaders) rva =
  let idx = headers.GetContainingSectionIndex rva
  let sHdr = headers.SectionHeaders.[idx]
  rva + sHdr.PointerToRawData - sHdr.VirtualAddress

let readStr headers (binReader: BinReader) rva =
  let rec loop acc pos =
    let byte = binReader.PeekByte pos
    if byte = 0uy then List.rev acc |> List.toArray
    else loop (byte :: acc) (pos + 1)
  if rva = 0 then ""
  else getRawOffset headers rva |> loop [] |> Text.Encoding.ASCII.GetString

let readImportDirectoryTableEntry (binReader: BinReader) headers pos =
  { ImportLookupTableRVA = binReader.PeekInt32 pos
    ForwarderChain = binReader.PeekInt32 (pos + 8)
    ImportDLLName = binReader.PeekInt32 (pos + 12) |> readStr headers binReader
    ImportAddressTableRVA = binReader.PeekInt32 (pos + 16) }

let isNULLImportDir tbl =
  tbl.ImportLookupTableRVA = 0
  && tbl.ForwarderChain = 0
  && tbl.ImportDLLName = ""
  && tbl.ImportAddressTableRVA = 0

let parseImportDirectoryTable binReader headers pos =
  let rec loop acc pos =
    let tbl = readImportDirectoryTableEntry binReader headers pos
    if isNULLImportDir tbl then acc else loop (tbl :: acc) (pos + 20)
  loop [] pos |> List.rev |> List.toArray

let parseImports (binReader: BinReader) (headers: PEHeaders) =
  match headers.PEHeader.ImportTableDirectory.RelativeVirtualAddress with
  | 0 -> [||]
  | rva ->
    getRawOffset headers rva |> parseImportDirectoryTable binReader headers

let parseILTEntry (binReader: BinReader) headers idt mask rva =
  let dllname = idt.ImportDLLName
  if rva &&& mask <> 0UL then
    ImportByOrdinal (uint16 rva |> int16, dllname)
  else
    let rva = 0x7fffffffUL &&& rva |> int
    let hint = getRawOffset headers rva |> binReader.PeekInt16
    let funname = readStr headers binReader (rva + 2)
    ImportByName (hint, funname, dllname)

let parseILT binReader headers wordSize map idt =
  let skip = if wordSize = WordSize.Bit32 then 4 else 8
  let mask =
    if wordSize = WordSize.Bit32 then 0x80000000UL else 0x8000000000000000UL
  let rec loop map rvaOffset pos =
    let rva = FileHelper.peekUIntOfType binReader wordSize pos
    if rva = 0UL then map
    else
      let entry = parseILTEntry binReader headers idt mask rva
      let map = Map.add (idt.ImportAddressTableRVA + rvaOffset) entry map
      loop map (rvaOffset + skip) (pos + skip)
  idt.ImportLookupTableRVA
  |> getRawOffset headers
  |> loop map 0

let parseImportMap binReader headers wordSize importDirTable =
  importDirTable
  |> Array.toList
  |> List.fold (parseILT binReader headers wordSize) Map.empty

let readExportDirectoryTableEntry (binReader: BinReader) headers pos =
  { ExportDLLName = binReader.PeekInt32 (pos + 12) |> readStr headers binReader
    OrdinalBase = binReader.PeekInt32 (pos + 16)
    AddressTableEntries = binReader.PeekInt32 (pos + 20)
    NumNamePointers = binReader.PeekInt32 (pos + 24)
    ExportAddressTableRVA = binReader.PeekInt32 (pos + 28)
    NamePointerRVA = binReader.PeekInt32 (pos + 32)
    OrdinalTableRVA = binReader.PeekInt32 (pos + 36) }

let parseEAT (binReader: BinReader) headers (sec: SectionHeader) edt =
  let lowerbound = sec.VirtualAddress
  let upperbound = sec.VirtualAddress + sec.VirtualSize
  let getEntry rva =
    if rva < lowerbound || rva > upperbound then ExportRVA rva
    else ForwarderRVA rva
  let rec loop acc cnt pos =
    if cnt = 0 then List.rev acc |> List.toArray
    else let rva = binReader.PeekInt32 (pos)
         loop (getEntry rva :: acc) (cnt - 1) (pos + 4)
  let offset = edt.ExportAddressTableRVA |> getRawOffset headers
  loop [] edt.AddressTableEntries offset

/// Parse Export Name Pointer Table (ENPT).
let parseENPT (binReader: BinReader) headers edt =
  let rec loop acc cnt pos1 pos2 =
    if cnt = 0 then acc
    else let str = binReader.PeekInt32 (pos1) |> readStr headers binReader
         let ord = binReader.PeekInt16 (pos2)
         loop ((str, ord) :: acc) (cnt - 1) (pos1 + 4) (pos2 + 2)
  let offset1 = edt.NamePointerRVA |> getRawOffset headers
  let offset2 = edt.OrdinalTableRVA |> getRawOffset headers
  loop [] edt.NumNamePointers offset1 offset2

let buildExportTable binReader headers sec edt =
  let addrtbl = parseEAT binReader headers sec edt
  let folder map (name, ord) =
    match addrtbl.[int ord] with
    | ExportRVA rva ->
      let rva = uint64 rva + headers.PEHeader.ImageBase
      Map.add rva name map
    | _ -> map
  parseENPT binReader headers edt
  |> List.fold folder Map.empty

let parseExports binReader (headers: PEHeaders) =
  match headers.PEHeader.ExportTableDirectory.RelativeVirtualAddress with
  | 0 -> Map.empty
  | rva ->
    let sec = headers.SectionHeaders.[headers.GetContainingSectionIndex rva]
    getRawOffset headers rva
    |> readExportDirectoryTableEntry binReader headers
    |> buildExportTable binReader headers sec

let getWordSize (peHeader: PEHeader) =
  match peHeader.Magic with
  | PEMagic.PE32 -> WordSize.Bit32
  | PEMagic.PE32Plus -> WordSize.Bit64
  | _ -> raise InvalidWordSizeException

let pdbTypeToSymbKind = function
  | SymFlags.Function -> SymbolKind.FunctionType
  | _ -> SymbolKind.NoType

let pdbSymbolToSymbol (sym: PESymbol) =
  { Address = sym.Address
    Name = sym.Name
    Kind = pdbTypeToSymbKind sym.Flags
    Target = TargetKind.StaticSymbol
    LibraryName = "" }

let parsePDB pdbBytes =
  let reader = BinReader.Init (pdbBytes)
  if PDB.isPDBHeader reader startOffset then ()
  else raise FileFormatMismatchException
  PDB.parse reader startOffset

let getPDBSymbols (execpath: string) = function
  | None ->
    let pdbPath = IO.Path.ChangeExtension (execpath, "pdb")
    if IO.File.Exists pdbPath then IO.File.ReadAllBytes pdbPath |> parsePDB
    else []
  | Some rawpdb -> parsePDB rawpdb

let buildPDBInfo (headers: PEHeaders) symbs =
  let baseaddr = headers.PEHeader.ImageBase
  let genSymbol (addrMap, nameMap, lst) (sec: SectionHeader) (sym: PESymbol) =
    let addr = baseaddr + uint64 sec.VirtualAddress + uint64 sym.Address
    let sym = { sym with Address = addr }
    Map.add addr sym addrMap,
    Map.add sym.Name sym nameMap,
    sym :: lst
  let folder acc sym =
    let secNum = int sym.Segment - 1
    match Seq.tryItem secNum headers.SectionHeaders with
    | Some sec -> genSymbol acc sec sym
    | None -> acc
  let mAddr, mName, lst = symbs |> List.fold folder (Map.empty, Map.empty, [])
  {
    SymbolByAddr = mAddr
    SymbolByName = mName
    SymbolArray = List.rev lst |> List.toArray
  }

let parsePE execpath rawpdb binReader (peReader: PEReader) =
  let headers = peReader.PEHeaders
  let wordSize = getWordSize headers.PEHeader
  let importDirTables = parseImports binReader headers
  let exportDirTables = parseExports binReader headers
  let importMap = parseImportMap binReader headers wordSize importDirTables
  let pdbInfo = getPDBSymbols execpath rawpdb |> buildPDBInfo headers
  {
    PEHeaders = headers
    SectionHeaders = peReader.PEHeaders.SectionHeaders |> Seq.toArray
    ImportMap= importMap
    ExportMap = exportDirTables
    WordSize = wordSize
    PDB = pdbInfo
  }

let findSymFromPDB addr pdb =
  Map.tryFind addr pdb.SymbolByAddr
  >>= (fun s -> Some s.Name)

let findSymFromIAT addr pe =
  let rva = int (addr - pe.PEHeaders.PEHeader.ImageBase)
  match Map.tryFind rva pe.ImportMap with
  | Some (ImportByName (_, n, _)) -> Some n
  | _ -> None

let findSymFromEAT addr pe () =
  let rva = addr - pe.PEHeaders.PEHeader.ImageBase
  match Map.tryFind rva pe.ExportMap with
  | Some n -> Some n
  | _ -> None

let tryFindFunctionSymbolName pe addr =
  match findSymFromPDB addr pe.PDB with
  | None ->
    findSymFromIAT addr pe
    |> Monads.OrElse.bind (findSymFromEAT addr pe)
  | name -> name

let getImportSymbols pe =
  let conv acc addr imp =
    match imp with
    | ImportByOrdinal (_, dllname) ->
      { Address = uint64 addr + pe.PEHeaders.PEHeader.ImageBase
        Name = ""
        Kind = SymbolKind.ExternFunctionType
        Target = TargetKind.DynamicSymbol
        LibraryName = dllname } :: acc
    | ImportByName (_, funname, dllname) ->
      { Address = uint64 addr + pe.PEHeaders.PEHeader.ImageBase
        Name = funname
        Kind = SymbolKind.ExternFunctionType
        Target = TargetKind.DynamicSymbol
        LibraryName = dllname } :: acc
  pe.ImportMap
  |> Map.fold conv []
  |> List.rev

let getSymbolKindBySectionIndex pe idx =
  let ch = pe.PEHeaders.SectionHeaders.[idx].SectionCharacteristics
  if ch.HasFlag SectionCharacteristics.MemExecute then SymbolKind.FunctionType
  else SymbolKind.ObjectType

let getExportSymbols pe =
  let conv acc addr exp =
    let rva = int (addr - pe.PEHeaders.PEHeader.ImageBase)
    match pe.PEHeaders.GetContainingSectionIndex rva with
    | -1 -> acc
    | idx ->
      { Address = addr
        Name = exp
        Kind = getSymbolKindBySectionIndex pe idx
        Target = TargetKind.DynamicSymbol
        LibraryName = "" } :: acc
  pe.ExportMap
  |> Map.fold conv []

let getAllDynamicSymbols pe =
  let isym = getImportSymbols pe
  let esym = getExportSymbols pe
  List.append isym esym

let secFlagToSectionKind (flags: SectionCharacteristics) =
  if flags.HasFlag SectionCharacteristics.MemExecute then
    SectionKind.ExecutableSection
  elif flags.HasFlag SectionCharacteristics.MemWrite then
    SectionKind.WritableSection
  else
    SectionKind.ExtraSection

let secHdrToSection pe (sec: SectionHeader) =
  { Address = uint64 sec.VirtualAddress + pe.PEHeaders.PEHeader.ImageBase
    Kind = secFlagToSectionKind sec.SectionCharacteristics
    Size = sec.VirtualSize |> uint64
    Name = sec.Name }

let initPE bytes execpath rawpdb =
  try
    let bs = Array.sub bytes startOffset (Array.length bytes - startOffset)
    let binReader = BinReader.Init (bs)
    use stream = new IO.MemoryStream (bs)
    use peReader = new PEReader (stream, PEStreamOptions.Default)
    parsePE execpath rawpdb binReader peReader
  with e ->
    printfn "%s" <| e.ToString ()
    raise FileFormatMismatchException

// vim: set tw=80 sts=2 sw=2:
