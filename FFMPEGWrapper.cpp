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
	//IntPtr temp =  Marshal::SecureStringToGlobalAllocAnsi(input_file);
	//IntPtr temp2 =  Marshal::SecureStringToGlobalAllocAnsi(output_file);
	//char * f = static_cast<char*> (temp.ToPointer());
	//char * f2 = static_cast<char*> (temp2.ToPointer());
	return FNative->convert_wav_to_mp3(input_file,output_file);
}