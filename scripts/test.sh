#!/bin/bash

while [[ "$#" -gt 0 ]]; do case $1 in
  -s|--source) sourceCorpus="$2"; shift;;
  -t|--target) targetCorpus="$2"; shift;;
  -i|--include) include="$2"; shift;;
  -c|--confidence) confidence="$2"; shift;;
  -r|--root) rootDir="$2"; shift;;
  *) echo "Unknown parameter passed: $1"; exit 1;;
esac; shift; done

includeParam=""
if [ -n "$include" ]; then
  includeParam="-i $include"
fi
confidence=${confidence:-'0.2'}

IFS=',' read -r -a confidences <<< "$confidence"

langPair="$sourceCorpus-$targetCorpus"
translatorCmd="dotnet run -f netcoreapp2.1 -c Release -p ../src/Translator.CommandLine/ -- "

sizes=()
for dir in $rootDir/engines/$langPair/*
do
    sizes+=("${dir##*/}")
done

command="$translatorCmd test -q -s usx,$rootDir/corpora/$sourceCorpus -t usx,$rootDir/corpora/$targetCorpus $includeParam -c {3} -st latin -tt latin $rootDir/engines/$langPair/{1}/{2}.cfg"

parallel --no-notice --bar --results $rootDir/output/$langPair/{1}/test/{3}-{2}/ -q $command ::: ${sizes[@]} ::: tuned untuned ::: ${confidences[@]} 2>&1 >/dev/null