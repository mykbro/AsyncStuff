# AsyncStuff

## Disclaimer
This codebase is made for self-teaching and educational purposes only.
Many features like input validation, object disposed checks, some exception handling, etc... are mostly missing.
As such this codebase cannot be considered production ready.

## What's this ?
This library contains an async implementation of different concurrency related classes that in the normal .Net framework don't have an async .Wait().

This library has been created from the need for an async BlockingQueue. In order to implement it, an async Monitor/ConditionVariable class was needed.
Other classes were then added when the need arose in other projects.

We also added a 'context switch free' SpinLock and SpinSemaphore.

## How does it work ?
All the async behaviours come from using the SemaporeSlim class behind the scenes with its .WaitAsync() method.
To know more about any single class please check its source code.

## How should I use this ?
It's a library... build it and link the assembly :)




