#pragma once
#include <cstdio>
#include <cstdlib>
#ifdef __cplusplus
extern "C" {
#endif
#include "libavcodec\avcodec.h"

#include "libavformat\avformat.h"
#include "libavformat\avio.h"

#include "libavutil\opt.h"
#include "libavutil\avassert.h"
#include "libavutil\avstring.h"
#include "libavutil\avutil.h"
#include "libavutil\audio_fifo.h"
#include "libavutil/dict.h"

#include "libswresample\swresample.h"

#ifdef __cplusplus
}
#endif


/** The output bit rate in kbit/s */
#define OUTPUT_BIT_RATE 96000
/** The number of output channels */
#define OUTPUT_CHANNELS 2

// Global timestamp for the audio frames
static int64_t pts = 0;
class FFMPEGNative
{
public:
	FFMPEGNative(void);
	~FFMPEGNative(void);
private:
	const char *get_error_text(const int error)
{
    static char error_buffer[255];
    av_strerror(error, error_buffer, sizeof(error_buffer));
    return error_buffer;
}

// Open an input file and the required decoder
int open_input_file(const char * filename, AVFormatContext ** input_format_context, AVCodecContext ** input_codec_context);

// Open an output file and the required encoder.
// Also set some basic encoder parameters.
// Some of these parameters are based on the input file's parameters.
int open_output_file(const char *filename, AVCodecContext *input_codec_context, AVFormatContext **output_format_context,
                     AVCodecContext **output_codec_context);

// Initialize one data packet for reading or writing.
void init_packet(AVPacket *packet);

// Initialize one audio frame for reading from the input file
int init_input_frame(AVFrame **frame);

// Initialize the audio resampler based on the input and output codec settings.
// If the input and output sample formats differ, a conversion is required
// libswresample takes care of this, but requires initialization.
int init_resampler(AVCodecContext *input_codec_context, AVCodecContext *output_codec_context, SwrContext **resample_context);

// Initialize a FIFO buffer for the audio samples to be encoded.
int init_fifo(AVAudioFifo **fifo, AVCodecContext *output_codec_context);

// Write the header of the output file container.
int write_output_file_header(AVFormatContext *output_format_context);

// Decode one audio frame from the input file.
int decode_audio_frame(AVFrame *frame, AVFormatContext *input_format_context, AVCodecContext *input_codec_context,
                              int *data_present, int *finished);

// Initialize a temporary storage for the specified number of audio samples.
// The conversion requires temporary storage due to the different format.
// The number of audio samples to be allocated is specified in frame_size.
int init_converted_samples(uint8_t ***converted_input_samples, AVCodecContext *output_codec_context, int frame_size);

// Convert the input audio samples into the output sample format.
// The conversion happens on a per-frame basis, the size of which is specified
// by frame_size.
int convert_samples(const uint8_t **input_data, uint8_t **converted_data, const int frame_size, SwrContext *resample_context);

// Add converted input audio samples to the FIFO buffer for later processing.
int add_samples_to_fifo(AVAudioFifo *fifo, uint8_t **converted_input_samples, const int frame_size);

// Read one audio frame from the input file, decodes, converts and stores
// it in the FIFO buffer.
int read_decode_convert_and_store(AVAudioFifo *fifo, AVFormatContext *input_format_context, AVCodecContext *input_codec_context,
                                         AVCodecContext *output_codec_context, SwrContext *resampler_context, int *finished);

// Initialize one input frame for writing to the output file.
// The frame will be exactly frame_size samples large.
int init_output_frame(AVFrame **frame, AVCodecContext *output_codec_context, int frame_size);

// Encode one frame worth of audio to the output file.
int encode_audio_frame(AVFrame *frame, AVFormatContext *output_format_context, AVCodecContext *output_codec_context, int *data_present);

// Load one audio frame from the FIFO buffer, encode and write it to the
// output file.
int load_encode_and_write(AVAudioFifo *fifo, AVFormatContext *output_format_context, AVCodecContext *output_codec_context);

// Write the trailer of the output file container.
int write_output_file_trailer(AVFormatContext *output_format_context);

public :
int convert_wav_to_mp3(char* input_file, char* output_file);

// Gets the meta data of a given media file
// TODO: return a complex object of key value pairs
int get_media_file_meta_data(char* file_name);
};

