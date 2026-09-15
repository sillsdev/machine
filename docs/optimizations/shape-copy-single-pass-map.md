# Build `Shape.CopyTo`'s node map in the copy pass

**Status:** retained (`Shape.CopyTo`).
**Portable:** yes, when a port has the same node-remapping requirement.

## Invariant

Annotations refer to nodes in the source shape and must be remapped to the corresponding nodes in the copy.
The source and destination nodes are already visited in matching order while cloning, so that loop can record
the mapping directly. No second `Zip`/`ToDictionary` traversal is required. An empty source range needs no
map; every nonempty range builds one because annotations are queried only after nodes have been copied.

## Implementation

`Shape.CopyTo` allocates the map on the first node of a nonempty range and fills it as each node is cloned.
Annotation ranges are then copied through that map exactly as before.

## Evidence

The change removes one enumeration and one LINQ-built dictionary from each annotated shape copy. It was not
timed independently from shape copy-on-write and feature-structure clone work, so no standalone ratio is
claimed.

## Correctness coverage and limits

`ShapeCopyToTests` covers empty ranges, leaf and spanning annotations, node/range remapping, and independence
of the copy.
The complete conformance-inclusive run passed 603 tests with one expected host-privilege skip.
