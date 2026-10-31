# Boundary-initial narrowing probe

A post-nasal rewrite narrows the boundary-initial `+b` sequence to `m`. This fixture pins the positive surface `nmo`, the forbidden unnarrowed spelling `nbo`, and the bare root `n`. It exposed a junction-probe defect in [PanGloss issue #14](https://github.com/sillsdev/PanGloss/issues/14): the inserted `m` was discarded, and the ordinary suffix path missed `nmo` unless a fusion composite masked the defect.

The expected signatures were recorded by C# adapter replay at Machine commit `aab4c59819adb7b366915e09df2fb28c62b8c03a`: `nmo` gives `KAN+BO|nmo`, `nbo` has no analysis, and `n` gives `KAN|n`. The same results were observed at Machine commit `c2454760181d77a3284482483cf78d921a2c4b9b`.
