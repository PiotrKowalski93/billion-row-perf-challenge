using System;
using System.Collections.Generic;
using System.Text;

namespace Level4_MMF
{
    public class PointerExamples
    {
        // =============================================================================
        // C# Unsafe & String Pointer Basics
        // =============================================================================

        public PointerExamples()
        {
            Section("1 — Managed String: Memory Model");
            {
                // In .NET, strings are immutable and stored on the heap.
                // Every "" literal or operation creates a new heap allocation.
                string s = "Hello"; // 0x00001

                // length + 2 bytes per character (UTF-16)
                // Object header (8) + Method table ptr (8) + Length (4) + chars (n×2)
                Console.WriteLine($"  Value      : {s}");
                Console.WriteLine($"  Length     : {s.Length}  (character count)");
                Console.WriteLine($"  [2]        : {s[2]}  — indexer, no new allocation");

                // s[0] = 'X'  → ❌ compile error — string is immutable
            }

            Section("2 — fixed: Preventing GC from Moving the String");
            {
                // GC can move objects on the heap (compaction).
                // To take a pointer, the object must be pinned against GC movement.
                // During the 'fixed' block, GC cannot move this object.

                string s = "Hello"; // 0x00001

                unsafe
                {
                    fixed (char* ptr = s)   // s is pinned, ptr = address of first char
                    {
                        Console.WriteLine($"  ptr address: 0x{(nint)ptr:X}");
                        Console.WriteLine($"  ptr[0]     : {ptr[0]}   (= s[0])");
                        Console.WriteLine($"  ptr[1]     : {ptr[1]}   (= s[1])");

                        // Arithmetic: ptr + n → n characters ahead (each 2 bytes)
                        Console.WriteLine($"  *(ptr+2)   : {*(ptr + 2)}   (= s[2])");
                    }
                    // Block ended → 'fixed' released, GC can move again
                }
            }

            Section("3 — String Character Reading with Pointer (vs indexer)");
            {
                // Indexer: bounds check on every access  → safe but slight overhead
                // Pointer : no bounds check              → fast but programmer is responsible
                string s = "Hello, World!";

                unsafe
                {
                    fixed (char* ptr = s)
                    {
                        // ---- Forward scan with pointer ----
                        Console.Write("  With pointer: ");
                        char* p = ptr;
                        while (*p != '\0')      // .NET strings are not null-terminated, but
                        {                       // fixed char* includes a null terminator
                            Console.Write(*p);
                            p++;                // advances 2 bytes (sizeof(char))
                        }
                        Console.WriteLine();

                        // ---- Comparison with indexer ----
                        Console.Write("  With indexer: ");

                        for (int i = 0; i < s.Length; i++)
                            Console.Write(s[i]);

                        Console.WriteLine();
                    }
                }
            }

            Section("4 — Pointer Arithmetic: sizeof and Address Difference");
            {
                unsafe
                {
                    Console.WriteLine($"  sizeof(char)  : {sizeof(char)}  byte  (UTF-16)");
                    Console.WriteLine($"  sizeof(byte)  : {sizeof(byte)}  byte");
                    Console.WriteLine($"  sizeof(int)   : {sizeof(int)}  byte");
                    Console.WriteLine($"  sizeof(long)  : {sizeof(long)}  byte");

                    string s = "ABCD";
                    fixed (char* ptr = s)
                    {
                        char* pA = &ptr[0];
                        char* pD = &ptr[3];

                        // Pointer difference: how many 'char' apart?
                        long charDistance = pD - pA;
                        // Byte difference
                        long byteDistance = (byte*)pD - (byte*)pA;

                        Console.WriteLine($"\n  &s[0] = 0x{(int)pA:X}");
                        Console.WriteLine($"  &s[3] = 0x{(int)pD:X}");
                        Console.WriteLine($"  Diff  = {charDistance} char  = {byteDistance} bytes");
                    }
                }
            }

            Section("5 — String Mutation: Modifying with Pointer (Dangerous!)");
            {
                // String is immutable — but we can force it with unsafe.
                // ⚠️ This should NEVER be used in real code:
                //    • String interning breaks (same literal changes everywhere)
                //    • Can lead to undefined behavior
                // Shown only to answer the question "what happens?"

                string s = new string("Harmless");   // new string() → non-interned copy

                Console.WriteLine($"  Before: {s}");

                unsafe
                {
                    fixed (char* ptr = s)
                    {
                        ptr[0] = 'D';   // 'H' → 'D'
                        ptr[1] = 'a';   // 'a' → 'a'
                        ptr[2] = 'n';   // 'r' → 'n'
                    }
                }

                Console.WriteLine($"  After : {s}");   // "Danmless" — string was mutated
            }

            Section("6 — ReadOnlySpan<char> vs Pointer: Modern Alternative");
            {
                // Safe way to achieve similar performance without using pointers.
                // Span<T> / ReadOnlySpan<T>:
                //   • Bounds check present (in debug) — but JIT optimizes most of them away
                //   • GC-compatible, no 'fixed' required
                //   • No unsafe block required

                string s = "Hello, World!";

                ReadOnlySpan<char> span = s.AsSpan();
                ReadOnlySpan<char> sub = span.Slice(7, 5);   // "World" — no allocation

                Console.WriteLine($"  Full string: {s}");
                Console.WriteLine($"  Span[7..12]: {sub}");
                Console.WriteLine($"  span[0]    : {span[0]}");

                // When you want a pointer:
                unsafe
                {
                    fixed (char* ptr = span)
                        Console.WriteLine($"  span ptr   : 0x{(int)ptr:X}  (same address as s's first char)");

                    fixed (char* ptr = s)
                        Console.WriteLine($"  s    ptr   : 0x{(int)ptr:X}");
                }
                // → Both addresses are the same; Span doesn't copy, it points to the same memory.
            }

        }

        static void Section(string sectionName)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"--- {sectionName} ---");
            Console.ResetColor();
        }
    }
}
