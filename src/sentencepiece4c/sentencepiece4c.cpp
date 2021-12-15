#include "sentencepiece4c.h"

#include <iterator>
#include <sstream>
#include <string>
#include <vector>

#include <sentencepiece_processor.h>
#include <sentencepiece_trainer.h>

extern "C"
{
	void* sp_createProcessor()
	{
		return new sentencepiece::SentencePieceProcessor();
	}

	int sp_loadProcessor(void* processorHandle, const char* filename)
	{
		auto processor = static_cast<sentencepiece::SentencePieceProcessor*>(processorHandle);
		return (int)processor->Load(filename).code();
	}

	int sp_encodeAsPieces(void* processorHandle, const char* input, char* output,
		unsigned int capacity, unsigned int* length)
	{
		auto processor = static_cast<sentencepiece::SentencePieceProcessor*>(processorHandle);
		std::vector<std::string> pieces;
		sentencepiece::util::Status status = processor->Encode(input, &pieces);

		std::ostringstream os;
		auto b = std::begin(pieces);
		auto e = std::end(pieces);

		if (b != e)
		{
			std::copy(b, std::prev(e), std::ostream_iterator<std::string>(os, " "));
			b = std::prev(e);
		}
		if (b != e)
		{
			os << *b;
		}

		std::string joined = os.str();
		if (output != nullptr)
		{
			size_t len = joined.copy(output, (size_t)capacity);
			if (len < capacity)
				output[len] = '\0';
		}
		*length = (unsigned int)joined.length();
		return (int)status.code();
	}

	void sp_destroyProcessor(void* processorHandle)
	{
		auto processor = static_cast<sentencepiece::SentencePieceProcessor*>(processorHandle);
		delete processor;
	}

	int sp_train(const char* inputFilenames, const char* modelPrefix, const char* args)
	{
		return (int)sentencepiece::SentencePieceTrainer::Train(std::string(args) + " --input=" + inputFilenames
			+ " --model_prefix=" + modelPrefix).code();
	}
}
