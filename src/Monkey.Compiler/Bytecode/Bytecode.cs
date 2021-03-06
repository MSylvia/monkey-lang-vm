using System;
using System.Collections.Generic;
using System.Linq;

namespace Monkey
{
    public static class Bytecode
    {
        public static List<byte> Create(byte opcode, List<int> operands)
        {
            var opcodeDefinition = Opcode.Find(opcode);
            if (opcodeDefinition.Name == Opcode.Name.Illegal)
            {
                return new List<byte>();
            }

            var instruction = new List<byte> { opcode };

            for (var i = 0; i < operands.Count; i++)
            {
                var length = opcodeDefinition.OperandLengths[i];

                switch (length)
                {
                    case 1:
                        instruction.Add((byte)operands[i]);
                        break;
                    case 2:
                        instruction.AddRange(BitConverter.GetBytes(Convert.ToUInt16(operands[i])));
                        break;
                }
            }
        
            return instruction;
        }
    }
}
