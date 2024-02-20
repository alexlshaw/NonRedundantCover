# NonRedundantCover

This code contains an implementation of the algorithm for generating a non-redundant cover of a table schema in the presence of null values given in [The implication problem of data dependencies over SQL table definitions: Axiomatic, algorithmic and logical characterizations, Sven and Link (2012)](https://researchspace.auckland.ac.nz/bitstream/handle/2292/19814/425link.pdf).

The main part of the code generates a random set of functional dependencies for a given table schema and null-free subschema, then uses the non-redundant cover algorithm to compute a minimal cover of that set. The purpose is to test the average cover size and generation performance.

The second part of the code prepares the data generated in the first part for graphing. 

Given that the possible selection space for functional dependencies is the power set of elements within the table schema, the generation process excludes trivial functional dependencies of the form $X=>Y : Y \subseteq X$

These trivial functional dependencies should always be removed, as in all cases, the closure of the attribute will be the same whether the trivial dependency is explicitly specified or not. This code also discards from the selection space functional dependencies that can be trivially derived (e.g. $AB => AC$ which can be trivially derived from $AB => C$).

More generally, the set of functional dependencies allowed are defined as $X=>Y : X \cap Y = \varnothing $

Note that despite this, the growth rate of the selection space is still $2^{n}$, so at around 20 attributes in the table schema the generation process starts taking a long time.
