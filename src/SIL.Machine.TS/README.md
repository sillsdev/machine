# Machine for TypeScript/JavaScript

Machine is a natural language processing library. It is specifically focused on providing tools and techniques that are useful for processing languages that are very resource-poor. The library is also useful as a foundation for building more advanced language processing techniques. The library currently only provides a basic set of algorithms, but the goal is to include many more in the future.

## Features

### Tokenization

Machine provides a set of word and segment tokenizers.

### Translation

Machine provides a machine translation client for the Machine Web API server. The translation engine is located on the server. The client APIs are used to initiate translation processes on the server. For interactive translation, the remote translation engine returns all possible translations for a source segment in a word graph. The client can efficiently search the graph for best translation suffix based on a provided prefix.

## Installation

```sh
npm install @sillsdev/machine
```
