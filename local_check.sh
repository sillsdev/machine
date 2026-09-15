#!/bin/bash
dotnet tool restore
dotnet restore
dotnet csharpier check .
if [ $? -ne 0 ]; then
  exit 1
fi
dotnet build --no-restore -c Release
if [ $? -ne 0 ]; then
  exit 1
fi
dotnet test --verbosity normal
if [ $? -ne 0 ]; then
  exit 1
fi
# The same checks CI runs after the test suite: recompute the generated-surface coverage ledger and
# execute every conformance fixture. Both are also asserted from dotnet test; running them through the
# CLI keeps the failure legible and reproducible by hand.
dotnet run --no-build -c Release --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- \
  --semantic-coverage --repository-root .
if [ $? -ne 0 ]; then
  exit 1
fi
dotnet run --no-build -c Release --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- \
  --fixtures conformance
if [ $? -ne 0 ]; then
  exit 1
fi
python conformance/parity-check.py

# The counterfactual evidence sweep re-parses every fixture once per surface, so it costs minutes and is
# deliberately not run here. It has its own workflow (.github/workflows/counterfactual-coverage.yml) and
# can be run on demand:
#   dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --counterfactual --repository-root .
