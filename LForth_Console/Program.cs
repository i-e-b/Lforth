using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LForth_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var program = File.ReadAllText("SampleProgram.lf");
            
            var finalState = LForthCore.Interpret(program);
            
            Console.WriteLine("Final state:");
            Console.WriteLine(finalState.ToString());
        }
    }

    public class LForthCore
    {
        private static readonly char[] _whitespace = {' ', '\r', '\n', '\t'};

        public static InterpreterState Interpret(string? program)
        {
            if (string.IsNullOrWhiteSpace(program)) return InterpreterState.Invalid("No program supplied");
            var words = program.Split(_whitespace, StringSplitOptions.RemoveEmptyEntries);
            
            var state = new InterpreterState();
            state.QuoteStack.Push(new CodeQuote(words)); // push the entire program to the bottom of the quote stack
            
            Run(state);
            
            return state;
        }

        private static void Run(InterpreterState state)
        {
            while (state.IsActive())
            {
                var isNoOp = state.CurrentWord(out var word);
                if (isNoOp) continue;
                
                if (IsReserved(word, out var action))
                {
                    action?.Invoke(state);
                }
                else if (state.Definitions.ContainsKey(word))
                {
                    state.PushFrame(state.Definitions[word]); // call a subroutine
                }
                else
                {
                    // parse data is either an atom or a number.
                    // strings are handled by the " reserved word.
                    state.PushData(DataItem.Parse(word));
                }
            }
        }

        private static bool IsReserved(string word, out Action<InterpreterState>? action)
        {
            switch (word)
            {
                case "[": // following words are pushed into the head quote (?)
                    action = (s) => StartQuote(s);
                    return true;

                case "]": // (?) push the head quote and drop down one (?)
                    action = (s) => EndQuote(s);
                    return true;

                case "call": // run the quote at the top of the code stack
                    action = (s) => CallQuote(s);
                    return true;

                case "idx": // dup the item at index, negative index is no-op
                    // (index popped from data stack). If an atom is on the data stack, switch stack [data, code]
                    action = (s) => DataIndex(s);
                    return true;

                case "def":
                    action = (s) => DefineWord(s);
                    return true;

                case "*":
                case "/":
                case "-":
                case "+":
                case "%":
                case "^":
                    action = s => NumericOp(s, word);
                    return true;

                default:
                    action = null;
                    return false;
            }
        }
    }

    public class InterpreterState
    {
        public bool Success { get; set; } = false;
        public string? ErrorMessage { get; init; }
        
        public Stack<DataItem> DataStack { get; } = new();
        public Stack<CodeQuote> QuoteStack { get; } = new();
        public Stack<ProgramFrame> ReturnStack { get; } = new();

        public Dictionary<string, CodeQuote> Definitions { get; } = new();
        public int Position;
        public StateFlags MachineState = StateFlags.NoFlags;

        public static InterpreterState Invalid(string message)
        {
            return new() { ErrorMessage = message };
        }

        public override string ToString()
        {
            return $"{(Success?"OK":"FAIL- "+ErrorMessage)}\r\n{string.Join(", ",DataStack.Select(d=>d.ToString()))}";
        }

        public bool IsActive()
        {
            if (Success) return false; // we decided to exit
            if ((MachineState & StateFlags.Halt) != 0) return false; // we hit an exit condition
            if (QuoteStack.Count > 1) return true; // we are running a non-root quote
            return Position < QuoteStack.Peek().Words.Count; // still have more words to run
        }

        public bool CurrentWord(out string? wordResult)
        {
            const bool noOp = true, normalOp = false;
            wordResult = null;
            
            if (QuoteStack.Count < 1)
            {
                MachineState = StateFlags.Halt;
                return noOp;
            }
            var currentQuote = QuoteStack.Peek();

            if (Position >= currentQuote.Words.Count)
            {
                // end of quote. Use return stack.
                Position = ReturnStack.Pop().Position;
                QuoteStack.Pop();
                return noOp;
            }
            
            wordResult = currentQuote.Words[Position];
            return normalOp;
        }
    }

    [Flags]
    public enum StateFlags
    {
        NoFlags = 0,
        QuoteStackSelected = 1,
        Halt = 256
    }

    public class ProgramFrame
    {
        public int Position;
        // TODO: something to pop quotes
    }

    public class CodeQuote
    {
        public readonly List<string> Words = new();

        public CodeQuote() { }
        public CodeQuote(string[] words) { Words.AddRange(words); }
    }

    public class DataItem
    {
        public static DataItem Parse(string word)
        {
            if (int.TryParse(word, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                return new DataItem {Type = DataType.Integer, Value = intValue};

            if (double.TryParse(word, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleResult))
                return new DataItem {Type = DataType.Float, Value = doubleResult};
            
            // ... any other types ...
            
            return new DataItem{ Type = DataType.Atom, Value = word};
        }

        public DataType Type { get; set; }

        public object? Value { get; set; }
    }

    public enum DataType
    {
        NoValue,
        NoResult,
        Integer,
        Float,
        String,
        Atom
    }
}