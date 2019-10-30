# Translation Engine Evaluation Scripts

Machine provides tools for building a machine translation engine from a parallel corpus. The resulting engine can be used to generate predictive translation suggestions. The scripts in this folder can be used to automate the evaluation of the translation engine for a USX-based parallel corpus. The scripts are designed to run in a Linux environment. The evaluation process is divided into three steps: setup, training, and testing. The scripts were used to generate the data for the _[CAT 2017 Preliminary Engine Evaluation](https://docs.google.com/document/d/1BW9MsWRR1yXe1mlTqUcApypb6zezIyYz4fHdUhb3c44/edit?usp=sharing)_ paper.

## Setup

All training data and results are contained in a single root data directory. The directory contains multiple subdirectories:

- `corpora`
- `engines`
- `output`

The `corpora` directory contains a subdirectory for each available corpus. Each corpus folder should contain USX files with the standard Scripture book number and id, e.g. `042LUK.usx`. This is the only directory that needs to be created and populated prior to performing training and testing.

The `engines` directory contains the trained translation engines generated during the training step. Each subdirectory corresponds to a trained parallel corpus.

The `output` directory contains the results from the training and testing steps.

The scripts require that the .NET Core 2.1 SDK and GNU Parallel are installed. Instructions to download and install the .NET Core SDK can be found [here](https://dotnet.microsoft.com/download/dotnet-core/2.1). The latest version of GNU Parallel is needed and can be downloaded [here](https://www.gnu.org/software/parallel/). There is no official package for Ubuntu, so the source must be downloaded, built, and installed manually. After downloading the package, unzip the source into a directory and run the following commands:

```bash
./configure
make
sudo make install
```

Lastly, the scripts must be executed from this repo, so the repo should be cloned locally.

## Training

The translation engine must be built from a parallel corpus using the `train.sh` script. The `train.sh` script trains multiple engines with increasing amounts of data. The engines are generated in parallel. A source and target corpus must be specified. The corpora should already exist in the `corpora` directory of the data root directory. Data should be excluded from the training set, so that it can be used during testing. This can be done by specifying a book to exclude from training. To perform training, execute the following command from the `scripts` directory of this repo:

```bash
./train.sh -s <source_corpus> -t <target_corpus> -e <book_id_to_exclude> -i <size_interval> -r <root_data_dir>
```

Here is an example:

```bash
./train.sh -s spanish -t english -e JHN -i 300 -r ../data
```

The generated engines are placed in the `engines/<source_corpus>-<target_corpus>/<training_size>/` directory of the root data directory, e.g. `engines/spanish-english/300/`. The output statistics are placed in the `output/<source_corpus>-<target_corpus>/<training_size>/train/` directory, e.g. `output/spanish-english/300/train/`. Traning generates the following statistics for each engine:

- Number of segments trained
- Language model perplexity
- BLEU calculated during tuning

## Testing

Once the engines are generated from the training data, the engines can be tested using the `test.sh` script. The `test.sh` script simulates a translator who is trying to produce the target verses in the test dataset. As with the training step, a source and target corpus must be specified. The testing data must also be specified. To perform testing, execute the following command from the `scripts` directory of this repo:

```bash
./test.sh -s <source_corpus> -t <target_corpus> -i <book_id_to_include> -c <confidence> -r <root_data_dir>
```

Here is an example:

```bash
./test.sh -s spanish -t english -i JHN -c 0.2,0.4 -r ../data
```

The output statistics are placed in the `output/<source_corpus>-<target_corpus>/<training_size>/test/`, e.g. `output/spanish-english/300/test/`. Testing generates the following statistics for each engine:

- Number of segments translated
- Number of suggestions
- Number of accepted suggestions
- Percentage of each accepted suggestion type (complete, initial, final, medial)
- Keystroke and mouse-action ratio (KSMR)
- Suggestion precision

For more information on these statistics, see the _[CAT 2017 Preliminary Engine Evaluation](https://docs.google.com/document/d/1BW9MsWRR1yXe1mlTqUcApypb6zezIyYz4fHdUhb3c44/edit?usp=sharing)_ paper
