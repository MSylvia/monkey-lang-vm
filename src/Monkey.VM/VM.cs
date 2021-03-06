﻿using System;
using System.Collections.Generic;
using System.Linq;

using Monkey;
using Monkey.Shared;
using static Monkey.Evaluator.Utilities;
using Object = Monkey.Shared.Object;

namespace Monkey
{
    public class VirtualMachine
    {
        private static Dictionary<object, Object> Invariants = new Dictionary<object, Object>
        {
            { true, Object.Create(ObjectKind.Boolean, true) },
            { false, Object.Create(ObjectKind.Boolean, false) },
            { "null", Object.Create(ObjectKind.Null, null) }
        };

        private VirtualMachineState internalState;

        public Object StackTop { get { return internalState.Stack.Top; } }

        public VirtualMachine()
        {
            internalState = new VirtualMachineState { Globals = new List<Object>() };
        }

        public void Run(List<byte> instructions, List<Object> constants, List<BuiltIn> builtIns)
        {
            internalState = InitializeState(instructions, constants, builtIns);

            while (internalState.CurrentFrame.InstructionPointer < internalState.CurrentFrame.Closure.Instructions.Count)
            {
                if (internalState.Stack.Top != default (Object) && internalState.Stack.Top.Kind == ObjectKind.Error) break;

                internalState.Opcode = internalState.CurrentFrame.Closure.Instructions[internalState.CurrentFrame.InstructionPointer];
                internalState.CurrentFrame.InstructionPointer++;

                switch (internalState.Opcode)
                {
                    case 1:  // Opcode.Constant
                        ExecuteConstantOperation();
                        break;
                    case 3:  // Opcode.Pop
                        internalState.Stack.Pop();
                        break;
                    case 2:  // Opcode.Add
                    case 4:  // Opcode.Subtract
                    case 5:  // Opcode.Multiply
                    case 6:  // Opcode.Divide
                    case 9:  // Opcode.Equal
                    case 10: // Opcode.NotEqual
                    case 11: // Opcode.GreaterThan
                        ExecuteBinaryOperation(internalState.Opcode);
                        break;
                    case 7:  // Opcode.True
                    case 8:  // Opcode.False
                        ExecuteBooleanOperation(internalState.Opcode);
                        break;
                    case 12: // Opcode.Minus
                        ExecuteMinusOperation();
                        break;
                    case 13: // Opcode.Bang
                        ExecuteBangOperation();
                        break;
                    case 14: // Opcode.Jump
                        ExecuteJumpOperation();
                        break;
                    case 15: // Opcode.JumpNotTruthy
                        ExecuteJumpNotTruthyOperation();
                        break;
                    case 16: // Opcode.Null
                        ExecuteNullOperation();
                        break;
                    case 17: // Opcode.SetGlobal
                        ExecuteSetGlobalOperation();
                        break;
                    case 18: // Opcode.GetGlobal
                        ExecuteGetGlobalOperation();
                        break;
                    case 19: // Opcode.Array
                        ExecuteArrayOperation();
                        break;
                    case 20: // Opcode.Hash
                        ExecuteHashOperation();
                        break;
                    case 21: // Opcode.Index
                        ExecuteIndexOperation();
                        break;
                    case 22: // Opcode.Call
                        ExecuteCallOperation();
                        break;
                    case 23: // Opcode.Return
                        ExecuteReturnOperation();
                        break;
                    case 24: // Opcode.ReturnValue
                        ExecuteReturnValueOperation();
                        break;
                    case 25: // Opcode.SetLocal
                        ExecuteSetLocalOperation();
                        break;
                    case 26: // Opcode.GetLocal
                        ExecuteGetLocalOperation();
                        break;
                    case 27: // Opcode.GetBuiltIn
                        ExecuteGetBuiltInOperation();
                        break;
                    case 28: // Opcode.Closure
                        ExecuteClosureOperation();
                        break;
                    case 29: // Opcode.GetFree
                        ExecuteGetFreeOperation();
                        break;
                    case 30: // Opcode.GetCurrentClosure
                        ExecuteGetCurrentClosureOperation();
                        break;
                }
            }
        }

        private VirtualMachineState InitializeState(List<byte> instructions, List<Object> constants, List<BuiltIn> builtIns)
        {
            var globalClosure = new Closure(instructions, new List<Object>());
            var globalFrame = new Frame(globalClosure, basePointer: 0);
            var frames = new Stack<Frame>();

            frames.Push(globalFrame);

            return new VirtualMachineState
            {
                BuiltIns = builtIns,
                Constants = constants,
                CurrentFrame = globalFrame,
                Frames = frames,
                Globals = internalState.Globals,
                Stack = new VirtualMachineStack()
            };
        }

        private int DecodeOperand(int length)
        {
            if (length == 1)
            {
                return internalState.CurrentFrame.Closure.Instructions[internalState.CurrentFrame.InstructionPointer];
            }
            
            var buffer = new byte[length];

            for (var i = 0; i < length; i++)
            {
                buffer[i] = internalState.CurrentFrame.Closure.Instructions[internalState.CurrentFrame.InstructionPointer + i];
            }

            return BitConverter.ToInt16(buffer, startIndex: 0);
        }

        private void ExecuteArrayOperation()
        {
            var count = DecodeOperand(2);
            var elements = new List<Object>();

            internalState.CurrentFrame.InstructionPointer += 2;

            for (var i = 0; i < count; i++)
            {
                elements.Add(internalState.Stack.Pop());
            }

            elements.Reverse();

            internalState.Stack.Push(Object.Create(ObjectKind.Array, elements));
        }

        private void ExecuteArrayIndexOperation(Object obj, Object index)
        {
            if (obj.Kind != ObjectKind.Array)
            {
                internalState.Stack.Push(Invariants["null"]);
                return;
            }

            var array = (List<Object>)obj.Value;

            if (index.Kind != ObjectKind.Integer || (int)index.Value < 0 || (int)index.Value >= array.Count)
            {
                internalState.Stack.Push(Invariants["null"]);
                return;
            }

            internalState.Stack.Push(array[(int)index.Value]);
        }

        private void ExecuteBangOperation()
        {
            Object operand = internalState.Stack.Pop();

            switch (operand.Kind)
            {
                case ObjectKind.Boolean:
                    internalState.Stack.Push(Invariants[!(bool)operand.Value]);
                    break;
                case ObjectKind.Null:
                    internalState.Stack.Push(Invariants[true]);
                    break;
                default:
                    internalState.Stack.Push(Invariants[false]);
                    break;
            }
        }

        private void ExecuteBooleanOperation(byte op)
        {
            switch (op)
            {
                case 7: // Opcode.True
                    internalState.Stack.Push(Invariants[true]);
                    break;
                case 8: // Opcode.False
                    internalState.Stack.Push(Invariants[false]);
                    break;
            }
        }

        private void ExecuteBinaryOperation(byte op)
        {
            Object right = internalState.Stack.Pop();
            Object left = internalState.Stack.Pop();

            if (left.Kind != right.Kind)
            {
                var info = new ErrorInfo
                {
                    Code = ErrorCode.BinaryOperationInvalidOperand,
                    Kind = ErrorKind.InvalidType,
                    Offenders = new List<object> { left, op, right },
                    Position = 2, // Consider right side as the offender
                    Source = ErrorSource.VM
                };

                internalState.Stack.Push(Object.Create(ObjectKind.Error, Error.Create(info)));
                return;
            }

            switch (left.Kind)
            {
                case ObjectKind.Boolean:
                    ExecuteBinaryBooleanOperation(op, (bool)left.Value, (bool)right.Value);
                    break;
                case ObjectKind.String:
                    ExecuteBinaryStringOperation(op, left.Value.ToString(), right.Value.ToString());
                    break;
                default:
                    ExecuteBinaryIntegerOperation(op, (int)left.Value, (int)right.Value);
                    break;
            }
        }

        private void ExecuteBinaryBooleanOperation(byte op, bool left, bool right)
        {
            switch (op)
            {
                case 9:  // Opcode.Equal
                    internalState.Stack.Push(left == right ? Invariants[true] : Invariants[false]);
                    break;
                case 10: // Opcode.NotEqual
                    internalState.Stack.Push(left != right ? Invariants[true] : Invariants[false]);
                    break;
            }
        }

        private void ExecuteBinaryIntegerOperation(byte op, int left, int right)
        {
            switch (op)
            {
                case 2:  // Opcode.Add
                    internalState.Stack.Push(Object.Create(ObjectKind.Integer, left + right));
                    break;
                case 4:  // Opcode.Subtract
                    internalState.Stack.Push(Object.Create(ObjectKind.Integer, left - right));
                    break;
                case 5:  // Opcode.Multiply
                    internalState.Stack.Push(Object.Create(ObjectKind.Integer, left * right));
                    break;
                case 6:  // Opcode.Divide
                    internalState.Stack.Push(Object.Create(ObjectKind.Integer, left / right));
                    break;
                case 9:  // Opcode.Equal
                    internalState.Stack.Push(left == right ? Invariants[true] : Invariants[false]);
                    break;
                case 10: // Opcode.NotEqual
                    internalState.Stack.Push(left != right ? Invariants[true] : Invariants[false]);
                    break;
                case 11: // Opcode.GreaterThan
                    internalState.Stack.Push(left > right ? Invariants[true] : Invariants[false]);
                    break;
            }
        }

        private void ExecuteBinaryStringOperation(byte op, string left, string right)
        {
            switch (op)
            {
                case 2: // Opcode.Add
                    internalState.Stack.Push(Object.Create(ObjectKind.String, string.Join(String.Empty, left, right)));
                    break;
            }
        }

        private void ExecuteBuiltInCallOperation(Object obj, int arity, int basePointer)
        {
            List<Object> args = new List<Object>();
            var end = basePointer + arity + 1;

            for (var i = basePointer + 1; i < end; i++)
            {
                args.Add(internalState.Stack[i]);
            }

            var fn = (Func<List<Object>, Object>)obj.Value;
            var result = fn(args);

            internalState.Stack.Push(result);
        }

        private void ExecuteCallOperation()
        {
            var arity = DecodeOperand(1);
            var basePointer = internalState.Stack.Count - arity - 1;
            var fn = internalState.Stack[basePointer];

            internalState.CurrentFrame.InstructionPointer += 1;

            switch (fn.Kind)
            {
                case ObjectKind.BuiltIn:
                    ExecuteBuiltInCallOperation(fn, arity, basePointer);
                    break;
                default:
                    ExecuteFunctionCallOperation(fn, arity, basePointer);
                    break;
            }
        }

        private void ExecuteClosureOperation()
        {
            var index = DecodeOperand(2);
            internalState.CurrentFrame.InstructionPointer += 2;

            var freeCount = DecodeOperand(1);
            internalState.CurrentFrame.InstructionPointer += 1;

            var frees = new List<Object>();

            for (var i = 0; i < freeCount; i++)
            {
                frees.Add(internalState.Stack[internalState.Stack.Count - freeCount + i]);
            }

            var fn = internalState.Constants[index];
            var closure = Object.Create(ObjectKind.Closure, new Closure((List<byte>)fn.Value, frees));

            internalState.Stack.Push(closure);
        }

        private void ExecuteConstantOperation()
        {
            internalState.Stack.Push(internalState.Constants[DecodeOperand(2)]);
            internalState.CurrentFrame.InstructionPointer += 2;
        }

        private void ExecuteFunctionCallOperation(Object fn, int arity, int basePointer)
        {
            PushFrame(new Frame((Closure)fn.Value, basePointer));
            PushArguments(arity);

            // Merge closure (free) variables into locals, since we are doing
            // the same in the symbol table
            internalState.CurrentFrame.Locals.AddRange(internalState.CurrentFrame.Closure.Frees);
        }

        private void ExecuteGetBuiltInOperation()
        {
            var index = DecodeOperand(1);
            internalState.CurrentFrame.InstructionPointer += 1;
            internalState.Stack.Push(internalState.BuiltIns[index].Function);
        }

        private void ExecuteGetCurrentClosureOperation()
        {
            internalState.Stack.Push(Object.Create(ObjectKind.Closure, internalState.CurrentFrame.Closure));
        }

        private void ExecuteGetFreeOperation()
        {
            var index = DecodeOperand(1);
            internalState.CurrentFrame.InstructionPointer += 1;
            internalState.Stack.Push(internalState.CurrentFrame.Closure.Frees[index]);
        }

        private void ExecuteGetGlobalOperation()
        {
            var index = DecodeOperand(2);
            internalState.CurrentFrame.InstructionPointer += 2;
            internalState.Stack.Push(internalState.Globals[index]);
        }

        private void ExecuteGetLocalOperation()
        {
            var index = DecodeOperand(1);
            internalState.CurrentFrame.InstructionPointer += 1;
            internalState.Stack.Push(internalState.CurrentFrame.Locals[index]);
        }

        private void ExecuteHashOperation()
        {
            var count = DecodeOperand(2);
            var hash = new Dictionary<string, Object>();
            var keys = new List<string>();
            var values = new List<Object>();

            internalState.CurrentFrame.InstructionPointer += 2;

            for (var i = 0; i < count; i++)
            {
                values.Add(internalState.Stack.Pop());
                keys.Add(internalState.Stack.Pop().Value.ToString().ToLower());
            }

            keys.Reverse();
            values.Reverse();

            for (var i = 0; i < keys.Count; i++)
            {
                hash.Add(keys[i], values[i]);
            }

            internalState.Stack.Push(Object.Create(ObjectKind.Hash, hash));
        }

        private void ExecuteHashIndexOperation(Object obj, Object index)
        {
            if (obj.Kind != ObjectKind.Hash)
            {
                internalState.Stack.Push(Invariants["null"]);
                return;
            }

            var hash = (Dictionary<string, Object>)obj.Value;

            if (index.Kind != ObjectKind.Integer && index.Kind != ObjectKind.Boolean && index.Kind != ObjectKind.String)
            {
                internalState.Stack.Push(Invariants["null"]);
                return;
            }

            var key = hash.Keys.Where(item => item == index.Value.ToString().ToLower()).FirstOrDefault();

            internalState.Stack.Push(key != default(string) ? hash[key] : Invariants["null"]);
        }

        private void ExecuteIndexOperation()
        {
            var index = internalState.Stack.Pop();
            var left = internalState.Stack.Pop();

            switch (left.Kind)
            {
                case ObjectKind.Array:
                    ExecuteArrayIndexOperation(left, index);
                    break;
                case ObjectKind.Hash:
                    ExecuteHashIndexOperation(left, index);
                    break;
            }
        }

        private void ExecuteJumpOperation()
        {
            internalState.CurrentFrame.InstructionPointer = DecodeOperand(2);
        }

        private void ExecuteJumpNotTruthyOperation()
        {
            var position = DecodeOperand(2);

            internalState.CurrentFrame.InstructionPointer += 2;
            var condition = internalState.Stack.Pop();

            if (!IsTruthy(condition))
            {
                internalState.CurrentFrame.InstructionPointer = position;
            }
        }

        private void ExecuteMinusOperation()
        {
            Object operand = internalState.Stack.Pop();

            if (operand.Kind != ObjectKind.Integer)
            {
                return;
            }

            int value = (int)operand.Value;

            internalState.Stack.Push(Object.Create(ObjectKind.Integer, -value));
        }

        private void ExecuteNullOperation()
        {
            internalState.Stack.Push(Invariants["null"]);
        }

        private void ExecuteReturnOperation()
        {
            internalState.Stack.ResetTo(internalState.CurrentFrame.Base);

            PopFrame();

            internalState.Stack.Push(Invariants["null"]);
        }

        private void ExecuteReturnValueOperation()
        {
            var value = internalState.Stack.Pop();

            internalState.Stack.ResetTo(internalState.CurrentFrame.Base);

            PopFrame();

            internalState.Stack.Push(value);
        }

        private void ExecuteSetGlobalOperation()
        {
            // Just skip the operand, we are not using it now
            internalState.CurrentFrame.InstructionPointer += 2;
            internalState.Globals.Add(internalState.Stack.Pop());
        }

        private void ExecuteSetLocalOperation()
        {
            internalState.CurrentFrame.InstructionPointer += 1;
            internalState.CurrentFrame.Locals.Add(internalState.Stack.Pop());
        }

        private void PopFrame()
        {
            // Do not pop up global frame
            if (internalState.Frames.Count > 1)
            {
                internalState.Frames.Pop();
                internalState.CurrentFrame = internalState.Frames.First();
            }
        }

        private void PushArguments(int count)
        {
            // We need to skip function object
            var start = internalState.CurrentFrame.Base + 1;

            for (var i = 0; i < count; i++)
            {
                internalState.CurrentFrame.Locals.Add(internalState.Stack[start + i]);
            }
        }

        private void PushFrame(Frame frame)
        {
            internalState.Frames.Push(frame);
            internalState.CurrentFrame = frame;
        }
    }
}
