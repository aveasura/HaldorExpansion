using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

class StoreGuiContractTests
{
    static readonly Dictionary<short, OpCode> Opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)).ToDictionary(o => o.Value);
    static IEnumerable<string> Calls(PEReader pe, MetadataReader reader, TypeDefinition type, string name)
    {
        var method = type.GetMethods().Select(h => reader.GetMethodDefinition(h)).Single(m => reader.GetString(m.Name) == name);
        byte[] bytes = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes();
        for (int i = 0; i < bytes.Length;)
        {
            short value = bytes[i++];
            if (value == 254) value = (short)(0xfe00 | bytes[i++]);
            var op = Opcodes[value];
            switch (op.OperandType)
            {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar: i++; break;
                case OperandType.InlineVar: i += 2; break;
                case OperandType.InlineI8:
                case OperandType.InlineR: i += 8; break;
                case OperandType.InlineSwitch: int n = BitConverter.ToInt32(bytes, i); i += 4 + 4 * n; break;
                case OperandType.InlineMethod:
                    if (op == OpCodes.Call || op == OpCodes.Callvirt)
                    {
                        var handle = MetadataTokens.EntityHandle(BitConverter.ToInt32(bytes, i));
                        if (handle.Kind == HandleKind.MemberReference)
                            yield return reader.GetString(reader.GetMemberReference((MemberReferenceHandle)handle).Name);
                        else if (handle.Kind == HandleKind.MethodDefinition)
                        {
                            var target = reader.GetMethodDefinition((MethodDefinitionHandle)handle);
                            yield return reader.GetString(reader.GetTypeDefinition(target.GetDeclaringType()).Name) + "." + reader.GetString(target.Name);
                        }
                    }
                    i += 4; break;
                default: i += 4; break;
            }
        }
    }
    static TypeDefinition Type(MetadataReader reader, string name)
    {
        return reader.TypeDefinitions.Select(h => reader.GetTypeDefinition(h)).Single(t => reader.GetString(t.Name) == name);
    }
    static void Main()
    {
        using (var pe = new PEReader(File.OpenRead("assembly_valheim.dll")))
        {
            var reader = pe.GetMetadataReader();
            var store = Type(reader, "StoreGui");
            foreach (string name in new[] { "FillList", "SelectItem", "GetSelectedItemIndex" })
            {
                if (Calls(pe, reader, store, name).Count(n => n == "Trader.GetAvailableItems") != 1)
                    throw new Exception(name + " unexpected original contract");
                System.Console.WriteLine("PASS: actual StoreGui." + name + " has exactly the expected list call");
            }
            var fields = store.GetFields().Select(h => reader.GetString(reader.GetFieldDefinition(h).Name)).ToList();
            foreach (string field in new[] { "m_trader", "m_selectedItem", "m_itemlistBaseSize" })
                if (!fields.Contains(field)) throw new Exception("Missing field " + field);
            System.Console.WriteLine("PASS: actual StoreGui private-field contracts");
        }
        using (var pe = new PEReader(File.OpenRead("HaldorExpansion.dll")))
        {
            var reader = pe.GetMetadataReader();
            Type(reader, "HaldorTabsListPatch");
            Type(reader, "HaldorTabsController");
            if (!Calls(pe, reader, Type(reader, "HaldorExpansionPlugin"), "Awake").Contains("TradingUiConfiguration.Register"))
                throw new Exception("Configuration registration missing from compiled plugin");
            System.Console.WriteLine("PASS: built DLL contains tab patches and startup config registration");
        }
        // Metadata checks only; native Unity layout/Harmony execution require in-game testing.
    }
}
