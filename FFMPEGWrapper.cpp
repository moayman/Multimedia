// This is the main DLL file.

#include "stdafx.h"
#include "FFMPEGWrapper.h"

FFMPEGWrapper::Wrapper :: Wrapper()
{
	FNative = new FFMPEGNative();
}

FFMPEGWrapper::Wrapper :: ~Wrapper()
{
	delete FNative;
}

int FFMPEGWrapper::Wrapper :: wrap_convert_wav_to_mp3(char* input_file, char* output_file)
{
	return FNative->convert_wav_to_mp3(input_file,output_file);
}
char** FFMPEGWrapper::Wrapper :: wrap_get_media_file_meta_data(char* file_name)
{
	return FNative->get_media_file_meta_data(file_name);
}