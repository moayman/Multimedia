// FFMPEGWrapper.h

#pragma once
#include "FFMPEGNative.h"
using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::Security;

namespace FFMPEGWrapper {

	public ref class Wrapper
	{
		// TODO: Add your methods for this class here.
	public:
		Wrapper();
		~Wrapper();
		int wrap_convert_wav_to_mp3(char* input_file, char* output_file);
	private:
		FFMPEGNative * FNative;
	};
}
