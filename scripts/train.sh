#!/bin/bash

while [[ "$#" -gt 0 ]]; do case $1 in
  -s|--source) sourceCorpus="$2"; shift;;
  -t|--target) targetCorpus="$2"; shift;;
  -e|--exclude) exclude="$2"; shift;;
  -i|--interval) sizeInterval="$2"; shift;;
  -r|--root) rootDir="$2"; shift;;
  *) echo "Unknown parameter passed: $1"; exit 1;;
esac; shift; done

if [ -z "$sourceCorpus" ]; then
  echo "A source corpus must be specified."
  exit 1
fi

if [ -z "$targetCorpus" ]; then
  echo "A target corpus must be specified."
  exit 1
fi

excludeParam=""
if [ -n "$exclude" ]; then
  excludeParam="-e $exclude"
fi
sizeInterval=${sizeInterval:-'300'}
rootDir=${rootDir:-'.'}

translatorCmd="dotnet run -f netcoreapp2.1 -c Release -p ../src/Translator.CommandLine/ -- "
langPair="$sourceCorpus-$targetCorpus"

segCount=$($translatorCmd corpus -s usx,$rootDir/corpora/$sourceCorpus -t usx,$rootDir/corpora/$targetCorpus $excludeParam -st latin -tt latin -c)
echo "Segment count: $segCount"
n=$(expr $segCount / $sizeInterval)
r=$(expr $segCount % $sizeInterval)
if [ $r -gt 0 ]
then
    n=$(expr $n + 1)
fi

sizes=()
for (( i = 1; i <= $n; i++ ))
do
    maxSize=$(expr $i \* $sizeInterval)
	sizes+=("$maxSize")
done

command="$translatorCmd train -q -s usx,$rootDir/corpora/$sourceCorpus -t usx,$rootDir/corpora/$targetCorpus $excludeParam -st latin -tt latin -m {1} $rootDir/engines/$langPair/{1}/tuned.cfg"

parallel --no-notice --bar --header : --results output -q $command ::: $langPair ${sizes[@]} ::: train 2>&1 >/dev/null

for size in "${sizes[@]}"
do
    filePath=$rootDir/engines/$langPair/$size
    mkdir -p $filePath
    cp ../src/Translator.CommandLine/data/default-smt.cfg $filePath/untuned.cfg
done
