#!/usr/bin/env python3
"""Generates the early-lexicon-agglutinative stress grammar and its words file.

A new FieldWorks project in a Quechua/Turkic-type language: one unordered stratum of 25 suffixal rules (pure
concatenation, multipleApplication 1, several with 1-2 segment allomorphs), a vowel-harmony phonological rule in
the same stratum (a surfaces as e after a front vowel and any consonants), 20 roots, and 60 words: 20 with a
lexicon root and 40 whose root is absent from the lexicon, each carrying 3-6 suffixes.
usage: generate.py   (writes grammar.xml and words.txt beside the script)
"""
import os, random
from xml.sax.saxutils import escape

here = os.path.dirname(os.path.abspath(__file__))
FEATS = ["syl", "cons", "son", "nas", "voice", "cont", "high", "low", "back", "round"]
V = dict(syl="+", cons="-", son="+", nas="-", voice="+", cont="+", high="-", low="-", back="-", round="-")
C = dict(syl="-", cons="+", son="-", nas="-", voice="+", cont="-", high="-", low="-", back="-", round="-")


def seg(base, **kw):
    d = dict(base)
    d.update(kw)
    return d


SEGS = {
    "a": seg(V, low="+", back="+"),
    "e": seg(V),
    "i": seg(V, high="+"),
    "o": seg(V, back="+", round="+"),
    "u": seg(V, high="+", back="+", round="+"),
    "b": seg(C),
    "d": seg(C, high="+"),
    "g": seg(C, back="+"),
    "k": seg(C, voice="-", back="+"),
    "l": seg(C, son="+", cont="+"),
    "m": seg(C, son="+", nas="+"),
    "n": seg(C, son="+", nas="+", high="+"),
    "p": seg(C, voice="-"),
    "r": seg(C, son="+", cont="+", high="+"),
    "s": seg(C, voice="-", cont="+"),
    "t": seg(C, voice="-", high="+"),
    "y": seg(C, cons="-", son="+", cont="+", high="+"),
}
assert len({tuple(sorted(d.items())) for d in SEGS.values()}) == len(SEGS)
FRONT = "ei"
CONS = "bdgklmnprsty"
# (gloss, allomorphs by underlying form); 25 rules, 7 with 1-2 segment allomorphs
RULES = [
    ("PL", ["lar"]), ("P1", ["um"]), ("P2", ["un"]), ("P3", ["si", "i"]), ("ACC", ["ni", "yi"]),
    ("DAT", ["ka", "a"]), ("LOC", ["da"]), ("ABL", ["dan"]), ("GEN", ["nin"]), ("INST", ["la"]),
    ("COM", ["bla"]), ("EQU", ["ga"]), ("NEG", ["ma"]), ("PAST", ["di", "ti"]), ("EVID", ["mis"]),
    ("FUT", ["asak"]), ("COND", ["sa"]), ("IMP", ["sin"]), ("ABIL", ["abil"]), ("CAUS", ["tir"]),
    ("PASS", ["il", "n"]), ("RECIP", ["is"]), ("REFL", ["in"]), ("DESID", ["gi"]), ("AGENT", ["ki"]),
]
ROOTS_BACK = ["kat", "pol", "mur", "tok", "bas", "gun", "dar", "lop", "sut", "nok"]
ROOTS_FRONT = ["kel", "pir", "sen", "bil", "des", "git", "ner", "tip", "mek", "lis"]
ROOTS = ROOTS_BACK + ROOTS_FRONT
assert len(RULES) == 25 and len(ROOTS) == 20


def harmony(s):
    out = []
    for ch in s:
        if ch == "a":
            j = len(out) - 1
            while j >= 0 and out[j] in CONS:
                j -= 1
            if j >= 0 and out[j] in FRONT:
                ch = "e"
        out.append(ch)
    return "".join(out)


def grammar():
    o = ['<?xml version="1.0" encoding="utf-8"?>', '<!DOCTYPE HermitCrabInput SYSTEM "HermitCrabInput.dtd">', "<HermitCrabInput>", "  <Language>"]
    o.append("    <Name>early-lexicon-agglutinative</Name>")
    o.append('    <PartsOfSpeech><PartOfSpeech id="posX"><Name>x</Name></PartOfSpeech></PartsOfSpeech>')
    o.append("    <PhonologicalFeatureSystem>")
    for f in FEATS:
        o.append(f'      <SymbolicFeature id="f_{f}"><Name>{f}</Name><Symbols><Symbol id="f_{f}_p">+</Symbol><Symbol id="f_{f}_m">-</Symbol></Symbols></SymbolicFeature>')
    o.append("    </PhonologicalFeatureSystem>")
    o.append("    <HeadFeatures />")
    o.append('    <CharacterDefinitionTable id="table1"><Name>Main</Name><SegmentDefinitions>')
    for s, d in SEGS.items():
        fv = "".join(f'<FeatureValue feature="f_{f}" symbolValues="f_{f}_{"p" if d[f] == "+" else "m"}" />' for f in FEATS)
        o.append(f'      <SegmentDefinition id="c_{s}"><Representations><Representation>{s}</Representation></Representations>{fv}</SegmentDefinition>')
    o.append("    </SegmentDefinitions></CharacterDefinitionTable>")
    o.append("    <NaturalClasses>")
    o.append('      <FeatureNaturalClass id="ncAny"><Name>Any</Name></FeatureNaturalClass>')
    o.append('      <FeatureNaturalClass id="ncFront"><Name>FrontV</Name><FeatureValue feature="f_syl" symbolValues="f_syl_p" /><FeatureValue feature="f_back" symbolValues="f_back_m" /></FeatureNaturalClass>')
    o.append('      <FeatureNaturalClass id="ncCons"><Name>Cons</Name><FeatureValue feature="f_cons" symbolValues="f_cons_p" /></FeatureNaturalClass>')
    o.append("    </NaturalClasses>")
    o.append("    <PhonologicalRuleDefinitions>")
    o.append(
        '      <PhonologicalRule id="prule1"><Name>vowel harmony</Name>'
        '<PhoneticInput><PhoneticSequence><Segment segment="c_a" /></PhoneticSequence></PhoneticInput>'
        "<PhonologicalSubrules><PhonologicalSubrule>"
        '<PhoneticOutput><PhoneticSequence><Segment segment="c_e" /></PhoneticSequence></PhoneticOutput>'
        "<Environment><LeftEnvironment><PhoneticTemplate><PhoneticSequence>"
        '<SimpleContext naturalClass="ncFront" />'
        '<OptionalSegmentSequence min="0" max="-1"><SimpleContext naturalClass="ncCons" /></OptionalSegmentSequence>'
        "</PhoneticSequence></PhoneticTemplate></LeftEnvironment></Environment>"
        "</PhonologicalSubrule></PhonologicalSubrules></PhonologicalRule>"
    )
    o.append("    </PhonologicalRuleDefinitions>")
    ids = " ".join(f"mr_{g}" for g, _ in RULES)
    o.append(f'    <Strata><Stratum characterDefinitionTable="table1" morphologicalRuleOrder="unordered" phonologicalRules="prule1" morphologicalRules="{ids}"><Name>Main</Name>')
    o.append("<MorphologicalRuleDefinitions>")
    for g, allos in RULES:
        subs = ""
        for k, a in enumerate(allos):
            subs += (
                f'<MorphologicalSubrule id="sub_{g}_{k}"><MorphologicalInput><PhoneticSequence id="st_{g}_{k}">'
                '<OptionalSegmentSequence min="1" max="-1"><SimpleContext naturalClass="ncAny" /></OptionalSegmentSequence>'
                f'</PhoneticSequence></MorphologicalInput><MorphologicalOutput><CopyFromInput index="st_{g}_{k}" />'
                f"<InsertSegments><PhoneticShape>{a}</PhoneticShape></InsertSegments></MorphologicalOutput></MorphologicalSubrule>"
            )
        o.append(
            f'<MorphologicalRule id="mr_{g}" requiredPartsOfSpeech="posX" outputPartOfSpeech="posX"><Name>{g}</Name>'
            f"<MorphologicalSubrules>{subs}</MorphologicalSubrules><MorphemeId>{g}</MorphemeId><Gloss>{g}</Gloss></MorphologicalRule>"
        )
    o.append("</MorphologicalRuleDefinitions><LexicalEntries>")
    for r in ROOTS:
        o.append(f'<LexicalEntry id="e_{r}" partOfSpeech="posX"><Allomorphs><Allomorph id="a_{r}"><PhoneticShape>{r}</PhoneticShape></Allomorph></Allomorphs><MorphemeId>{r}</MorphemeId><Gloss>{r}</Gloss></LexicalEntry>')
    o.append("</LexicalEntries></Stratum></Strata></Language></HermitCrabInput>")
    return "\n".join(o) + "\n"


def words():
    rnd = random.Random(562)
    ws = []

    def chain(n):
        return rnd.sample(RULES, n)

    def surface(stem, rules):
        s = stem
        for _g, allos in rules:
            s = s + rnd.choice(allos)
        return harmony(s)

    for _ in range(20):
        ws.append(surface(rnd.choice(ROOTS), chain(rnd.randint(1, 3))))
    unknown = set()
    while len(unknown) < 40:
        stem = rnd.choice(CONS) + rnd.choice("aeiou") + rnd.choice(CONS)
        if stem not in ROOTS:
            unknown.add(stem)
    for stem in sorted(unknown):
        ws.append(surface(stem, chain(rnd.randint(3, 6))))
    rnd.shuffle(ws)
    return ws


with open(os.path.join(here, "grammar.xml"), "w", encoding="utf-8") as f:
    f.write(grammar())
with open(os.path.join(here, "words.txt"), "w", encoding="utf-8") as f:
    f.write("\n".join(words()) + "\n")
