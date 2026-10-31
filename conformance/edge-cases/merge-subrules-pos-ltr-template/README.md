# POS-gated merge under a mandatory template: leftToRightIterative

This is the `leftToRightIterative` counterpart of `merge-subrules-pos-rtl-template`.
The rewrite order is the only behavioral change; all eight words, complete analysis signatures,
rule-trace sets and negative controls remain the same. It checks that the noun and verb
subrules keep their POS gates and that the verb analysis survives under its mandatory suffix.
The two-site noun `FF` also checks both merge sites.

C# founding-oracle self-check and the PanGloss shared replay exercise this fixture independently.
PanGloss separately keeps strategy-specific complete-identity assertions for all three orders;
individual FST strategies may intentionally refuse an order that another strategy covers.
