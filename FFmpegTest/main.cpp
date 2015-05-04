#include <cstdio>
#include <cstdlib>
#include <vector>

#ifdef __cplusplus
extern "C" {
#endif
    #include <libavcodec/avcodec.h>
    #include <libavformat/avformat.h>
    #include <libavutil/opt.h>
    #include <libavutil/channel_layout.h>
    #include <libavutil/common.h>
    #include <libavutil/imgutils.h>
    #include <libavutil/mathematics.h>
    #include <libavutil/samplefmt.h>

#ifdef __cplusplus
}
#endif

#ifndef u_int8_t
#define uint8_t u_int8_t
#endif

void audio_encode_example(char*inputfile, char *filename);
int select_channel_layout(AVCodec *codec);
int select_sample_rate(AVCodec *codec);
int check_sample_fmt(AVCodec *codec, enum AVSampleFormat sample_fmt);

int main()
{


    audio_encode_example("bullet.wav", "bullet.mp2");
}

int check_sample_fmt(AVCodec *codec, enum AVSampleFormat sample_fmt)
{
    const enum AVSampleFormat *p = codec->sample_fmts;
    while (*p != AV_SAMPLE_FMT_NONE) {
        if (*p == sample_fmt)
            return 1;
        p++;
    }
    return 0;
}

/* just pick the highest supported samplerate */
 int select_sample_rate(AVCodec *codec)
{
    const int *p;
    int best_samplerate = 0;
    if (!codec->supported_samplerates)
        return 44100;
    p = codec->supported_samplerates;
    while (*p) {
        best_samplerate = FFMAX(*p, best_samplerate);
        p++;
    }
    return best_samplerate;
}
/* select layout with the highest channel count */
 int select_channel_layout(AVCodec *codec)
{
    const uint64_t *p;
    uint64_t best_ch_layout = 0;
    int best_nb_channels   = 0;
    if (!codec->channel_layouts)
        return AV_CH_LAYOUT_STEREO;
    p = codec->channel_layouts;
    while (*p) {
        int nb_channels = av_get_channel_layout_nb_channels(*p);
        if (nb_channels > best_nb_channels) {
            best_ch_layout    = *p;
            best_nb_channels = nb_channels;
        }
        p++;
    }
    return best_ch_layout;
}



void audio_encode_example(char*inputfile, char *filename)
{
    avcodec_register_all();
    av_register_all();

    AVFormatContext *ptr_format_context = avformat_alloc_context();

    // open the file
    if (avformat_open_input(&ptr_format_context, inputfile, NULL, NULL) != 0)
    {
        fprintf(stderr, "failed to open the file\n");
        return;
    }

    // read the header
    if (avformat_find_stream_info(ptr_format_context, NULL) < 0)
    {
        return;
    }

    // dump the file info
    av_dump_format(ptr_format_context, 0, inputfile, 0);

    // hold the data buffers
    std::vector<u_int8_t * > samples;
    AVPacket pkt;

    // start decoding
    // as wav is raw pcm, decoding is simply reading the file data section
    while (av_read_frame(ptr_format_context, &pkt) == 0)
    {
        //avcodec_decode_audio4(ptr_codec_context, ptr_frame, &got_output, &pkt);
        //int buffer_size = pkt.size;
        //u_int8_t * buffer = (u_int8_t *)av_malloc(buffer_size);
        //memcpy(buffer, pkt.data, buffer_size);
        samples.push_back(pkt.data);
    }

    AVCodec *codec;
    AVCodecContext *c= NULL;
    AVFrame *frame;

    printf("Encode audio file %s\n", filename);

    // FIXME: change to mp3
    codec = avcodec_find_encoder(AV_CODEC_ID_MP2);
    if (!codec)
    {
        fprintf(stderr, "Codec not found\n");
        return;
    }
    c = avcodec_alloc_context3(codec);
    if (!c)
    {
        fprintf(stderr, "Could not allocate audio codec context\n");
        return;
    }

    // FIXME: fix bitrate
    c->bit_rate = 64000;

    /* check that the encoder supports s16 pcm input */
    c->sample_fmt = AV_SAMPLE_FMT_S16;
    if (!check_sample_fmt(codec, c->sample_fmt))
    {
        fprintf(stderr, "Encoder does not support sample format %s",
                av_get_sample_fmt_name(c->sample_fmt));
        return;
    }

    // FIXME: check these parameters
    c->sample_rate    = select_sample_rate(codec);
    c->channel_layout = select_channel_layout(codec);
    c->channels       = av_get_channel_layout_nb_channels(c->channel_layout);

    /* open it */
    if (avcodec_open2(c, codec, NULL) < 0)
    {
        fprintf(stderr, "Could not open codec\n");
        return;
    }

    // allocate the frame
    frame = av_frame_alloc();
    if (!frame)
    {
        fprintf(stderr, "Could not allocate audio frame\n");
        return;
    }
    frame->nb_samples     = c->frame_size;
    frame->format         = c->sample_fmt;
    frame->channel_layout = c->channel_layout;

    FILE *f = fopen(filename, "wb");

    // encode each frame
    int i, ret, got_output;
    int frame_numbers = samples.size();
    for (i = 0; i < frame_numbers; i++)
    {
        // get the buffer size to needed in the frame
        int buffer_size = av_samples_get_buffer_size(NULL, c->channels, c->frame_size,
                                                     c->sample_fmt, 0);

        // fill the frame with the data
        avcodec_fill_audio_frame(frame, c->channels, c->sample_fmt, (const u_int8_t *)samples[i], buffer_size, 0);

        av_init_packet(&pkt);
        pkt.data = NULL; // packet data will be allocated by the encoder
        pkt.size = 0;

        // encode the data with the codec
        ret = avcodec_encode_audio2(c, &pkt, frame, &got_output);

        if (ret < 0)
        {
            fprintf(stderr, "Error encoding audio frame\n");
            return;
        }

        // got the encoded packet, output to file
        if (got_output)
        {
            fwrite(pkt.data, 1, pkt.size, f);
            av_free_packet(&pkt);
        }
    }

    // got delayed frames
    for (got_output = 1; got_output; i++) {
        ret = avcodec_encode_audio2(c, &pkt, NULL, &got_output);
        if (ret < 0)
        {
            fprintf(stderr, "Error encoding frame\n");
            return;
        }
        if (got_output) {
            fwrite(pkt.data, 1, pkt.size, f);
            av_free_packet(&pkt);
        }
    }


    // FIXME: memory leeks
    fclose(f);
    av_frame_free(&frame);
    avcodec_close(c);
    av_free(c);

}
