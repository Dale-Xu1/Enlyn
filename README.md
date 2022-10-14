# Enlyn

## Overview

A compiled statically-typed class-based programming language. This is a toy programming language and it compiles to a custom-written interpreter, so it does not have any innovative features unfortunately.

## Setup

Clone the repository and cd into the Enlyn project. Then run the following command:
```bash
dotnet run <path>
```

An example program has been written into the file `Main.en`, so you can run the program to start and edit it as you want.

## Example Program

```
class Main : IO
{
    public new()
    {
        let a : A = new B()
        a.f()

        if a is null then return

        let i = 0
        while i < 10
        {
            this.out(i)
            i = i + 1
        }

        let name = this.in()
        this.out("Hi " + name)
    }
}

class A
{
    protected io : IO = new IO()
    public f() = this.io.out("A")
}

class B : A
{
    public override f() = this.io.out("B")
}
```
