using NUnit.Framework;
using SIL.ObjectModel;

namespace SIL.Machine.FeatureModel
{
    [TestFixture]
    public class FeatureStructTests
    {
        [Test]
        public void Unify()
        {
            Func<FeatureStruct, FeatureStruct, FeatureStruct?> resultsSelector = (fs1, fs2) =>
            {
                return fs1.Unify(fs2, out FeatureStruct res) ? res : null;
            };
            Func<FeatureStruct, FeatureStruct, VariableBindings, FeatureStruct?> varResultsSelector = (
                fs1,
                fs2,
                varBindings
            ) =>
            {
                return fs1.Unify(fs2, varBindings, out FeatureStruct res) ? res : null;
            };
            TestBinaryOperation(
                FreezableEqualityComparer<FeatureStruct?>.Default,
                resultsSelector,
                varResultsSelector,
                // simple
                featSys => null,
                featSys => FeatureStruct.New(featSys).Symbol("a2").Symbol("b1").Symbol("c2").Value,
                // complex
                featSys => null,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Symbol("a2"))
                        .Feature("cx2")
                        .EqualTo(cx2 => cx2.Symbol("b1"))
                        .Feature("cx3")
                        .EqualTo(cx2 => cx2.Symbol("c2"))
                        .Feature("cx4")
                        .EqualTo(cx4 => cx4.Symbol("d1"))
                        .Value,
                // re-entrant
                featSys => null,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a1"))
                        .Feature("cx2")
                        .ReferringTo(1)
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Symbol(1, "a1"))
                        .Feature("cx2")
                        .EqualTo(cx2 => cx2.Feature("a").ReferringTo(1))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a1"))
                        .Feature("cx2")
                        .ReferringTo(1)
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a2"))
                        .Feature("cx2")
                        .ReferringTo(1)
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Symbol(1, "a2"))
                        .Feature("cx2")
                        .EqualTo(cx2 => cx2.Feature("a").ReferringTo(1))
                        .Value,
                featSys => null,
                featSys => null,
                // cyclic
                featSys => null,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a2").Symbol("b2").Feature("cx2").ReferringTo(1))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a2").Symbol("b1").Symbol("c1").Feature("cx2").ReferringTo(1))
                        .Value,
                featSys => null,
                featSys => null,
                // variable
                featSys => null,
                featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
                featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
                featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
                featSys => null
            );

            var featureSystem = new FeatureSystem
            {
                new ComplexFeature("a"),
                new SymbolicFeature("b", new FeatureSymbol("c")),
                new ComplexFeature("d"),
                new SymbolicFeature("e", new FeatureSymbol("f")),
                new ComplexFeature("g"),
                new SymbolicFeature("h", new FeatureSymbol("j"))
            };

            FeatureStruct featStruct1 = FeatureStruct
                .New(featureSystem)
                .Feature("a")
                .EqualTo(a => a.Feature("b").EqualTo("c"))
                .Feature("d")
                .EqualTo(1, d => d.Feature("e").EqualTo("f"))
                .Feature("g")
                .ReferringTo(1)
                .Value;

            FeatureStruct featStruct2 = FeatureStruct
                .New(featureSystem)
                .Feature("a")
                .EqualTo(1, a => a.Feature("b").EqualTo("c"))
                .Feature("d")
                .ReferringTo(1)
                .Feature("g")
                .EqualTo(g => g.Feature("h").EqualTo("j"))
                .Value;

            FeatureStruct result;
            Assert.IsTrue(featStruct1.Unify(featStruct2, out result));

            featureSystem = new FeatureSystem
            {
                new ComplexFeature("a"),
                new SymbolicFeature("b", new FeatureSymbol("c")),
                new ComplexFeature("d"),
                new SymbolicFeature("e", new FeatureSymbol("f")),
                new ComplexFeature("g"),
                new SymbolicFeature("h", new FeatureSymbol("j")),
                new ComplexFeature("i")
            };

            featStruct1 = FeatureStruct
                .New(featureSystem)
                .Feature("a")
                .EqualTo(1, a => a.Feature("b").EqualTo("c"))
                .Feature("d")
                .EqualTo(2, d => d.Feature("e").EqualTo("f"))
                .Feature("g")
                .ReferringTo(2)
                .Feature("i")
                .ReferringTo(1)
                .Value;

            featStruct2 = FeatureStruct
                .New(featureSystem)
                .Feature("a")
                .EqualTo(1, a => a.Feature("b").EqualTo("c"))
                .Feature("d")
                .EqualTo(g => g.Feature("h").EqualTo("j"))
                .Feature("g")
                .ReferringTo(1)
                .Value;

            Assert.That(featStruct1.Unify(featStruct2, out result), Is.True);
            Assert.That(
                result,
                Is.EqualTo(
                        FeatureStruct
                            .New(featureSystem)
                            .Feature("a")
                            .EqualTo(
                                1,
                                a => a.Feature("b").EqualTo("c").Feature("e").EqualTo("f").Feature("h").EqualTo("j")
                            )
                            .Feature("d")
                            .ReferringTo(1)
                            .Feature("g")
                            .ReferringTo(1)
                            .Feature("i")
                            .ReferringTo(1)
                            .Value
                    )
                    .Using(FreezableEqualityComparer<FeatureStruct>.Default)
            );
        }

        [Test]
        public void IsUnifiable()
        {
            Func<FeatureStruct, FeatureStruct, bool> resultsSelector = (fs1, fs2) => fs1.IsUnifiable(fs2);
            Func<FeatureStruct, FeatureStruct, VariableBindings, bool> varResultsSelector = (fs1, fs2, varBindings) =>
                fs1.IsUnifiable(fs2, varBindings);
            TestBinaryOperation(
                EqualityComparer<bool>.Default,
                resultsSelector,
                varResultsSelector,
                // simple
                featSys => false,
                featSys => true,
                // complex
                featSys => false,
                featSys => true,
                // re-entrant
                featSys => false,
                featSys => true,
                featSys => true,
                featSys => true,
                featSys => true,
                featSys => true,
                featSys => false,
                featSys => false,
                // cyclic
                featSys => false,
                featSys => true,
                featSys => true,
                featSys => false,
                featSys => false,
                // variable
                featSys => false,
                featSys => true,
                featSys => true,
                featSys => true,
                featSys => false
            );
        }

        [Test]
        public void PriorityUnion()
        {
            Func<FeatureStruct, FeatureStruct, FeatureStruct> resultsSelector = (fs1, fs2) =>
            {
                fs1.PriorityUnion(fs2);
                return fs1;
            };
            Func<FeatureStruct, FeatureStruct, VariableBindings, FeatureStruct> varResultsSelector = (
                fs1,
                fs2,
                varBindings
            ) =>
            {
                fs1.PriorityUnion(fs2, varBindings);
                return fs1;
            };
            TestBinaryOperation(
                FreezableEqualityComparer<FeatureStruct>.Default,
                resultsSelector,
                varResultsSelector,
                // simple
                featSys => FeatureStruct.New(featSys).Symbol("a2").Symbol("b1").Symbol("c2").Value,
                featSys => FeatureStruct.New(featSys).Symbol("a2").Symbol("b1").Symbol("c2").Value,
                // complex
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Symbol("a2"))
                        .Feature("cx2")
                        .EqualTo(cx2 => cx2.Symbol("b1"))
                        .Feature("cx3")
                        .EqualTo(cx2 => cx2.Symbol("c2"))
                        .Feature("cx4")
                        .EqualTo(cx4 => cx4.Symbol("d2", "d3"))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Symbol("a2"))
                        .Feature("cx2")
                        .EqualTo(cx2 => cx2.Symbol("b1"))
                        .Feature("cx3")
                        .EqualTo(cx2 => cx2.Symbol("c2"))
                        .Feature("cx4")
                        .EqualTo(cx4 => cx4.Symbol("d1", "d2"))
                        .Value,
                // re-entrant
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a2"))
                        .Feature("cx2")
                        .ReferringTo(1)
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx2")
                        .EqualTo(1, cx2 => cx2.Symbol("a1", "a3"))
                        .Feature("cx1")
                        .ReferringTo(1)
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx2")
                        .EqualTo(cx2 => cx2.Symbol(1, "a1", "a3"))
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Feature("a").ReferringTo(1))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a1", "a2"))
                        .Feature("cx2")
                        .ReferringTo(1)
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a2", "a3", "a4"))
                        .Feature("cx2")
                        .ReferringTo(1)
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx2")
                        .EqualTo(cx2 => cx2.Symbol(1, "a2", "a3", "a4"))
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Feature("a").ReferringTo(1))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx2")
                        .EqualTo(1, cx2 => cx2.Symbol("a3"))
                        .Feature("cx1")
                        .ReferringTo(1)
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx2")
                        .EqualTo(1, cx2 => cx2.Symbol("a2", "a3", "a4"))
                        .Feature("cx1")
                        .ReferringTo(1)
                        .Value,
                // cyclic
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a2").Symbol("b1").Feature("cx2").ReferringTo(1))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a2").Symbol("b2").Feature("cx2").ReferringTo(1))
                        .Value,
                // fs1 is not re-entrant and fs2 is re-entrant on cx2, so fs2 wins out and replaces fs1's value for cx2
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a2").Symbol("b1").Feature("cx2").ReferringTo(1))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a2").Symbol("b1").Symbol("c2").Feature("cx2").ReferringTo(1))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a2").Symbol("b1").Symbol("c2").Feature("cx2").ReferringTo(1))
                        .Value,
                // variable
                featSys => FeatureStruct.New(featSys).Feature("a").Not.EqualToVariable("var1").Symbol("b-").Value,
                featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
                featSys => FeatureStruct.New(featSys).Feature("a").Not.EqualToVariable("var1").Symbol("b-").Value,
                featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
                featSys => FeatureStruct.New(featSys).Symbol("a-").Symbol("b-").Value
            );
        }

        [Test]
        public void Union()
        {
            Func<FeatureStruct, FeatureStruct, FeatureStruct> resultsSelector = (fs1, fs2) =>
            {
                fs1.Union(fs2);
                return fs1;
            };
            Func<FeatureStruct, FeatureStruct, VariableBindings, FeatureStruct> varResultsSelector = (
                fs1,
                fs2,
                varBindings
            ) =>
            {
                fs1.Union(fs2, varBindings);
                return fs1;
            };

            TestBinaryOperation(
                FreezableEqualityComparer<FeatureStruct>.Default,
                resultsSelector,
                varResultsSelector,
                // simple
                featSys => FeatureStruct.New(featSys).Symbol("a1", "a2").Value,
                featSys => FeatureStruct.New(featSys).Symbol("a1", "a2").Symbol("c2").Value,
                // complex
                featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Symbol("a1", "a2"))
                        .Feature("cx4")
                        .EqualTo(cx4 => cx4.Symbol("d1", "d2"))
                        .Value,
                // re-entrant
                featSys => FeatureStruct.New(featSys).Feature("cx2").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value,
                featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx2 => cx2.Symbol("a1", "a2", "a3")).Value,
                featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx2 => cx2.Symbol("a1", "a2", "a3")).Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a1", "a2"))
                        .Feature("cx2")
                        .ReferringTo(1)
                        .Value,
                featSys => FeatureStruct.New().Value,
                featSys => FeatureStruct.New().Value,
                featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2", "a3")).Value,
                featSys => FeatureStruct.New().Value,
                // cyclic
                featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value,
                featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value,
                featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value,
                // fs1 is not re-entrant, so the result is also not re-entrant
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Symbol("a1", "a2").Feature("cx2").EqualTo(cx2 => cx2.Symbol("c1", "c2")))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a1", "a2").Symbol("c1", "c2").Feature("cx2").ReferringTo(1))
                        .Value,
                // variable
                featSys => FeatureStruct.New(featSys).Symbol("b-").Value,
                // it is unclear what to do when performing a union of a variable and a value, so I just have the value win out
                featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
                featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
                featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
                featSys => FeatureStruct.New(featSys).Symbol("b-").Value
            );
        }

        [Test]
        public void Subtract()
        {
            Func<FeatureStruct, FeatureStruct, FeatureStruct> resultsSelector = (fs1, fs2) =>
            {
                fs1.Subtract(fs2);
                return fs1;
            };
            Func<FeatureStruct, FeatureStruct, VariableBindings, FeatureStruct> varResultsSelector = (
                fs1,
                fs2,
                varBindings
            ) =>
            {
                fs1.Subtract(fs2, varBindings);
                return fs1;
            };

            TestBinaryOperation(
                FreezableEqualityComparer<FeatureStruct>.Default,
                resultsSelector,
                varResultsSelector,
                // simple
                featSys => FeatureStruct.New(featSys).Symbol("a1").Symbol("b1").Value,
                featSys => FeatureStruct.New(featSys).Symbol("a1").Symbol("b1").Value,
                // complex
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Symbol("a1"))
                        .Feature("cx2")
                        .EqualTo(cx2 => cx2.Symbol("b1"))
                        .Feature("cx4")
                        .EqualTo(cx4 => cx4.Symbol("d1"))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Symbol("a1"))
                        .Feature("cx2")
                        .EqualTo(cx2 => cx2.Symbol("b1"))
                        .Value,
                // re-entrant
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a1"))
                        .Feature("cx2")
                        .ReferringTo(1)
                        .Value,
                featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a2")).Value,
                featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a2")).Value,
                featSys => FeatureStruct.New().Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a1"))
                        .Feature("cx2")
                        .ReferringTo(1)
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Symbol(1, "a1"))
                        .Feature("cx2")
                        .EqualTo(cx2 => cx2.Feature("a").ReferringTo(1))
                        .Value,
                featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a1"))
                        .Feature("cx2")
                        .ReferringTo(1)
                        .Value,
                // cyclic
                featSys =>
                    FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1").Symbol("b1")).Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a1").Feature("cx2").ReferringTo(1))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Symbol("a1").Symbol("b1").Feature("cx2").EqualTo(cx2 => cx2.Symbol("c1")))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(cx1 => cx1.Symbol("a1").Symbol("b1").Feature("cx2").EqualTo(cx2 => cx2.Symbol("c1")))
                        .Value,
                featSys =>
                    FeatureStruct
                        .New(featSys)
                        .Feature("cx1")
                        .EqualTo(1, cx1 => cx1.Symbol("a1").Symbol("b1").Symbol("c1").Feature("cx2").ReferringTo(1))
                        .Value,
                // variable
                featSys => FeatureStruct.New(featSys).Feature("a").EqualToVariable("var1").Value,
                // it is unclear what to do when subtracting a variable and a value, so I preserve the existing value
                featSys => FeatureStruct.New(featSys).Feature("a").EqualToVariable("var1").Value,
                featSys => FeatureStruct.New(featSys).Symbol("a+").Value,
                featSys => FeatureStruct.New().Value,
                featSys => FeatureStruct.New(featSys).Symbol("a+").Value
            );
        }

        private void TestBinaryOperation<TResult>(
            IEqualityComparer<TResult> comparer,
            Func<FeatureStruct, FeatureStruct, TResult> resultsSelector,
            Func<FeatureStruct, FeatureStruct, VariableBindings, TResult> varResultsSelector,
            params Func<FeatureSystem, TResult>[] expectedSelectors
        )
        {
            // simple
            var featSys = new FeatureSystem
            {
                new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2"), new FeatureSymbol("a3")),
                new SymbolicFeature("b", new FeatureSymbol("b1"), new FeatureSymbol("b2"), new FeatureSymbol("b3")),
                new SymbolicFeature("c", new FeatureSymbol("c1"), new FeatureSymbol("c2"), new FeatureSymbol("c3"))
            };
            featSys.Freeze();

            FeatureStruct fs1 = FeatureStruct.NewMutable(featSys).Symbol("a1").Symbol("b1").Value;
            FeatureStruct fs2 = FeatureStruct.NewMutable(featSys).Symbol("a2").Symbol("c2").Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[0](featSys)).Using(comparer));

            fs1 = FeatureStruct.NewMutable(featSys).Symbol("a1", "a2").Symbol("b1").Symbol("c2").Value;
            fs2 = FeatureStruct.NewMutable(featSys).Symbol("a2").Symbol("c2").Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[1](featSys)).Using(comparer));

            // complex
            featSys = new FeatureSystem
            {
                new ComplexFeature("cx1"),
                new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2"), new FeatureSymbol("a3")),
                new ComplexFeature("cx2"),
                new SymbolicFeature("b", new FeatureSymbol("b1"), new FeatureSymbol("b2"), new FeatureSymbol("b3")),
                new ComplexFeature("cx3"),
                new SymbolicFeature("c", new FeatureSymbol("c1"), new FeatureSymbol("c2"), new FeatureSymbol("c3")),
                new ComplexFeature("cx4"),
                new SymbolicFeature("d", new FeatureSymbol("d1"), new FeatureSymbol("d2"), new FeatureSymbol("d3"))
            };
            featSys.Freeze();

            fs1 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(cx1 => cx1.Symbol("a1"))
                .Feature("cx2")
                .EqualTo(cx2 => cx2.Symbol("b1"))
                .Feature("cx4")
                .EqualTo(cx4 => cx4.Symbol("d1"))
                .Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(cx1 => cx1.Symbol("a2"))
                .Feature("cx3")
                .EqualTo(cx2 => cx2.Symbol("c2"))
                .Feature("cx4")
                .EqualTo(cx4 => cx4.Symbol("d2", "d3"))
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[2](featSys)).Using(comparer));

            fs1 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(cx1 => cx1.Symbol("a1", "a2"))
                .Feature("cx2")
                .EqualTo(cx2 => cx2.Symbol("b1"))
                .Feature("cx4")
                .EqualTo(cx4 => cx4.Symbol("d1"))
                .Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(cx1 => cx1.Symbol("a2"))
                .Feature("cx3")
                .EqualTo(cx2 => cx2.Symbol("c2"))
                .Feature("cx4")
                .EqualTo(cx4 => cx4.Symbol("d1", "d2"))
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[3](featSys)).Using(comparer));

            // re-entrant
            featSys = new FeatureSystem
            {
                new ComplexFeature("cx1"),
                new SymbolicFeature(
                    "a",
                    new FeatureSymbol("a1"),
                    new FeatureSymbol("a2"),
                    new FeatureSymbol("a3"),
                    new FeatureSymbol("a4")
                ),
                new ComplexFeature("cx2")
            };
            featSys.Freeze();

            fs1 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(1, cx1 => cx1.Symbol("a1"))
                .Feature("cx2")
                .ReferringTo(1)
                .Value;
            fs2 = FeatureStruct.NewMutable(featSys).Feature("cx2").EqualTo(cx2 => cx2.Symbol("a2")).Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[4](featSys)).Using(comparer));

            fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx2")
                .EqualTo(1, cx2 => cx2.Symbol("a1", "a3"))
                .Feature("cx1")
                .ReferringTo(1)
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[5](featSys)).Using(comparer));

            fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx2")
                .EqualTo(cx2 => cx2.Symbol(1, "a1", "a3"))
                .Feature("cx1")
                .EqualTo(cx1 => cx1.Feature("a").ReferringTo(1))
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[6](featSys)).Using(comparer));

            fs1 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(1, cx1 => cx1.Symbol("a1"))
                .Feature("cx2")
                .ReferringTo(1)
                .Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx2")
                .EqualTo(1, cx2 => cx2.Symbol("a1", "a2"))
                .Feature("cx1")
                .ReferringTo(1)
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[7](featSys)).Using(comparer));

            fs1 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(1, cx1 => cx1.Symbol("a1", "a2"))
                .Feature("cx2")
                .ReferringTo(1)
                .Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx2")
                .EqualTo(1, cx2 => cx2.Symbol("a2", "a3", "a4"))
                .Feature("cx1")
                .ReferringTo(1)
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[8](featSys)).Using(comparer));

            fs1 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(cx1 => cx1.Symbol(1, "a1", "a2"))
                .Feature("cx2")
                .EqualTo(cx2 => cx2.Feature("a").ReferringTo(1))
                .Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx2")
                .EqualTo(cx2 => cx2.Symbol(1, "a2", "a3", "a4"))
                .Feature("cx1")
                .EqualTo(cx1 => cx1.Feature("a").ReferringTo(1))
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[9](featSys)).Using(comparer));

            fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx2")
                .EqualTo(1, cx2 => cx2.Symbol("a3"))
                .Feature("cx1")
                .ReferringTo(1)
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[10](featSys)).Using(comparer));

            fs1 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(1, cx1 => cx1.Symbol("a1"))
                .Feature("cx2")
                .ReferringTo(1)
                .Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx2")
                .EqualTo(1, cx2 => cx2.Symbol("a2", "a3", "a4"))
                .Feature("cx1")
                .ReferringTo(1)
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[11](featSys)).Using(comparer));

            // cyclic
            featSys = new FeatureSystem
            {
                new ComplexFeature("cx1"),
                new SymbolicFeature("a", new FeatureSymbol("a1"), new FeatureSymbol("a2"), new FeatureSymbol("a3")),
                new SymbolicFeature("b", new FeatureSymbol("b1"), new FeatureSymbol("b2"), new FeatureSymbol("b3")),
                new SymbolicFeature("c", new FeatureSymbol("c1"), new FeatureSymbol("c2"), new FeatureSymbol("c3")),
                new ComplexFeature("cx2")
            };
            featSys.Freeze();

            fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1").Symbol("b1")).Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(1, cx1 => cx1.Symbol("a2").Feature("cx2").ReferringTo(1))
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[12](featSys)).Using(comparer));

            fs1 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(1, cx1 => cx1.Symbol("a1", "a2").Feature("cx2").ReferringTo(1))
                .Value;
            fs2 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a2").Symbol("b2")).Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[13](featSys)).Using(comparer));

            fs1 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(cx1 => cx1.Symbol("a1", "a2").Symbol("b1").Feature("cx2").EqualTo(cx2 => cx2.Symbol("c1")))
                .Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(1, cx1 => cx1.Symbol("a2").Feature("cx2").ReferringTo(1))
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[14](featSys)).Using(comparer));

            fs1 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(cx1 => cx1.Symbol("a1").Symbol("b1").Feature("cx2").EqualTo(cx2 => cx2.Symbol("c1", "c2")))
                .Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(1, cx1 => cx1.Symbol("a2").Symbol("c2").Feature("cx2").ReferringTo(1))
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[15](featSys)).Using(comparer));

            fs1 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(1, cx1 => cx1.Symbol("a1").Symbol("b1").Symbol("c1").Feature("cx2").ReferringTo(1))
                .Value;
            fs2 = FeatureStruct
                .NewMutable(featSys)
                .Feature("cx1")
                .EqualTo(1, cx1 => cx1.Symbol("a2").Symbol("c2").Feature("cx2").ReferringTo(1))
                .Value;
            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[16](featSys)).Using(comparer));

            // variable
            featSys = new FeatureSystem
            {
                new SymbolicFeature("a", new FeatureSymbol("a+", "+"), new FeatureSymbol("a-", "-")),
                new SymbolicFeature("b", new FeatureSymbol("b+", "+"), new FeatureSymbol("b-", "-"))
            };
            featSys.Freeze();

            fs1 = FeatureStruct.NewMutable(featSys).Feature("a").EqualToVariable("var1").Symbol("b-").Value;

            fs2 = FeatureStruct.NewMutable(featSys).Feature("a").Not.EqualToVariable("var1").Symbol("b-").Value;

            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[17](featSys)).Using(comparer));

            fs1 = FeatureStruct.NewMutable(featSys).Feature("a").EqualToVariable("var1").Symbol("b-").Value;

            fs2 = FeatureStruct.NewMutable(featSys).Symbol("a+").Symbol("b-").Value;

            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[18](featSys)).Using(comparer));

            fs1 = FeatureStruct.NewMutable(featSys).Symbol("a+").Symbol("b-").Value;

            fs2 = FeatureStruct.NewMutable(featSys).Feature("a").Not.EqualToVariable("var1").Symbol("b-").Value;

            Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[19](featSys)).Using(comparer));

            fs1 = FeatureStruct.NewMutable(featSys).Symbol("a+").Symbol("b-").Value;

            fs2 = FeatureStruct.NewMutable(featSys).Feature("a").Not.EqualToVariable("var1").Symbol("b-").Value;

            var varBindings = new VariableBindings();
            varBindings["var1"] = new SymbolicFeatureValue(featSys.GetSymbol("a-"));
            Assert.That(
                varResultsSelector(fs1, fs2, varBindings),
                Is.EqualTo(expectedSelectors[20](featSys)).Using(comparer)
            );

            fs1 = FeatureStruct.NewMutable(featSys).Symbol("a+").Symbol("b-").Value;

            fs2 = FeatureStruct.NewMutable(featSys).Feature("a").Not.EqualToVariable("var1").Symbol("b-").Value;

            varBindings = new VariableBindings();
            varBindings["var1"] = new SymbolicFeatureValue(featSys.GetSymbol("a+"));
            Assert.That(
                varResultsSelector(fs1, fs2, varBindings),
                Is.EqualTo(expectedSelectors[21](featSys)).Using(comparer)
            );
        }
    }
}
