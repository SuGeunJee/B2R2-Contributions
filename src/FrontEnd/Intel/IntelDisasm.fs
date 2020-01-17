(*
  B2R2 - the Next-Generation Reversing Platform

  Author: Sang Kil Cha <sangkilc@kaist.ac.kr>
          DongYeop Oh <oh51dy@kaist.ac.kr>
          Seung Il Jung <sijung@kaist.ac.kr>

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

module internal B2R2.FrontEnd.Intel.Disasm

open B2R2
open B2R2.BinFile
open B2R2.FrontEnd

let regToStr = function
  | R.RAX -> "rax"
  | R.RBX -> "rbx"
  | R.RCX -> "rcx"
  | R.RDX -> "rdx"
  | R.RSP -> "rsp"
  | R.RBP -> "rbp"
  | R.RSI -> "rsi"
  | R.RDI -> "rdi"
  | R.EAX -> "eax"
  | R.EBX -> "ebx"
  | R.ECX -> "ecx"
  | R.EDX -> "edx"
  | R.ESP -> "esp"
  | R.EBP -> "ebp"
  | R.ESI -> "esi"
  | R.EDI -> "edi"
  | R.AX -> "ax"
  | R.BX -> "bx"
  | R.CX -> "cx"
  | R.DX -> "dx"
  | R.SP -> "sp"
  | R.BP -> "bp"
  | R.SI -> "si"
  | R.DI -> "di"
  | R.AL -> "al"
  | R.BL -> "bl"
  | R.CL -> "cl"
  | R.DL -> "dl"
  | R.AH -> "ah"
  | R.BH -> "bh"
  | R.CH -> "ch"
  | R.DH -> "dh"
  | R.R8 -> "r8"
  | R.R9 -> "r9"
  | R.R10 -> "r10"
  | R.R11 -> "r11"
  | R.R12 -> "r12"
  | R.R13 -> "r13"
  | R.R14 -> "r14"
  | R.R15 -> "r15"
  | R.R8D -> "r8d"
  | R.R9D -> "r9d"
  | R.R10D -> "r10d"
  | R.R11D -> "r11d"
  | R.R12D -> "r12d"
  | R.R13D -> "r13d"
  | R.R14D -> "r14d"
  | R.R15D -> "r15d"
  | R.R8W -> "r8w"
  | R.R9W -> "r8w"
  | R.R10W -> "r10w"
  | R.R11W -> "r11w"
  | R.R12W -> "r12w"
  | R.R13W -> "r13w"
  | R.R14W -> "r14w"
  | R.R15W -> "r15w"
  | R.R8L -> "r8l"
  | R.R9L -> "r9l"
  | R.R10L -> "r10l"
  | R.R11L -> "r11l"
  | R.R12L -> "r12l"
  | R.R13L -> "r13l"
  | R.R14L -> "r14l"
  | R.R15L -> "r15l"
  | R.SPL -> "spl"
  | R.BPL -> "bpl"
  | R.SIL -> "sil"
  | R.DIL -> "dil"
  | R.EIP -> "eip"
  | R.RIP -> "rip"
  | R.ST0 -> "st(0)"
  | R.ST1 -> "st(1)"
  | R.ST2 -> "st(2)"
  | R.ST3 -> "st(3)"
  | R.ST4 -> "st(4)"
  | R.ST5 -> "st(5)"
  | R.ST6 -> "st(6)"
  | R.ST7 -> "st(7)"
  | R.MM0 -> "mm0"
  | R.MM1 -> "mm1"
  | R.MM2 -> "mm2"
  | R.MM3 -> "mm3"
  | R.MM4 -> "mm4"
  | R.MM5 -> "mm5"
  | R.MM6 -> "mm6"
  | R.MM7 -> "mm7"
  | R.XMM0 -> "xmm0"
  | R.XMM1 -> "xmm1"
  | R.XMM2 -> "xmm2"
  | R.XMM3 -> "xmm3"
  | R.XMM4 -> "xmm4"
  | R.XMM5 -> "xmm5"
  | R.XMM6 -> "xmm6"
  | R.XMM7 -> "xmm7"
  | R.XMM8 -> "xmm8"
  | R.XMM9 -> "xmm9"
  | R.XMM10 -> "xmm10"
  | R.XMM11 -> "xmm11"
  | R.XMM12 -> "xmm12"
  | R.XMM13 -> "xmm13"
  | R.XMM14 -> "xmm14"
  | R.XMM15 -> "xmm15"
  | R.YMM0 -> "ymm0"
  | R.YMM1 -> "ymm1"
  | R.YMM2 -> "ymm2"
  | R.YMM3 -> "ymm3"
  | R.YMM4 -> "ymm4"
  | R.YMM5 -> "ymm5"
  | R.YMM6 -> "ymm6"
  | R.YMM7 -> "ymm7"
  | R.YMM8 -> "ymm8"
  | R.YMM9 -> "ymm9"
  | R.YMM10 -> "ymm10"
  | R.YMM11 -> "ymm11"
  | R.YMM12 -> "ymm12"
  | R.YMM13 -> "ymm13"
  | R.YMM14 -> "ymm14"
  | R.YMM15 -> "ymm15"
  | R.ZMM0 -> "zmm0"
  | R.ZMM1 -> "zmm1"
  | R.ZMM2 -> "zmm2"
  | R.ZMM3 -> "zmm3"
  | R.ZMM4 -> "zmm4"
  | R.ZMM5 -> "zmm5"
  | R.ZMM6 -> "zmm6"
  | R.ZMM7 -> "zmm7"
  | R.ZMM8 -> "zmm8"
  | R.ZMM9 -> "zmm9"
  | R.ZMM10 -> "zmm10"
  | R.ZMM11 -> "zmm11"
  | R.ZMM12 -> "zmm12"
  | R.ZMM13 -> "zmm13"
  | R.ZMM14 -> "zmm14"
  | R.ZMM15 -> "zmm15"
  | R.CS -> "cs"
  | R.DS -> "ds"
  | R.SS -> "ss"
  | R.ES -> "es"
  | R.FS -> "fs"
  | R.GS -> "gs"
  | R.CSBase -> "CSBase"
  | R.DSBase -> "DSBase"
  | R.ESBase -> "ESBase"
  | R.FSBase -> "FSBase"
  | R.GSBase -> "GSBase"
  | R.SSBase -> "SSBase"
  | R.CR0 -> "cr0"
  | R.CR2 -> "cr2"
  | R.CR3 -> "cr3"
  | R.CR4 -> "cr4"
  | R.CR8 -> "cr8"
  | R.DR0 -> "dr0"
  | R.DR1 -> "dr1"
  | R.DR2 -> "dr2"
  | R.DR3 -> "dr3"
  | R.DR6 -> "dr6"
  | R.DR7 -> "dr7"
  | R.BND0 -> "bnd0"
  | R.BND1 -> "bnd1"
  | R.BND2 -> "bnd2"
  | R.BND3 -> "bnd3"
  | R.OF -> "of"
  | R.DF -> "df"
  | R.IF -> "if"
  | R.TF -> "tf"
  | R.SF -> "sf"
  | R.ZF -> "zf"
  | R.AF -> "af"
  | R.PF -> "pf"
  | R.CF -> "cf"
  | R.K0 -> "k0"
  | R.K1 -> "k1"
  | R.K2 -> "k2"
  | R.K3 -> "k3"
  | R.K4 -> "k4"
  | R.K5 -> "k5"
  | R.K6 -> "k6"
  | R.K7 -> "k7"
  | _ -> "UnknowReg"

let opCodeToString = function
  | Opcode.AAA -> "aaa"
  | Opcode.AAD -> "aad"
  | Opcode.AAM -> "aam"
  | Opcode.AAS -> "aas"
  | Opcode.ADC -> "adc"
  | Opcode.ADD -> "add"
  | Opcode.ADDPD -> "addpd"
  | Opcode.ADDPS -> "addps"
  | Opcode.ADDSD -> "addsd"
  | Opcode.ADDSS -> "addss"
  | Opcode.AND -> "and"
  | Opcode.ANDNPD -> "andnpd"
  | Opcode.ANDNPS -> "andnps"
  | Opcode.ANDPD -> "andpd"
  | Opcode.ANDPS -> "andps"
  | Opcode.ARPL -> "arpl"
  | Opcode.BNDMOV -> "bndmov"
  | Opcode.BOUND -> "bound"
  | Opcode.BSF -> "bsf"
  | Opcode.BSR -> "bsr"
  | Opcode.BSWAP -> "bswap"
  | Opcode.BT -> "bt"
  | Opcode.BTC -> "btc"
  | Opcode.BTR -> "btr"
  | Opcode.BTS -> "bts"
  | Opcode.CALLFar | Opcode.CALLNear -> "call"
  | Opcode.CBW -> "cbw"
  | Opcode.CDQ -> "cdq"
  | Opcode.CDQE -> "cdqe"
  | Opcode.CLAC -> "clac"
  | Opcode.CLC -> "clc"
  | Opcode.CLD -> "cld"
  | Opcode.CLFLUSH -> "clflush"
  | Opcode.CLI -> "cli"
  | Opcode.CLTS -> "clts"
  | Opcode.CMC -> "cmc"
  | Opcode.CMOVA -> "cmova"
  | Opcode.CMOVAE -> "cmovae"
  | Opcode.CMOVB -> "cmovb"
  | Opcode.CMOVBE -> "cmovbe"
  | Opcode.CMOVG -> "cmovg"
  | Opcode.CMOVGE -> "cmovge"
  | Opcode.CMOVL -> "cmovl"
  | Opcode.CMOVLE -> "cmovle"
  | Opcode.CMOVNO -> "cmovno"
  | Opcode.CMOVNP -> "cmovnp"
  | Opcode.CMOVNS -> "cmovns"
  | Opcode.CMOVNZ -> "cmovnz"
  | Opcode.CMOVO -> "cmovo"
  | Opcode.CMOVP -> "cmovp"
  | Opcode.CMOVS -> "cmovs"
  | Opcode.CMOVZ -> "cmovz"
  | Opcode.CMP -> "cmp"
  | Opcode.CMPPD -> "cmppd"
  | Opcode.CMPPS -> "cmpps"
  | Opcode.CMPSB -> "cmpsb"
  | Opcode.CMPSD -> "cmpsd"
  | Opcode.CMPSQ -> "cmpsq"
  | Opcode.CMPSS -> "cmpss"
  | Opcode.CMPSW -> "cmpsw"
  | Opcode.CMPXCHG -> "cmpxchg"
  | Opcode.CMPXCHG16B -> "cmpxchg16b"
  | Opcode.CMPXCHG8B -> "cmpxchg8b"
  | Opcode.COMISD -> "comisd"
  | Opcode.COMISS -> "comiss"
  | Opcode.CPUID -> "cpuid"
  | Opcode.CQO -> "cqo"
  | Opcode.CRC32 -> "crc32"
  | Opcode.CVTDQ2PD -> "cvtdq2pd"
  | Opcode.CVTDQ2PS -> "cvtdq2ps"
  | Opcode.CVTPD2DQ -> "cvtpd2dq"
  | Opcode.CVTPD2PI -> "cvtpd2pi"
  | Opcode.CVTPD2PS -> "cvtpd2ps"
  | Opcode.CVTPI2PD -> "cvtpi2pd"
  | Opcode.CVTPI2PS -> "cvtpi2ps"
  | Opcode.CVTPS2DQ -> "cvtps2dq"
  | Opcode.CVTPS2PD -> "cvtps2pd"
  | Opcode.CVTPS2PI -> "cvtps2pi"
  | Opcode.CVTSD2SI -> "cvtsd2si"
  | Opcode.CVTSD2SS -> "cvtsd2ss"
  | Opcode.CVTSI2SD -> "cvtsi2sd"
  | Opcode.CVTSI2SS -> "cvtsi2ss"
  | Opcode.CVTSS2SD -> "cvtss2sd"
  | Opcode.CVTSS2SI -> "cvtss2si"
  | Opcode.CVTTPD2DQ -> "cvttpd2dq"
  | Opcode.CVTTPD2PI -> "cvttpd2pi"
  | Opcode.CVTTPS2DQ -> "cvttps2dq"
  | Opcode.CVTTPS2PI -> "cvttps2pi"
  | Opcode.CVTTSD2SI -> "cvttsd2si"
  | Opcode.CVTTSS2SI -> "cvttss2si"
  | Opcode.CWD -> "cwd"
  | Opcode.CWDE -> "cwde"
  | Opcode.DAA -> "daa"
  | Opcode.DAS -> "das"
  | Opcode.DEC -> "dec"
  | Opcode.DIV -> "div"
  | Opcode.DIVPD -> "divpd"
  | Opcode.DIVPS -> "divps"
  | Opcode.DIVSD -> "divsd"
  | Opcode.DIVSS -> "divss"
  | Opcode.EMMS -> "emms"
  | Opcode.ENTER -> "enter"
  | Opcode.F2XM1 -> "f2xm1"
  | Opcode.FABS -> "fabs"
  | Opcode.FADD -> "fadd"
  | Opcode.FADDP -> "faddp"
  | Opcode.FBLD -> "fbld"
  | Opcode.FBSTP -> "fbstp"
  | Opcode.FCHS -> "fchs"
  | Opcode.FCLEX -> "fclex"
  | Opcode.FCMOVB -> "fcmovb"
  | Opcode.FCMOVBE -> "fcmovbe"
  | Opcode.FCMOVE -> "fcmove"
  | Opcode.FCMOVNB -> "fcmovnb"
  | Opcode.FCMOVNBE -> "fcmovnbe"
  | Opcode.FCMOVNE -> "fcmovne"
  | Opcode.FCMOVNU -> "fcmovnu"
  | Opcode.FCMOVU -> "fcmovu"
  | Opcode.FCOM -> "fcom"
  | Opcode.FCOMI -> "fcomi"
  | Opcode.FCOMIP -> "fcomip"
  | Opcode.FCOMP -> "fcomp"
  | Opcode.FCOMPP -> "fcompp"
  | Opcode.FCOS -> "fcos"
  | Opcode.FDECSTP -> "fdecstp"
  | Opcode.FDIV -> "fdiv"
  | Opcode.FDIVP -> "fdivp"
  | Opcode.FDIVR -> "fdivr"
  | Opcode.FDIVRP -> "fdivrp"
  | Opcode.FFREE -> "ffree"
  | Opcode.FIADD -> "fiadd"
  | Opcode.FICOM -> "ficom"
  | Opcode.FICOMP -> "ficomp"
  | Opcode.FIDIV -> "fidiv"
  | Opcode.FIDIVR -> "fidivr"
  | Opcode.FILD -> "fild"
  | Opcode.FIMUL -> "fimul"
  | Opcode.FINCSTP -> "fincstp"
  | Opcode.FINIT -> "finit"
  | Opcode.FIST -> "fist"
  | Opcode.FISTP -> "fistp"
  | Opcode.FISTTP -> "fisttp"
  | Opcode.FISUB -> "fisub"
  | Opcode.FISUBR -> "fisubr"
  | Opcode.FLD -> "fld"
  | Opcode.FLD1 -> "fld1"
  | Opcode.FLDCW -> "fldcw"
  | Opcode.FLDENV -> "fldenv"
  | Opcode.FLDL2E -> "fldl2e"
  | Opcode.FLDL2T -> "fldl2t"
  | Opcode.FLDLG2 -> "fldlg2"
  | Opcode.FLDLN2 -> "fldln2"
  | Opcode.FLDPI -> "fldpi"
  | Opcode.FLDZ -> "fldz"
  | Opcode.FMUL -> "fmul"
  | Opcode.FMULP -> "fmulp"
  | Opcode.FNOP -> "fnop"
  | Opcode.FNSTCW -> "fnstcw"
  | Opcode.FPATAN -> "fpatan"
  | Opcode.FPREM -> "fprem"
  | Opcode.FPREM1 -> "fprem1"
  | Opcode.FPTAN -> "fptan"
  | Opcode.FRNDINT -> "frndint"
  | Opcode.FRSTOR -> "frstor"
  | Opcode.FSAVE -> "fsave"
  | Opcode.FSCALE -> "fscale"
  | Opcode.FSIN -> "fsin"
  | Opcode.FSINCOS -> "fsincos"
  | Opcode.FSQRT -> "fsqrt"
  | Opcode.FST -> "fst"
  | Opcode.FSTENV -> "fstenv"
  | Opcode.FSTP -> "fstp"
  | Opcode.FSTSW -> "fstsw"
  | Opcode.FSUB -> "fsub"
  | Opcode.FSUBP -> "fsubp"
  | Opcode.FSUBR -> "fsubr"
  | Opcode.FSUBRP -> "fsubrp"
  | Opcode.FTST -> "ftst"
  | Opcode.FUCOM -> "fucom"
  | Opcode.FUCOMI -> "fucomi"
  | Opcode.FUCOMIP -> "fucomip"
  | Opcode.FUCOMP -> "fucomp"
  | Opcode.FUCOMPP -> "fucompp"
  | Opcode.FXAM -> "fxam"
  | Opcode.FXCH -> "fxch"
  | Opcode.FXRSTOR -> "fxrstor"
  | Opcode.FXRSTOR64 -> "fxrstor64"
  | Opcode.FXSAVE -> "fxsave"
  | Opcode.FXSAVE64 -> "fxsave64"
  | Opcode.FXTRACT -> "fxtract"
  | Opcode.FYL2X -> "fyl2x"
  | Opcode.FYL2XP1 -> "fyl2xp1"
  | Opcode.GETSEC -> "getsec"
  | Opcode.HLT -> "hlt"
  | Opcode.IDIV -> "idiv"
  | Opcode.IMUL -> "imul"
  | Opcode.IN -> "in"
  | Opcode.INC -> "inc"
  | Opcode.INS -> "ins"
  | Opcode.INSB -> "insb"
  | Opcode.INSD -> "insd"
  | Opcode.INSW -> "insw"
  | Opcode.INT -> "int"
  | Opcode.INT3 -> "int3"
  | Opcode.INTO -> "into"
  | Opcode.INVD -> "invd"
  | Opcode.INVLPG -> "invlpg"
  | Opcode.IRETD -> "iretd"
  | Opcode.IRETQ -> "iretq"
  | Opcode.IRETW -> "iretw"
  | Opcode.JA -> "ja"
  | Opcode.JB -> "jb"
  | Opcode.JBE -> "jbe"
  | Opcode.JCXZ -> "jcxz"
  | Opcode.JECXZ -> "jecxz"
  | Opcode.JG -> "jg"
  | Opcode.JL -> "jl"
  | Opcode.JLE -> "jle"
  | Opcode.JMPFar | Opcode.JMPNear -> "jmp"
  | Opcode.JNB -> "jnb"
  | Opcode.JNL -> "jnl"
  | Opcode.JNO -> "jno"
  | Opcode.JNP -> "jnp"
  | Opcode.JNS -> "jns"
  | Opcode.JNZ -> "jnz"
  | Opcode.JO -> "jo"
  | Opcode.JP -> "jp"
  | Opcode.JRCXZ -> "jrcxz"
  | Opcode.JS -> "js"
  | Opcode.JZ -> "jz"
  | Opcode.LAHF -> "lahf"
  | Opcode.LAR -> "lar"
  | Opcode.LDDQU -> "lddqu"
  | Opcode.LDMXCSR -> "ldmxcsr"
  | Opcode.LDS -> "lds"
  | Opcode.LEA -> "lea"
  | Opcode.LEAVE -> "leave"
  | Opcode.LES -> "les"
  | Opcode.LFENCE -> "lfence"
  | Opcode.LFS -> "lfs"
  | Opcode.LGDT -> "lgdt"
  | Opcode.LGS -> "lgs"
  | Opcode.LIDT -> "lidt"
  | Opcode.LLDT -> "lldt"
  | Opcode.LMSW -> "lmsw"
  | Opcode.LODSB -> "lodsb"
  | Opcode.LODSD -> "lodsd"
  | Opcode.LODSQ -> "lodsq"
  | Opcode.LODSW -> "lodsw"
  | Opcode.LOOP -> "loop"
  | Opcode.LOOPE -> "loope"
  | Opcode.LOOPNE -> "loopne"
  | Opcode.LSL -> "lsl"
  | Opcode.LSS -> "lss"
  | Opcode.LTR -> "ltr"
  | Opcode.LZCNT -> "lzcnt"
  | Opcode.MAXPD -> "maxpd"
  | Opcode.MAXPS -> "maxps"
  | Opcode.MAXSD -> "maxsd"
  | Opcode.MAXSS -> "maxss"
  | Opcode.MFENCE -> "mfence"
  | Opcode.MINPD -> "minpd"
  | Opcode.MINPS -> "minps"
  | Opcode.MINSD -> "minsd"
  | Opcode.MINSS -> "minss"
  | Opcode.MONITOR -> "monitor"
  | Opcode.MOV -> "mov"
  | Opcode.MOVAPD -> "movapd"
  | Opcode.MOVAPS -> "movaps"
  | Opcode.MOVBE -> "movbe"
  | Opcode.MOVD -> "movd"
  | Opcode.MOVDDUP -> "movddup"
  | Opcode.MOVDQ2Q -> "movdq2q"
  | Opcode.MOVDQA -> "movdqa"
  | Opcode.MOVDQU -> "movdqu"
  | Opcode.MOVHLPS -> "movhlps"
  | Opcode.MOVHPD -> "movhpd"
  | Opcode.MOVHPS -> "movhps"
  | Opcode.MOVLHPS -> "movlhps"
  | Opcode.MOVLPD -> "movlpd"
  | Opcode.MOVLPS -> "movlps"
  | Opcode.MOVMSKPD -> "movmskpd"
  | Opcode.MOVMSKPS -> "movmskps"
  | Opcode.MOVNTDQ -> "movntdq"
  | Opcode.MOVNTI -> "movnti"
  | Opcode.MOVNTPD -> "movntpd"
  | Opcode.MOVNTPS -> "movntps"
  | Opcode.MOVNTQ -> "movntq"
  | Opcode.MOVQ -> "movq"
  | Opcode.MOVQ2DQ -> "movq2dq"
  | Opcode.MOVSB -> "movsb"
  | Opcode.MOVSD -> "movsd"
  | Opcode.MOVSHDUP -> "movshdup"
  | Opcode.MOVSLDUP -> "movsldup"
  | Opcode.MOVSQ -> "movsq"
  | Opcode.MOVSS -> "movss"
  | Opcode.MOVSW -> "movsw"
  | Opcode.MOVSX -> "movsx"
  | Opcode.MOVSXD -> "movsxd"
  | Opcode.MOVUPD -> "movupd"
  | Opcode.MOVUPS -> "movups"
  | Opcode.MOVZX -> "movzx"
  | Opcode.MUL -> "mul"
  | Opcode.MULPD -> "mulpd"
  | Opcode.MULPS -> "mulps"
  | Opcode.MULSD -> "mulsd"
  | Opcode.MULSS -> "mulss"
  | Opcode.MULX -> "mulx"
  | Opcode.MWAIT -> "mwait"
  | Opcode.NEG -> "neg"
  | Opcode.NOP -> "nop"
  | Opcode.NOT -> "not"
  | Opcode.OR -> "or"
  | Opcode.ORPD -> "orpd"
  | Opcode.ORPS -> "orps"
  | Opcode.OUT -> "out"
  | Opcode.OUTS -> "outs"
  | Opcode.OUTSB -> "outsb"
  | Opcode.OUTSD -> "outsd"
  | Opcode.OUTSW -> "outsw"
  | Opcode.PABSB -> "pabsb"
  | Opcode.PABSD -> "pabsd"
  | Opcode.PABSW -> "pabsw"
  | Opcode.PACKSSDW -> "packssdw"
  | Opcode.PACKSSWB -> "packsswb"
  | Opcode.PACKUSDW -> "packusdw"
  | Opcode.PACKUSWB -> "packuswb"
  | Opcode.PADDB -> "paddb"
  | Opcode.PADDD -> "paddd"
  | Opcode.PADDQ -> "paddq"
  | Opcode.PADDSB -> "paddsb"
  | Opcode.PADDSW -> "paddsw"
  | Opcode.PADDUSB -> "paddusb"
  | Opcode.PADDUSW -> "paddusw"
  | Opcode.PADDW -> "paddw"
  | Opcode.PALIGNR -> "palignr"
  | Opcode.PAND -> "pand"
  | Opcode.PANDN -> "pandn"
  | Opcode.PAUSE -> "pause"
  | Opcode.PAVGB -> "pavgb"
  | Opcode.PAVGW -> "pavgw"
  | Opcode.PCMPEQB -> "pcmpeqb"
  | Opcode.PCMPEQD -> "pcmpeqd"
  | Opcode.PCMPEQQ -> "pcmpeqq"
  | Opcode.PCMPEQW -> "pcmpeqw"
  | Opcode.PCMPESTRI -> "pcmpestri"
  | Opcode.PCMPESTRM -> "pcmpestrm"
  | Opcode.PCMPGTB -> "pcmpgtb"
  | Opcode.PCMPGTD -> "pcmpgtd"
  | Opcode.PCMPGTQ -> "pcmpgtq"
  | Opcode.PCMPGTW -> "pcmpgtw"
  | Opcode.PCMPISTRI -> "pcmpistri"
  | Opcode.PCMPISTRM -> "pcmpistrm"
  | Opcode.PEXTRW -> "pextrw"
  | Opcode.PHADDD -> "phaddd"
  | Opcode.PHADDSW -> "phaddsw"
  | Opcode.PHADDW -> "phaddw"
  | Opcode.PHMINPOSUW -> "phminposuw"
  | Opcode.PHSUBD -> "phsubd"
  | Opcode.PHSUBSW -> "phsubsw"
  | Opcode.PHSUBW -> "phsubw"
  | Opcode.PINSRB -> "pinsrb"
  | Opcode.PINSRW -> "pinsrw"
  | Opcode.PMADDWD -> "pmaddwd"
  | Opcode.PMAXSB -> "pmaxsb"
  | Opcode.PMAXSD -> "pmaxsd"
  | Opcode.PMAXSW -> "pmaxsw"
  | Opcode.PMAXUB -> "pmaxub"
  | Opcode.PMAXUD -> "pmaxud"
  | Opcode.PMAXUW -> "pmaxuw"
  | Opcode.PMINSB -> "pminsb"
  | Opcode.PMINSD -> "pminsd"
  | Opcode.PMINSW -> "pminsw"
  | Opcode.PMINUB -> "pminub"
  | Opcode.PMINUD -> "pminud"
  | Opcode.PMINUW -> "pminuw"
  | Opcode.PMOVMSKB -> "pmovmskb"
  | Opcode.PMOVSXBD -> "pmovsxbd"
  | Opcode.PMOVSXBQ -> "pmovsxbq"
  | Opcode.PMOVSXBW -> "pmovsxbw"
  | Opcode.PMOVSXDQ -> "pmovsxdq"
  | Opcode.PMOVSXWD -> "pmovsxwd"
  | Opcode.PMOVSXWQ -> "pmovsxwq"
  | Opcode.PMOVZXBD -> "pmovzxbd"
  | Opcode.PMOVZXBQ -> "pmovzxbq"
  | Opcode.PMOVZXBW -> "pmovzxbw"
  | Opcode.PMOVZXDQ -> "pmovzxdq"
  | Opcode.PMOVZXWD -> "pmovzxwd"
  | Opcode.PMOVZXWQ -> "pmovzxwq"
  | Opcode.PMULDQ -> "pmuldq"
  | Opcode.PMULHRSW -> "pmulhrsw"
  | Opcode.PMULHUW -> "pmulhuw"
  | Opcode.PMULHW -> "pmulhw"
  | Opcode.PMULLD -> "pmulld"
  | Opcode.PMULLW -> "pmullw"
  | Opcode.PMULUDQ -> "pmuludq"
  | Opcode.POP -> "pop"
  | Opcode.POPA -> "popa"
  | Opcode.POPAD -> "popad"
  | Opcode.POPCNT -> "popcnt"
  | Opcode.POPF -> "popf"
  | Opcode.POPFD -> "popfd"
  | Opcode.POPFQ -> "popfq"
  | Opcode.POR -> "por"
  | Opcode.PREFETCHNTA -> "prefetchnta"
  | Opcode.PREFETCHT0 -> "prefetcht0"
  | Opcode.PREFETCHT1 -> "prefetcht1"
  | Opcode.PREFETCHT2 -> "prefetcht2"
  | Opcode.PREFETCHW -> "prefetchw"
  | Opcode.PREFETCHWT1 -> "prefetchwt1"
  | Opcode.PSADBW -> "psadbw"
  | Opcode.PSHUFB -> "pshufb"
  | Opcode.PSHUFD -> "pshufd"
  | Opcode.PSHUFHW -> "pshufhw"
  | Opcode.PSHUFLW -> "pshuflw"
  | Opcode.PSHUFW -> "pshufw"
  | Opcode.PSIGNB -> "psignb"
  | Opcode.PSIGND -> "psignd"
  | Opcode.PSIGNW -> "psignw"
  | Opcode.PSLLD -> "pslld"
  | Opcode.PSLLDQ -> "pslldq"
  | Opcode.PSLLQ -> "psllq"
  | Opcode.PSLLW -> "psllw"
  | Opcode.PSRAD -> "psrad"
  | Opcode.PSRAW -> "psraw"
  | Opcode.PSRLD -> "psrld"
  | Opcode.PSRLDQ -> "psrldq"
  | Opcode.PSRLQ -> "psrlq"
  | Opcode.PSRLW -> "psrlw"
  | Opcode.PSUBB -> "psubb"
  | Opcode.PSUBD -> "psubd"
  | Opcode.PSUBQ -> "psubq"
  | Opcode.PSUBSB -> "psubsb"
  | Opcode.PSUBSW -> "psubsw"
  | Opcode.PSUBUSB -> "psubusb"
  | Opcode.PSUBUSW -> "psubusw"
  | Opcode.PSUBW -> "psubw"
  | Opcode.PTEST -> "ptest"
  | Opcode.PUNPCKHBW -> "punpckhbw"
  | Opcode.PUNPCKHDQ -> "punpckhdq"
  | Opcode.PUNPCKHQDQ -> "punpckhqdq"
  | Opcode.PUNPCKHWD -> "punpckhwd"
  | Opcode.PUNPCKLBW -> "punpcklbw"
  | Opcode.PUNPCKLDQ -> "punpckldq"
  | Opcode.PUNPCKLQDQ -> "punpcklqdq"
  | Opcode.PUNPCKLWD -> "punpcklwd"
  | Opcode.PUSH -> "push"
  | Opcode.PUSHA -> "pusha"
  | Opcode.PUSHAD -> "pushad"
  | Opcode.PUSHF -> "pushf"
  | Opcode.PUSHFD -> "pushfd"
  | Opcode.PUSHFQ -> "pushfq"
  | Opcode.PXOR -> "pxor"
  | Opcode.RCL -> "rcl"
  | Opcode.RCR -> "rcr"
  | Opcode.RDFSBASE -> "rdfsbase"
  | Opcode.RDGSBASE -> "rdgsbase"
  | Opcode.RDMSR -> "rdmsr"
  | Opcode.RDPKRU -> "rdpkru"
  | Opcode.RDPMC -> "rdpmc"
  | Opcode.RDRAND -> "rdrand"
  | Opcode.RDSEED -> "rdseed"
  | Opcode.RDTSC -> "rdtsc"
  | Opcode.RDTSCP -> "rdtscp"
  | Opcode.RETFar | Opcode.RETFarImm
  | Opcode.RETNear | Opcode.RETNearImm -> "ret"
  | Opcode.ROL -> "rol"
  | Opcode.ROR -> "ror"
  | Opcode.RORX -> "rorx"
  | Opcode.ROUNDSD -> "roundsd"
  | Opcode.RSM -> "rsm"
  | Opcode.SAHF -> "sahf"
  | Opcode.SAR -> "sar"
  | Opcode.SARX -> "sarx"
  | Opcode.SBB -> "sbb"
  | Opcode.SCASB -> "scasb"
  | Opcode.SCASD -> "scasd"
  | Opcode.SCASQ -> "scasq"
  | Opcode.SCASW -> "scasw"
  | Opcode.SETA -> "seta"
  | Opcode.SETB -> "setb"
  | Opcode.SETBE -> "setbe"
  | Opcode.SETG -> "setg"
  | Opcode.SETL -> "setl"
  | Opcode.SETLE -> "setle"
  | Opcode.SETNB -> "setnb"
  | Opcode.SETNL -> "setnl"
  | Opcode.SETNO -> "setno"
  | Opcode.SETNP -> "setnp"
  | Opcode.SETNS -> "setns"
  | Opcode.SETNZ -> "setnz"
  | Opcode.SETO -> "seto"
  | Opcode.SETP -> "setp"
  | Opcode.SETS -> "sets"
  | Opcode.SETZ -> "setz"
  | Opcode.SFENCE -> "sfence"
  | Opcode.SGDT -> "sgdt"
  | Opcode.SHL -> "shl"
  | Opcode.SHLD -> "shld"
  | Opcode.SHLX -> "shlx"
  | Opcode.SHR -> "shr"
  | Opcode.SHRD -> "shrd"
  | Opcode.SHRX -> "shrx"
  | Opcode.SHUFPD -> "shufpd"
  | Opcode.SHUFPS -> "shufps"
  | Opcode.SIDT -> "sidt"
  | Opcode.SLDT -> "sldt"
  | Opcode.SMSW -> "smsw"
  | Opcode.SQRTPD -> "sqrtpd"
  | Opcode.SQRTPS -> "sqrtps"
  | Opcode.SQRTSD -> "sqrtsd"
  | Opcode.SQRTSS -> "sqrtss"
  | Opcode.STAC -> "stac"
  | Opcode.STC -> "stc"
  | Opcode.STD -> "std"
  | Opcode.STI -> "sti"
  | Opcode.STMXCSR -> "stmxcsr"
  | Opcode.STOSB -> "stosb"
  | Opcode.STOSD -> "stosd"
  | Opcode.STOSQ -> "stosq"
  | Opcode.STOSW -> "stosw"
  | Opcode.STR -> "str"
  | Opcode.SUB -> "sub"
  | Opcode.SUBPD -> "subpd"
  | Opcode.SUBPS -> "subps"
  | Opcode.SUBSD -> "subsd"
  | Opcode.SUBSS -> "subss"
  | Opcode.SWAPGS -> "swapgs"
  | Opcode.SYSCALL -> "syscall"
  | Opcode.SYSENTER -> "sysenter"
  | Opcode.SYSEXIT -> "sysexit"
  | Opcode.SYSRET -> "sysret"
  | Opcode.TEST -> "test"
  | Opcode.TZCNT -> "tzcnt"
  | Opcode.UCOMISD -> "ucomisd"
  | Opcode.UCOMISS -> "ucomiss"
  | Opcode.UD2 -> "ud2"
  | Opcode.UNPCKHPD -> "unpckhpd"
  | Opcode.UNPCKHPS -> "unpckhps"
  | Opcode.UNPCKLPD -> "unpcklpd"
  | Opcode.UNPCKLPS -> "unpcklps"
  | Opcode.VADDPD -> "vaddpd"
  | Opcode.VADDPS -> "vaddps"
  | Opcode.VADDSD -> "vaddsd"
  | Opcode.VADDSS -> "vaddss"
  | Opcode.VANDNPD -> "vandnpd"
  | Opcode.VANDNPS -> "vandnps"
  | Opcode.VANDPD -> "vandpd"
  | Opcode.VANDPS -> "vandps"
  | Opcode.VBROADCASTI128 -> "vbroadcasti128"
  | Opcode.VBROADCASTSS -> "vbroadcastss"
  | Opcode.VCOMISD -> "vcomisd"
  | Opcode.VCOMISS -> "vcomiss"
  | Opcode.VCVTSD2SI -> "vcvtsd2si"
  | Opcode.VCVTSI2SD -> "vcvtsi2sd"
  | Opcode.VCVTSI2SS -> "vcvtsi2ss"
  | Opcode.VCVTSS2SI -> "vcvtss2si"
  | Opcode.VCVTTSD2SI -> "vcvttsd2si"
  | Opcode.VCVTTSS2SI -> "vcvttss2si"
  | Opcode.VDIVPD -> "vdivpd"
  | Opcode.VDIVPS -> "vdivps"
  | Opcode.VDIVSD -> "vdivsd"
  | Opcode.VDIVSS -> "vdivss"
  | Opcode.VERR -> "verr"
  | Opcode.VERW -> "verw"
  | Opcode.VINSERTI128 -> "vinserti128"
  | Opcode.VLDDQU -> "vlddqu"
  | Opcode.VMCALL -> "vmcall"
  | Opcode.VMCLEAR -> "vmclear"
  | Opcode.VMFUNC -> "vmfunc"
  | Opcode.VMLAUNCH -> "vmlaunch"
  | Opcode.VMOVAPD -> "vmovapd"
  | Opcode.VMOVAPS -> "vmovaps"
  | Opcode.VMOVD -> "vmovd"
  | Opcode.VMOVDDUP -> "vmovddup"
  | Opcode.VMOVDQA -> "vmovdqa"
  | Opcode.VMOVDQA32 -> "vmovdqa32"
  | Opcode.VMOVDQA64 -> "vmovdqa64"
  | Opcode.VMOVDQU -> "vmovdqu"
  | Opcode.VMOVDQU32 -> "vmovdqu32"
  | Opcode.VMOVDQU64 -> "vmovdqu64"
  | Opcode.VMOVHLPS -> "vmovhlps"
  | Opcode.VMOVHPD -> "vmovhpd"
  | Opcode.VMOVHPS -> "vmovhps"
  | Opcode.VMOVLHPS -> "vmovlhps"
  | Opcode.VMOVLPD -> "vmovlpd"
  | Opcode.VMOVLPS -> "vmovlps"
  | Opcode.VMOVMSKPD -> "vmovmskpd"
  | Opcode.VMOVMSKPS -> "vmovmskps"
  | Opcode.VMOVNTDQ -> "vmovntdq"
  | Opcode.VMOVNTPD -> "vmovntpd"
  | Opcode.VMOVNTPS -> "vmovntps"
  | Opcode.VMOVQ -> "vmovq"
  | Opcode.VMOVSD -> "vmovsd"
  | Opcode.VMOVSHDUP -> "vmovshdup"
  | Opcode.VMOVSLDUP -> "vmovsldup"
  | Opcode.VMOVSS -> "vmovss"
  | Opcode.VMOVUPD -> "vmovupd"
  | Opcode.VMOVUPS -> "vmovups"
  | Opcode.VMPTRLD -> "vmptrld"
  | Opcode.VMPTRST -> "vmptrst"
  | Opcode.VMRESUME -> "vmresume"
  | Opcode.VMULPD -> "vmulpd"
  | Opcode.VMULPS -> "vmulps"
  | Opcode.VMULSD -> "vmulsd"
  | Opcode.VMULSS -> "vmulss"
  | Opcode.VMXOFF -> "vmxoff"
  | Opcode.VMXON -> "vmxon"
  | Opcode.VORPD -> "vorpd"
  | Opcode.VORPS -> "vorps"
  | Opcode.VPABSB -> "vpabsb"
  | Opcode.VPABSD -> "vpabsd"
  | Opcode.VPABSW -> "vpabsw"
  | Opcode.VPACKSSDW -> "vpackssdw"
  | Opcode.VPACKSSWB -> "vpacksswb"
  | Opcode.VPACKUSDW -> "vpackusdw"
  | Opcode.VPACKUSWB -> "vpackuswb"
  | Opcode.VPADDB -> "vpaddb"
  | Opcode.VPADDD -> "vpaddd"
  | Opcode.VPADDQ -> "vpaddq"
  | Opcode.VPADDSB -> "vpaddsb"
  | Opcode.VPADDSW -> "vpaddsw"
  | Opcode.VPADDUSB -> "vpaddusb"
  | Opcode.VPADDUSW -> "vpaddusw"
  | Opcode.VPADDW -> "vpaddw"
  | Opcode.VPALIGNR -> "vpalignr"
  | Opcode.VPAND -> "vpand"
  | Opcode.VPANDN -> "vpandn"
  | Opcode.VPAVGB -> "vpavgb"
  | Opcode.VPAVGW -> "vpavgw"
  | Opcode.VPBROADCASTB -> "vpbroadcastb"
  | Opcode.VPCMPEQB -> "vpcmpeqb"
  | Opcode.VPCMPEQD -> "vpcmpeqd"
  | Opcode.VPCMPEQQ -> "vpcmpeqq"
  | Opcode.VPCMPEQW -> "vpcmpeqw"
  | Opcode.VPCMPESTRI -> "vpcmpestri"
  | Opcode.VPCMPESTRM -> "vpcmpestrm"
  | Opcode.VPCMPGTB -> "vpcmpgtb"
  | Opcode.VPCMPGTD -> "vpcmpgtd"
  | Opcode.VPCMPGTQ -> "vpcmpgtq"
  | Opcode.VPCMPGTW -> "vpcmpgtw"
  | Opcode.VPCMPISTRI -> "vpcmpistri"
  | Opcode.VPCMPISTRM -> "vpcmpistrm"
  | Opcode.VPEXTRW -> "vpextrw"
  | Opcode.VPHADDD -> "vphaddd"
  | Opcode.VPHADDSW -> "vphaddsw"
  | Opcode.VPHADDW -> "vphaddw"
  | Opcode.VPHMINPOSUW -> "vphminposuw"
  | Opcode.VPHSUBD -> "vphsubd"
  | Opcode.VPHSUBSW -> "vphsubsw"
  | Opcode.VPHSUBW -> "vphsubw"
  | Opcode.VPINSRB -> "vpinsrb"
  | Opcode.VPINSRW -> "vpinsrw"
  | Opcode.VPMADDWD -> "vpmaddwd"
  | Opcode.VPMAXSB -> "vpmaxsb"
  | Opcode.VPMAXSD -> "vpmaxsd"
  | Opcode.VPMAXSW -> "vpmaxsw"
  | Opcode.VPMAXUB -> "vpmaxub"
  | Opcode.VPMAXUD -> "vpmaxud"
  | Opcode.VPMAXUW -> "vpmaxuw"
  | Opcode.VPMINSB -> "vpminsb"
  | Opcode.VPMINSD -> "vpminsd"
  | Opcode.VPMINSW -> "vpminsw"
  | Opcode.VPMINUB -> "vpminub"
  | Opcode.VPMINUD -> "vpminud"
  | Opcode.VPMINUW -> "vpminuw"
  | Opcode.VPMOVMSKB -> "vpmovmskb"
  | Opcode.VPMOVSXBD -> "vpmovsxbd"
  | Opcode.VPMOVSXBQ -> "vpmovsxbq"
  | Opcode.VPMOVSXBW -> "vpmovsxbw"
  | Opcode.VPMOVSXDQ -> "vpmovsxdq"
  | Opcode.VPMOVSXWD -> "vpmovsxwd"
  | Opcode.VPMOVSXWQ -> "vpmovsxwq"
  | Opcode.VPMOVZXBD -> "vpmovzxbd"
  | Opcode.VPMOVZXBQ -> "vpmovzxbq"
  | Opcode.VPMOVZXBW -> "vpmovzxbw"
  | Opcode.VPMOVZXDQ -> "vpmovzxdq"
  | Opcode.VPMOVZXWD -> "vpmovzxwd"
  | Opcode.VPMOVZXWQ -> "vpmovzxwq"
  | Opcode.VPMULDQ -> "vpmuldq"
  | Opcode.VPMULHRSW -> "vpmulhrsw"
  | Opcode.VPMULHUW -> "vpmulhuw"
  | Opcode.VPMULHW -> "vpmulhw"
  | Opcode.VPMULLD -> "vpmulld"
  | Opcode.VPMULLW -> "vpmullw"
  | Opcode.VPMULUDQ -> "vpmuludq"
  | Opcode.VPOR -> "vpor"
  | Opcode.VPSADBW -> "vpsadbw"
  | Opcode.VPSHUFB -> "vpshufb"
  | Opcode.VPSHUFD -> "vpshufd"
  | Opcode.VPSHUFHW -> "vpshufhw"
  | Opcode.VPSHUFLW -> "vpshuflw"
  | Opcode.VPSIGNB -> "vpsignb"
  | Opcode.VPSIGND -> "vpsignd"
  | Opcode.VPSIGNW -> "vpsignw"
  | Opcode.VPSLLD -> "vpslld"
  | Opcode.VPSLLDQ -> "vpslldq"
  | Opcode.VPSLLQ -> "vpsllq"
  | Opcode.VPSLLW -> "vpsllw"
  | Opcode.VPSRAD -> "vpsrad"
  | Opcode.VPSRAW -> "vpsraw"
  | Opcode.VPSRLD -> "vpsrld"
  | Opcode.VPSRLDQ -> "vpsrldq"
  | Opcode.VPSRLQ -> "vpsrlq"
  | Opcode.VPSRLW -> "vpsrlw"
  | Opcode.VPSUBB -> "vpsubb"
  | Opcode.VPSUBD -> "vpsubd"
  | Opcode.VPSUBQ -> "vpsubq"
  | Opcode.VPSUBSB -> "vpsubsb"
  | Opcode.VPSUBSW -> "vpsubsw"
  | Opcode.VPSUBUSB -> "vpsubusb"
  | Opcode.VPSUBUSW -> "vpsubusw"
  | Opcode.VPSUBW -> "vpsubw"
  | Opcode.VPTEST -> "vptest"
  | Opcode.VPUNPCKHBW -> "vpunpckhbw"
  | Opcode.VPUNPCKHDQ -> "vpunpckhdq"
  | Opcode.VPUNPCKHQDQ -> "vpunpckhqdq"
  | Opcode.VPUNPCKHWD -> "vpunpckhwd"
  | Opcode.VPUNPCKLBW -> "vpunpcklbw"
  | Opcode.VPUNPCKLDQ -> "vpunpckldq"
  | Opcode.VPUNPCKLQDQ -> "vpunpcklqdq"
  | Opcode.VPUNPCKLWD -> "vpunpcklwd"
  | Opcode.VPXOR -> "vpxor"
  | Opcode.VSHUFPD -> "vshufpd"
  | Opcode.VSHUFPS -> "vshufps"
  | Opcode.VSUBPD -> "vsubpd"
  | Opcode.VSUBPS -> "vsubps"
  | Opcode.VSUBSD -> "vsubsd"
  | Opcode.VSUBSS -> "vsubss"
  | Opcode.VUCOMISD -> "vucomisd"
  | Opcode.VUCOMISS -> "vucomiss"
  | Opcode.VUNPCKHPD -> "vunpckhpd"
  | Opcode.VUNPCKHPS -> "vunpckhps"
  | Opcode.VUNPCKLPD -> "vunpcklpd"
  | Opcode.VUNPCKLPS -> "vunpcklps"
  | Opcode.VXORPD -> "vxorpd"
  | Opcode.VXORPS -> "vxorps"
  | Opcode.VZEROUPPER -> "vzeroupper"
  | Opcode.WAIT -> "wait"
  | Opcode.WBINVD -> "wbinvd"
  | Opcode.WRFSBASE -> "wrfsbase"
  | Opcode.WRGSBASE -> "wrgsbase"
  | Opcode.WRMSR -> "wrmsr"
  | Opcode.WRPKRU -> "wrpkru"
  | Opcode.XABORT -> "xabort"
  | Opcode.XADD -> "xadd"
  | Opcode.XBEGIN -> "xbegin"
  | Opcode.XCHG -> "xchg"
  | Opcode.XEND -> "xend"
  | Opcode.XGETBV -> "xgetbv"
  | Opcode.XLATB -> "xlatb"
  | Opcode.XOR -> "xor"
  | Opcode.XORPD -> "xorpd"
  | Opcode.XORPS -> "xorps"
  | Opcode.XRSTOR -> "xrstor"
  | Opcode.XRSTORS -> "xrstors"
  | Opcode.XRSTORS64 -> "xrstors64"
  | Opcode.XSAVE -> "xsave"
  | Opcode.XSAVEC -> "xsavec"
  | Opcode.XSAVEC64 -> "xsavec64"
  | Opcode.XSAVES -> "xsaves"
  | Opcode.XSAVES64 -> "xsaves64"
  | Opcode.XSAVEOPT -> "xsaveopt"
  | Opcode.XSETBV -> "xsetbv"
  | Opcode.XTEST -> "xtest"
  | _ -> failwith "Unknown opcode encountered."

let inline private iToHexStr (i: int64) builder acc =
  builder AsmWordKind.Value ("0x" + i.ToString("X")) acc

let inline private uToHexStr (i: uint64) builder acc =
  builder AsmWordKind.Value ("0x" + i.ToString("X")) acc

let inline private ptrDirectiveString isFar = function
  | 1 -> "byte ptr"
  | 2 -> "word ptr"
  | 4 -> if isFar then "word far ptr" else "dword ptr"
  | 6 -> "dword far ptr"
  | 8 -> "qword ptr"
  | 10 -> if isFar then "qword far ptr" else "tword ptr"
  | 16 -> "xmmword ptr"
  | 32 -> "ymmword ptr"
  | 64 -> "zmmword ptr"
  | _ -> failwith "Invalid ptr attribute"

let inline dispToString showSign wordSz (disp: Disp) builder acc =
  let mask = WordSize.toRegType wordSz |> RegType.getMask |> uint64
  if showSign && disp < 0L then
    builder AsmWordKind.String "-" acc |> iToHexStr (- disp) builder
  elif showSign then
    builder AsmWordKind.String "+" acc |> iToHexStr disp builder
  else
    uToHexStr (uint64 disp &&& mask) builder acc

let inline private memDispToStr showSign disp wordSz builder acc =
  match disp with
  | None -> acc
  | Some d -> dispToString showSign wordSz d builder acc

let inline scaleToString (scale: Scale) builder acc =
  if scale = Scale.X1 then acc
  else
    builder AsmWordKind.String "*" acc
    |> builder AsmWordKind.Value ((int scale).ToString())

let inline private memScaleDispToStr emptyBase si d wordSz builder acc =
  match si with
  | None -> memDispToStr (not emptyBase) d wordSz builder acc
  | Some (i, scale) ->
    let acc = if emptyBase then acc else builder AsmWordKind.String "+" acc
    builder AsmWordKind.Variable (regToStr i) acc
    |> scaleToString scale builder
    |> memDispToStr true d wordSz builder

let private memAddrToStr b si disp wordSz builder acc =
  match b with
  | None -> memScaleDispToStr true si disp wordSz builder acc
  | Some b ->
    builder AsmWordKind.Variable (regToStr b) acc
    |> memScaleDispToStr false si disp wordSz builder

let inline isFar (ins: InsInfo) =
  match ins.Opcode with
  | Opcode.JMPFar | Opcode.CALLFar -> true
  | _ -> false

let mToString wordSz (ins: InsInfo) b si d oprSz builder acc =
  let ptrDirective = RegType.toByteWidth oprSz |> ptrDirectiveString (isFar ins)
  match Helper.getSegment ins.Prefixes with
  | None ->
    builder AsmWordKind.String ptrDirective acc
    |> builder AsmWordKind.String (" [")
    |> memAddrToStr b si d wordSz builder
    |> builder AsmWordKind.String "]"
  | Some seg ->
    builder AsmWordKind.String ptrDirective acc
    |> builder AsmWordKind.String (" [")
    |> builder AsmWordKind.Variable (regToStr seg)
    |> builder AsmWordKind.String ":"
    |> memAddrToStr b si d wordSz builder
    |> builder AsmWordKind.String "]"

let commentWithSymbol (fi: FileInfo option) targetAddr builder acc =
  match fi with
  | Some fi ->
    match fi.TryFindFunctionSymbolName (targetAddr) with
    | false, _ ->
      builder AsmWordKind.String " ; " acc |> uToHexStr targetAddr builder
    | true, "" -> acc
    | true, name ->
      builder AsmWordKind.String " ; <" acc
      |> builder AsmWordKind.Value name
      |> builder AsmWordKind.String ">"
  | None ->
    builder AsmWordKind.String " ; " acc |> uToHexStr targetAddr builder

let inline relToString pc offset fi builder acc =
  (if offset < 0L then builder AsmWordKind.String "-" acc
   else builder AsmWordKind.String "+" acc)
  |> iToHexStr (abs offset) builder
  |> commentWithSymbol fi (pc + uint64 offset) builder

let inline absToString selector (offset: Addr) builder acc =
  uToHexStr (uint64 selector) builder acc
  |> builder AsmWordKind.String ":"
  |> uToHexStr offset builder

let getOpmaskRegister = function
  | 0x0uy -> Register.K0
  | 0x1uy -> Register.K1
  | 0x2uy -> Register.K2
  | 0x3uy -> Register.K3
  | 0x4uy -> Register.K4
  | 0x5uy -> Register.K5
  | 0x6uy -> Register.K6
  | 0x7uy -> Register.K7
  | _ -> raise InvalidRegisterException

/// Zeroing/Merging (EVEX.z)
let maskZtoString ev builder acc =
  if ev.Z = Zeroing then acc
  else builder AsmWordKind.String "{z}" acc

/// Opmask register
let maskRegToString ePrx builder acc =
  if ePrx.AAA = 0uy then acc
  else
    builder AsmWordKind.String " {" acc
    |> builder AsmWordKind.Variable (getOpmaskRegister ePrx.AAA |> regToStr)
    |> builder AsmWordKind.String "}"

let buildMask (ins: InsInfo) builder acc =
  match ins.VEXInfo with
  | Some { EVEXPrx = Some ePrx }->
    maskRegToString ePrx builder acc |> maskZtoString ePrx builder
  | _ -> acc

let oprToString wordSz ins insAddr fi opr isFstOpr builder acc =
  match opr with
  | OprReg reg ->
    let acc = builder AsmWordKind.Variable (regToStr reg) acc
    if isFstOpr then buildMask ins builder acc else acc
  | OprMem (b, si, disp, oprSz) ->
    let acc = mToString wordSz ins b si disp oprSz builder acc
    if isFstOpr then buildMask ins builder acc else acc
  | OprImm imm -> iToHexStr imm builder acc
  | OprDirAddr (Absolute (sel, offset, _)) -> absToString sel offset builder acc
  | OprDirAddr (Relative (offset)) -> relToString insAddr offset fi builder acc
  | GoToLabel _ -> failwith "Only used in assembly parser"

let inline buildPref (prefs: Prefix) builder acc =
  if (prefs &&& Prefix.PrxLOCK) <> Prefix.PrxNone then
    builder AsmWordKind.String "lock " acc
  elif (prefs &&& Prefix.PrxREPNZ) <> Prefix.PrxNone then
    builder AsmWordKind.String "repnz " acc
  elif (prefs &&& Prefix.PrxREPZ) <> Prefix.PrxNone then
    builder AsmWordKind.String "repz " acc
  else acc

let inline buildOpcode opcode builder acc =
  builder AsmWordKind.Mnemonic (opCodeToString opcode) acc

let recomputeRIPRel pc disp (ins: InsInfo) (insLen: uint32) builder acc =
  let oprSize = RegType.toByteWidth ins.InsSize.OperationSize
  let dir = ptrDirectiveString false oprSize
  builder AsmWordKind.String dir acc
  |> builder AsmWordKind.String " ["
  |> uToHexStr (pc + uint64 disp + uint64 insLen) builder
  |> builder AsmWordKind.String "]"

let buildOprs ins insLen pc fi wordSz builder acc =
  match ins.Operands with
  | NoOperand -> acc
  | OneOperand (OprMem (Some Register.RIP, None, Some off, 64<rt>)) ->
    builder AsmWordKind.String (" ") acc
    |> mToString wordSz ins (Some Register.RIP) None (Some off) 64<rt> builder
    |> commentWithSymbol fi (pc + uint64 insLen + uint64 off) builder
  | OneOperand opr ->
    builder AsmWordKind.String " " acc
    |> oprToString wordSz ins pc fi opr true builder
  | TwoOperands (OprMem (Some R.RIP, None, Some disp, _), opr) ->
    builder AsmWordKind.String " " acc
    |> recomputeRIPRel pc disp ins insLen builder
    |> builder AsmWordKind.String ", "
    |> oprToString wordSz ins pc fi opr false builder
  | TwoOperands (opr, OprMem (Some R.RIP, None, Some disp, _)) ->
    builder AsmWordKind.String " " acc
    |> oprToString wordSz ins pc fi opr true builder
    |> builder AsmWordKind.String ", "
    |> recomputeRIPRel pc disp ins insLen builder
  | TwoOperands (opr1, opr2) ->
    builder AsmWordKind.String " " acc
    |> oprToString wordSz ins pc fi opr1 true builder
    |> builder AsmWordKind.String ", "
    |> oprToString wordSz ins pc fi opr2 false builder
  | ThreeOperands (opr1, opr2, opr3) ->
    builder AsmWordKind.String " " acc
    |> oprToString wordSz ins pc fi opr1 true builder
    |> builder AsmWordKind.String ", "
    |> oprToString wordSz ins pc fi opr2 false builder
    |> builder AsmWordKind.String ", "
    |> oprToString wordSz ins pc fi opr3 false builder
  | FourOperands (opr1, opr2, opr3, opr4) ->
    builder AsmWordKind.String " " acc
    |> oprToString wordSz ins pc fi opr1 true builder
    |> builder AsmWordKind.String ", "
    |> oprToString wordSz ins pc fi opr2 false builder
    |> builder AsmWordKind.String ", "
    |> oprToString wordSz ins pc fi opr3 false builder
    |> builder AsmWordKind.String ", "
    |> oprToString wordSz ins pc fi opr4 false builder

let disasm showAddr wordSize fi ins pc insLen builder acc =
  DisasmBuilder.addr pc wordSize showAddr builder acc
  |> buildPref ins.Prefixes builder
  |> buildOpcode ins.Opcode builder
  |> buildOprs ins insLen pc fi wordSize builder

// vim: set tw=80 sts=2 sw=2:
