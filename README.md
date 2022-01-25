# Machine for .NET
Machine is a natural language processing library. It is specifically focused on providing tools and techniques that are useful for processing languages that are very resource-poor. The library is also useful as a foundation for building more advanced language processing techniques. The library currently only provides a basic set of algorithms, but the goal is to include many more in the future.

## Features

### Translation
Machine provides a set of translation engines. It currently includes a SMT engine based on a fork of [Thot](https://github.com/sillsdev/thot) and a rule-based engine based on the HermitCrab morhphological parser.

### Morphology
Machine contains a rule-based morphological/phonological parser called HermitCrab.

### Feature Structures
Machine provides a flexible implementation of feature structures with efficient unification, subsumption, and priority union operations. Feature values can be atomic symbols, strings, or variables.

### Annotations
An annotation is a tagged portion of data with its associated metadata. The metadata for an annotation is represented as a feature structure, which is essentially a set of feature-value pairs. Annotations can also be hierarchical; an annotation can contain other annotations. Annotations are normally used on textual data, but Machine can support annotations on any type of data.

### Patterns
Machine contains a regex-like pattern matching engine. Machine is different than most pattern matching engines, which specify patterns that match strings of characters. Instead, Machine can specify patterns that match annotations on data. An annotation describes the metadata for a part of the data. Data can be tagged in any way that is desired. For example, all the words in a document can be tagged with their part of speech. Because Machine works on metadata, instead of the underlying data, it provides a very powerful, flexible pattern matching capability that is difficult to duplicate with normal regular expressions. Machine compiles patterns in to a format that allows for efficient matching (in most cases, linear to the number of annotations on the input).

A pattern in Machine supports many of the features that normal regular expressions support, such as alternation,
repetition, Kleene star, optionality, capturing groups, etc. It does not support backtracking. As mentioned earlier, the patterns are not matched against characters, but instead against feature structures, since this is how annotations are represented. Machine does not check for exact matches between feature structures, but uses an operation called unification. Unification is a way of combining two feature structures, but only if they are compatible. Two feature structures are not compatible, if they have contradictory values for the same feature. An annotation matches a feature structure constraint in a pattern if the feature structures can be unified. Machine patterns handle matching of hierarchical annotations by searching for matches in a depth-first manner.

Patterns are represented as finite state automata (FSA). FSAs provide a natural model for the type of regular languages that Machine patterns represent. In addition, FSAs can be determinized so that pattern matching can be
performed efficiently.

### Rules
Machine also provides a rules module, which can be used to specify rules for manipulating annotated data. Pattern
rules provide a mechanism for modifying parts of data that match the specified pattern. Rule application behavior
is specified as code. Pattern rules can be applied iteratively or simultaneously. Rules can be aggregated using rule batches and rule cascades. Rule batches can be used to apply a set of rules disjunctively. Rule cascades can be used to apply multiple rules in successive order.

### Statistical Methods

#### Probability Distributions
Machine includes various methods for estimating probability distributions from observed data. The current discounting techniques include Witten-Bell, Simple Good-Turing, maximum likelihood, and Lidstone.

#### *n*-gram Model
Machine includes a generic *n*-gram model implementation. The *n*-gram model is smoothed using Modified Kneser-Ney smoothing.

### Clustering
Machine provides implementations of various clustering algorithms. These include density-based algorithms, such as DBSCAN and OPTICS, and hierarchical algorithms, such as UPGMA and Neighbor-joining.

### Sequence Alignment

#### Pairwise
Pairwise sequence alignment is implemented using a dynamic programming approach similar to most common implementations of the Levenshtein distance. It supports substitution, insertion, deletion, expansion, and compression. It also supports the following alignment modes: global, local, half-local, and semi-global.

#### Multiple
The implementation of multiple sequence alignment is based on the [CLUSTAL W algorithm](https://www-bimas.cit.nih.gov/clustalw/clustalw.html).

### Stemming
Machine provides an unsupervised stemming algorithm specifically designed for resource-poor languages. The stemmer is trained using a list of words either derived from a corpus or a lexicon. The algorithm can also be used to identify possible affixes. It is based on the unsupervised stemming algorithm proposed in Harald Hammarström's [doctoral dissertation](http://aflat.org/files/phd.pdf).

## Installation

Machine is available as a set of NuGet packages:

- [SIL.Machine](https://www.nuget.org/packages/SIL.Machine/): core library
- [SIL.Machine.Translation.Thot](https://www.nuget.org/packages/SIL.Machine.Translation.Thot/): statistical machine translation and word alignment
- [SIL.Machine.Morphology.HermitCrab](https://www.nuget.org/packages/SIL.Machine.Morphology.HermitCrab/): rule-based morphological parsing
- [SIL.Machine.WebApi](https://www.nuget.org/packages/SIL.Machine.WebApi/): ASP.NET Core web API middleware

Machine is also available as a command-line tool that can be installed as a .NET tool.

```
dotnet tool install -g SIL.Machine.Tool
```

## Tutorials

If you would like to find out more about how to use Machine, check out the tutorial Jupyter notebooks:

- [Text Corpora](samples/corpora.ipynb)