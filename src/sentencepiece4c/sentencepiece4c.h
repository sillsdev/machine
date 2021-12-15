#ifndef _SENTENCEPIECE4C_H_
#define _SENTENCEPIECE4C_H_

#if defined _WIN32 || defined __CYGWIN__
#if defined sentencepiece4c_EXPORTS
#if defined __GNUC__
#define SP4C_API __attribute__((dllexport))
#else
#define SP4C_API __declspec(dllexport)
#endif
#else
#if defined __GNUC__
#define SP4C_API __attribute__((dllimport))
#else
#define SP4C_API __declspec(dllimport)
#endif
#endif
#elif __GNUC__ >= 4
#define SP4C_API __attribute__((visibility("default")))
#else
#define SP4C_API
#endif

#ifdef __cplusplus
extern "C"
{
#endif

	SP4C_API void* sp_createProcessor();
	SP4C_API int sp_loadProcessor(void* processorHandle, const char* filename);
	SP4C_API int sp_encodeAsPieces(void* processorHandle, const char* input, char* output,
		unsigned int capacity, unsigned int* length);
	SP4C_API void sp_destroyProcessor(void* processorHandle);
	SP4C_API int sp_train(const char* inputFilenames, const char* modelPrefix, const char* args);


#ifdef __cplusplus
}
#endif

#endif