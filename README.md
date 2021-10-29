# Lforth
An experiment with a 3 stack forth variant

Concepts: We have 3 stacks: Data, Return, Instruction.

Each point on the Data stack can be another stack, which we can build by `pivot`
and a few other words.

The return stack is normal, and stores call frames.

The instruction stack allows us to define more expressive and natural syntax
without the need for many 'special' words in the interpreter.

## The pivot

Push a bunch of data to the stack
```
1 2 3 4 5 6 7
```
then pivot
```
3 pivot

         7
         6
1 2 3 4 [5]
```
we can have new stack words to do neat tricks
```
topple

1 2 3 4  7 6 5
```
or
```
flip 

         5
         6
1 2 3 4 [7]
```
You can still push after this
```
1 2 3 4 5 6 7  3 pivot  8 9 10  2 pivot

         7
         6     10
1 2 3 4 [5] 8 [ 9]
```

Having the pivot point allows us to take var-args in a nice way

```
1 2 3 4 [5 (6,7)]

/sum 0 do pull + while more done

=> 1 2 3 4 18
```
Where `pull` takes data from the pivot-at-head, and `more` returns true iff the
pivot is not empty.

## Word classes

By default, words are executed immediately. Quoted blocks go on an instruction
stack, as a single unit. Instruction quotes are started with `[` and end with `]`.

The quotes can be nested. (to-do: work out how the stack works with nested quotes).

To-do: figure out a minimal setup so we can do a for loop.

Define a word:
```
[ . . . ] "new-word" def
```

Run a code block immediately
```
[ run me now ] call
```

Conditional code:
```
[ this code might run ] [ test ] if
[ maybe this ] [ or this ] [ test ] ifelse
```

Duplicate or drop code:
```
[ this code runs twice ] code-dup call call
[ I will run ] [ this code won't run ] code-drop call
```

Exit running block and pop from stack
```
[ [ inner code ] call end; [ never runs ] ]
```

Return to start of current code block (goto like):
```
[ ... run this forever ... loop; ]
```
(therefore, don't do `[loop;]`)

Duplicate a code-block by index
```
[ run this ] [ not this ] 1 code-dup call drop drop
```

## Tagging stack items

Ability to reference an index on the stack without juggling.

`0 idx` is the same as `dup`

`1 idx` gives us a duplicate of an item one back (etc)

so
`1 2 3 4 5 6   4 idx`
results in
`1 2 3 4 5 6 2`

and
`1 2 3   1 idx 2 idx + + ` -> `1 2 6`


# Scratchpad
Thoughts and stuff

```
[ if-case ] [ else-case ] [ test ] ifelse

'#' as a prefix to operate on code stack?

[
    call  ( run the test - data stack has 0 if false, -1 if true )
    1 * ( now 1 if true, 0 if false )
    # idx ( copy the appropriate block )
    call ( run the code )
    # drop # drop ( wipe both cases - does not affect data stack )
] "ifelse" def
```