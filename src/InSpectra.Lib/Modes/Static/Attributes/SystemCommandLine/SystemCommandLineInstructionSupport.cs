namespace InSpectra.Lib.Modes.Static.Attributes.SystemCommandLine;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class SystemCommandLineInstructionSupport
{
    public static bool TryGetArgumentIndex(MethodDef method, Instruction instruction, out int index)
    {
        index = instruction.OpCode.Code switch
        {
            Code.Ldarg_0 => 0,
            Code.Ldarg_1 => 1,
            Code.Ldarg_2 => 2,
            Code.Ldarg_3 => 3,
            _ => -1,
        };
        if (index >= 0)
        {
            return true;
        }

        if (instruction.Operand is Parameter parameter)
        {
            index = parameter.MethodSigIndex + (method.MethodSig?.HasThis == true ? 1 : 0);
            return true;
        }

        index = -1;
        return false;
    }

    public static bool TryGetLocalIndex(Instruction instruction, out int index)
    {
        index = instruction.OpCode.Code switch
        {
            Code.Ldloc_0 or Code.Stloc_0 => 0,
            Code.Ldloc_1 or Code.Stloc_1 => 1,
            Code.Ldloc_2 or Code.Stloc_2 => 2,
            Code.Ldloc_3 or Code.Stloc_3 => 3,
            _ => -1,
        };
        if (index >= 0)
        {
            return true;
        }

        if (instruction.Operand is Local local)
        {
            index = local.Index;
            return true;
        }

        index = -1;
        return false;
    }

    public static bool TryReadInt32(Instruction instruction, out int value)
    {
        value = instruction.OpCode.Code switch
        {
            Code.Ldc_I4_M1 => -1,
            Code.Ldc_I4_0 => 0,
            Code.Ldc_I4_1 => 1,
            Code.Ldc_I4_2 => 2,
            Code.Ldc_I4_3 => 3,
            Code.Ldc_I4_4 => 4,
            Code.Ldc_I4_5 => 5,
            Code.Ldc_I4_6 => 6,
            Code.Ldc_I4_7 => 7,
            Code.Ldc_I4_8 => 8,
            Code.Ldc_I4_S => (sbyte)instruction.Operand,
            Code.Ldc_I4 => (int)instruction.Operand,
            _ => 0,
        };
        return instruction.OpCode.Code is >= Code.Ldc_I4_M1 and <= Code.Ldc_I4
            || instruction.OpCode.Code == Code.Ldc_I4_S;
    }
}

