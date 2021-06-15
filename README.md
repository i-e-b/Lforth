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

By default, words are *not* executed immediately, but go on an instruction stack.
Different classes can cause eviction at different points. (I'm not 100% on how
this will work)

`do ... while ? done`

`do` is a high class word, so anything after it is not executed until we hit an
equal or higher word? Or do we push/pivot to allow nesting?

So we end up switching scope a few times

`{0} do { pull + } while { more } done`
`do { "hello, world" printline } repeat { 3 } times`

`call` will run all the instructions on the head pivot, leaving whatever output
on the value stack as normal

```
/do "do-block" setclass
/while "do-block" dup endclass setclass
        -- closes scope, causing an instruction stack pivot, starts new scope
/repeat "do-block" dup endclass setclass
/done "do-block" endclass
        ?????
        -- ends the scope. We now have a condition at the
        -- stack head and a code block to call after it
/times "do-block" endclass [[ something like: dup if 1 idx call ??? ]]
        do  1 - done -- push the decrement as an action
        loop[ >0 IF 2 idx call THEN ; ] dup call WHILE
        -- 'condition' is now a counter we can decrement.
        -- Keep calling the code block until it's zero
```

I *think* that we can implement pretty much any flow control with just a loop-while
special word.

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